namespace CafeManager.Core;

// Server-side data operations, not complete use cases. Callers must authorize the actor.
public interface ICafeDataCommands
{
    Task<long> AcceptRoundAsync(long roundId, DateTime utcNow, CancellationToken ct = default);
    Task StartUnitAsync(long unitId, DateTime utcNow, CancellationToken ct = default);
    Task<long> RecordCashPaymentAsync(Guid requestId, long orderId, long shiftId, long staffId, decimal amount, CancellationToken ct = default);
    Task<long> RecordCashRefundAsync(Guid requestId, long refundId, long shiftId, long staffId, CancellationToken ct = default);
    Task<ShiftReconciliation> ReconcileShiftAsync(long shiftId, CancellationToken ct = default);
}
public sealed record ShiftReconciliation(decimal OpeningCash, decimal CashPayments, decimal CashRefunds,
    decimal OtherIn, decimal OtherOut, decimal? CountedCash)
{
    public decimal ExpectedCash => OpeningCash + CashPayments - CashRefunds + OtherIn - OtherOut;
    public decimal? Difference => CountedCash - ExpectedCash;
}

