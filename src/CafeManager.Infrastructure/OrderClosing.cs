using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

// Đóng đơn (Serving -> Closed) khi đã thu đủ tiền và đã giao đủ ly.
// Gọi sau khi thu tiền và sau khi giao ly; gọi lại nhiều lần vẫn cho cùng kết quả.
internal static class OrderClosing
{
    internal static async Task<bool> TryCloseAsync(CafeDbContext db, long orderId, CancellationToken ct)
    {
        var order = await db.SalesOrder.SingleOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order is null || order.Status != "Serving") return false;

        var total = (await OrderTotals.ComputeAsync(db, [orderId], acceptedOnly: true, ct)).GetValueOrDefault(orderId);
        var paid = await db.Payment.Where(p => p.OrderId == orderId && p.Status == "Succeeded")
                                   .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        if (total <= 0 || paid < total) return false;

        var unitStatuses = await (from u in db.PreparationUnit
                                  join l in db.OrderLine on u.LineId equals l.LineId
                                  join r in db.OrderRound on l.RoundId equals r.RoundId
                                  where r.OrderId == orderId && r.Status == "Accepted"
                                  select u.Status).ToListAsync(ct);
        var live = unitStatuses.Where(s => s != PreparationStatuses.Cancelled).ToList();
        if (live.Count == 0 || live.Any(s => s != PreparationStatuses.Delivered)) return false;

        order.Status = "Closed";
        await db.SaveChangesAsync(ct);   // RowVersion bảo vệ khi hai thao tác cùng lúc
        return true;
    }
}
