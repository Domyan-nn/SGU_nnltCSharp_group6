using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

// Tổng tiền đơn tính từ dòng và tùy chọn đã chốt giá (không lưu tổng trong SalesOrder).
internal static class OrderTotals
{
    // acceptedOnly = true: chỉ tính các lượt đã tiếp nhận, là số tiền thật sự phải thu; false: tính cả lượt nháp (hiển thị cho khách).
    internal static async Task<Dictionary<long, decimal>> ComputeAsync(CafeDbContext db, IReadOnlyCollection<long> orderIds, bool acceptedOnly, CancellationToken ct)
    {
        var lines = await (from l in db.OrderLine join r in db.OrderRound on l.RoundId equals r.RoundId
                           where orderIds.Contains(r.OrderId) && (!acceptedOnly || r.Status == "Accepted")
                           select new { r.OrderId, l.LineId, l.Quantity, l.UnitPriceSnapshot }).AsNoTracking().ToListAsync(ct);
        var lineIds = lines.Select(l => l.LineId).ToList();
        var addons = await db.OrderLineAddon.AsNoTracking().Where(a => lineIds.Contains(a.LineId)).ToListAsync(ct);
        return lines.GroupBy(l => l.OrderId).ToDictionary(g => g.Key, g => g.Sum(l =>
            (l.UnitPriceSnapshot + addons.Where(a => a.LineId == l.LineId).Sum(a => a.UnitPriceSnapshot * a.Quantity)) * l.Quantity));
    }
}
