namespace CafeManager.Core;

// Server-side data operations, not complete use cases. Callers must authorize the actor.
public interface ICafeDataCommands
{
    Task<long> AcceptRoundAsync(long roundId, DateTime utcNow, CancellationToken ct = default);
    Task StartUnitAsync(long unitId, DateTime utcNow, CancellationToken ct = default);
    Task<long> RecordCashPaymentAsync(Guid requestId, long orderId, long shiftId, long staffId, decimal amount, CancellationToken ct = default);
    // Thu tiền mặt cho đơn đã tiếp nhận (UC08): tính số còn phải thu trong cùng transaction, ghi đúng số đó, trả tiền thối.
    // Cùng requestId và cùng nội dung trả lại đúng phiếu cũ. Ném ArgumentOutOfRangeException khi tiền đưa chưa đủ,
    // InvalidOperationException khi đơn không còn nợ, ca đã đóng hoặc mã yêu cầu bị dùng cho nội dung khác.
    Task<CashCollection> CollectCashAsync(Guid requestId, long orderId, long shiftId, long staffId, decimal amountReceived, CancellationToken ct = default);
    Task<long> RecordCashRefundAsync(Guid requestId, long refundId, long shiftId, long staffId, CancellationToken ct = default);
    Task<ShiftReconciliation> ReconcileShiftAsync(long shiftId, CancellationToken ct = default);
}
public sealed record CashCollection(long PaymentId, decimal AmountDue, decimal AmountReceived, decimal Change);
public sealed record ShiftReconciliation(decimal OpeningCash, decimal CashPayments, decimal CashRefunds,
    decimal OtherIn, decimal OtherOut, decimal? CountedCash)
{
    public decimal ExpectedCash => OpeningCash + CashPayments - CashRefunds + OtherIn - OtherOut;
    public decimal? Difference => CountedCash - ExpectedCash;
}

