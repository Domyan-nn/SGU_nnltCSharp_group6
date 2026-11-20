using System.Globalization;
using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

public sealed record PaymentRow(long OrderId, decimal Amount, DateTime PaidAtUtc);
public sealed record RefundRow(decimal Amount, DateTime ExecutedAtUtc);
public sealed record SoldLineRow(string ProductName, int Quantity, decimal UnitPrice, decimal AddonPerUnit);

// Phần tính thuần (không chạm CSDL) để kiểm thử được mà không cần SQL Server.
public static class RevenueAggregator
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    // Ngày theo giờ Việt Nam của một thời điểm UTC (Kind không xác định đọc từ CSDL được coi là UTC).
    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(utc + VietnamOffset);

    public static RevenueReportDto Build(DateOnly from, DateOnly to, IReadOnlyList<PaymentRow> payments,
        IReadOnlyList<RefundRow> refunds, IReadOnlyList<SoldLineRow> lines, int top)
    {
        var paidByDay = payments.GroupBy(p => LocalDate(p.PaidAtUtc))
            .ToDictionary(g => g.Key, g => (Amount: g.Sum(p => p.Amount), Orders: g.Select(p => p.OrderId).Distinct().Count()));
        var refundByDay = refunds.GroupBy(r => LocalDate(r.ExecutedAtUtc)).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        // Mỗi ngày trong kỳ đều có một dòng (kể cả ngày không bán) để biểu đồ liên tục.
        var days = new List<DailyRevenueDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            paidByDay.TryGetValue(d, out var paid);
            days.Add(new DailyRevenueDto(d, paid.Amount - refundByDay.GetValueOrDefault(d), paid.Orders));
        }

        var totalPaid = payments.Sum(p => p.Amount);
        var totalRefunded = refunds.Sum(r => r.Amount);
        var topProducts = lines.GroupBy(l => l.ProductName)
            .Select(g => new TopProductDto(g.Key, g.Sum(l => l.Quantity), g.Sum(l => (l.UnitPrice + l.AddonPerUnit) * l.Quantity)))
            .OrderByDescending(p => p.Quantity).ThenByDescending(p => p.Revenue).ThenBy(p => p.ProductName, StringComparer.Ordinal)
            .Take(top).ToList();

        return new RevenueReportDto(from, to, totalPaid, totalRefunded, totalPaid - totalRefunded,
            payments.Select(p => p.OrderId).Distinct().Count(), days, topProducts);
    }
}

// Báo cáo doanh thu (UC15, B4). Quyền MENU_MANAGE được kiểm tra ở lớp Web. Chỉ đọc, không ghi CSDL.
public sealed class StaffReportService(CafeDbContext db)
{
    public const int MaxDays = 366;
    public const int MaxTop = 50;

    private static bool TryDate(string text, out DateOnly value) =>
        DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

    private static AppException Invalid(string message) => new(ApiErrorCodes.Validation, message);

    // from/to dạng yyyy-MM-dd (giờ Việt Nam). Bỏ trống: 7 ngày gần nhất tính đến hôm nay.
    public async Task<RevenueReportDto> RevenueAsync(string? from, string? to, int top, DateTime utcNow, CancellationToken ct = default)
    {
        var toDate = RevenueAggregator.LocalDate(utcNow);
        if (!string.IsNullOrWhiteSpace(to) && !TryDate(to, out toDate)) throw Invalid("Ngày kết thúc phải có dạng yyyy-MM-dd.");
        var fromDate = toDate.AddDays(-6);
        if (!string.IsNullOrWhiteSpace(from) && !TryDate(from, out fromDate)) throw Invalid("Ngày bắt đầu phải có dạng yyyy-MM-dd.");
        if (fromDate.Year < 2000 || toDate.Year > 2100) throw Invalid("Khoảng ngày nằm ngoài phạm vi cho phép.");
        if (toDate < fromDate) throw Invalid("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        if (toDate.DayNumber - fromDate.DayNumber + 1 > MaxDays) throw Invalid($"Khoảng báo cáo tối đa {MaxDays} ngày.");
        if (top < 1 || top > MaxTop) throw Invalid($"Số món bán chạy phải từ 1 đến {MaxTop}.");

        // Biên [fromUtc, toUtc) tương ứng đủ các ngày giờ Việt Nam từ fromDate đến hết toDate.
        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue) - RevenueAggregator.VietnamOffset;
        var toUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue) - RevenueAggregator.VietnamOffset;

        var rawPayments = await db.Payment.AsNoTracking()
            .Where(p => p.Status == "Succeeded" && p.PaidAt != null && p.PaidAt >= fromUtc && p.PaidAt < toUtc)
            .Select(p => new { p.OrderId, p.Amount, p.PaidAt }).ToListAsync(ct);
        var payments = new List<PaymentRow>();
        foreach (var p in rawPayments)
            if (p.PaidAt is DateTime paidAt) payments.Add(new PaymentRow(p.OrderId, p.Amount, paidAt));

        var rawRefunds = await db.RefundTransaction.AsNoTracking()
            .Where(r => r.Status == "Succeeded" && r.ExecutedAt != null && r.ExecutedAt >= fromUtc && r.ExecutedAt < toUtc)
            .Select(r => new { r.Amount, r.ExecutedAt }).ToListAsync(ct);
        var refunds = new List<RefundRow>();
        foreach (var r in rawRefunds)
            if (r.ExecutedAt is DateTime executedAt) refunds.Add(new RefundRow(r.Amount, executedAt));

        // Món bán chạy: dòng của các lượt đã tiếp nhận thuộc những đơn có thu tiền trong kỳ (mỗi đơn tính một lần).
        var orderIds = payments.Select(p => p.OrderId).Distinct().ToList();
        var lines = await (from l in db.OrderLine join r in db.OrderRound on l.RoundId equals r.RoundId
                           where orderIds.Contains(r.OrderId) && r.Status == "Accepted"
                           select new { l.LineId, l.ProductNameSnapshot, l.Quantity, l.UnitPriceSnapshot }).AsNoTracking().ToListAsync(ct);
        var lineIds = lines.Select(l => l.LineId).ToList();
        var addons = await db.OrderLineAddon.AsNoTracking().Where(a => lineIds.Contains(a.LineId))
            .Select(a => new { a.LineId, a.Quantity, a.UnitPriceSnapshot }).ToListAsync(ct);
        var addonPerUnit = addons.GroupBy(a => a.LineId).ToDictionary(g => g.Key, g => g.Sum(a => a.UnitPriceSnapshot * a.Quantity));
        var sold = lines.Select(l => new SoldLineRow(l.ProductNameSnapshot, l.Quantity, l.UnitPriceSnapshot, addonPerUnit.GetValueOrDefault(l.LineId))).ToList();

        return RevenueAggregator.Build(fromDate, toDate, payments, refunds, sold, top);
    }
}
