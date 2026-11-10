namespace CafeManager.Core;

// Persistence models; lifecycle commands belong to server services. Time values are UTC.

public sealed class Category
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class Product
{
    public long ProductId { get; set; }
    public long CategoryId { get; set; }
    public string ProductName { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class ProductVariant
{
    public long VariantId { get; set; }
    public long ProductId { get; set; }
    public string VariantName { get; set; } = "";
    public string VariantType { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class AllowedAddon
{
    public long ProductId { get; set; }
    public long AddonVariantId { get; set; }
    public int MaxPerUnit { get; set; }
}

public sealed class PriceHistory
{
    public long PriceHistoryId { get; set; }
    public long VariantId { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class CafeTable
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = "";
    public string Status { get; set; } = "";
}

public sealed class CustomerProfile
{
    public long CustomerId { get; set; }
    public string IdentityUserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
}

public sealed class ServiceSession
{
    public long SessionId { get; set; }
    public long CurrentTableId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TableTransferHistory
{
    public long TransferId { get; set; }
    public long SessionId { get; set; }
    public long FromTableId { get; set; }
    public long ToTableId { get; set; }
    public DateTime TransferAt { get; set; }
}

public sealed class SalesOrder
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public long? CustomerId { get; set; }
    public long? SessionId { get; set; }
    public string OrderType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpectedPickupAt { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? PickupCode { get; set; }
    public string? RejectReason { get; set; }
    public long? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class OrderRound
{
    public long RoundId { get; set; }
    public long OrderId { get; set; }
    public int RoundNumber { get; set; }
    public string Status { get; set; } = "";
    public DateTime? AcceptedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class OrderLine
{
    public long LineId { get; set; }
    public long RoundId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string VariantNameSnapshot { get; set; } = "";
    public decimal UnitPriceSnapshot { get; set; }
}

public sealed class OrderLineAddon
{
    public long LineAddonId { get; set; }
    public long LineId { get; set; }
    public long AddonVariantId { get; set; }
    public int Quantity { get; set; }
    public string AddonNameSnapshot { get; set; } = "";
    public decimal UnitPriceSnapshot { get; set; }
}

public sealed class PreparationTicket
{
    public long TicketId { get; set; }
    public long RoundId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class PreparationUnit
{
    public long UnitId { get; set; }
    public long TicketId { get; set; }
    public long LineId { get; set; }
    public int UnitNumber { get; set; }
    public string Status { get; set; } = "";
    public long RecipeVersionId { get; set; }
    public long? DeliveredBy { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class PreparationAttempt
{
    public long AttemptId { get; set; }
    public long UnitId { get; set; }
    public int AttemptNo { get; set; }
    public string Outcome { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class PreparationIngredient
{
    public long UnitId { get; set; }
    public long IngredientId { get; set; }
    public decimal QuantitySnapshot { get; set; }
    public decimal QuantityReserved { get; set; }
}

public sealed class Counter
{
    public long CounterId { get; set; }
    public string CounterCode { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class Shift
{
    public long ShiftId { get; set; }
    public long CounterId { get; set; }
    public long OpenedBy { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? CountedCash { get; set; }
    public long? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CloseNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Payment
{
    public long PaymentId { get; set; }
    public string PaymentCode { get; set; } = "";
    public long OrderId { get; set; }
    public long ShiftId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? PaidAt { get; set; }
    public Guid RequestId { get; set; }
    public long CreatedBy { get; set; }
    public string? ExternalReference { get; set; }
}

public sealed class SalesAdjustment
{
    public long AdjustmentId { get; set; }
    public long OrderId { get; set; }
    public decimal AmountSigned { get; set; }
    public string Reason { get; set; } = "";
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class RefundRequest
{
    public long RefundId { get; set; }
    public string RefundCode { get; set; } = "";
    public long PaymentId { get; set; }
    public long? AdjustmentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public long RequestedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RefundTransaction
{
    public long RefundTransactionId { get; set; }
    public long RefundId { get; set; }
    public long ShiftId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ExternalReference { get; set; }
    public Guid RequestId { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public long CreatedBy { get; set; }
}

public sealed class Ingredient
{
    public long IngredientId { get; set; }
    public string IngredientName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class RecipeVersion
{
    public long RecipeVersionId { get; set; }
    public long VariantId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class RecipeDetail
{
    public long RecipeVersionId { get; set; }
    public long IngredientId { get; set; }
    public decimal QuantityRequired { get; set; }
}

public sealed class IngredientBatch
{
    public long BatchId { get; set; }
    public long IngredientId { get; set; }
    public decimal QuantityReceived { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class StockBalance
{
    public long IngredientId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class StockMovement
{
    public long MovementId { get; set; }
    public string MovementCode { get; set; } = "";
    public long IngredientId { get; set; }
    public string MovementType { get; set; } = "";
    public decimal Quantity { get; set; }
    public string SourceType { get; set; } = "";
    public long SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class StockCount
{
    public long CountId { get; set; }
    public long IngredientId { get; set; }
    public decimal BookQty { get; set; }
    public decimal CountedQty { get; set; }
    public DateTime CountedAt { get; set; }
    public long CountedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public string Status { get; set; } = "";
}

public sealed class StaffAccount
{
    public long StaffId { get; set; }
    public string IdentityUserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class Role
{
    public long RoleId { get; set; }
    public string RoleName { get; set; } = "";
}

public sealed class Permission
{
    public long PermissionId { get; set; }
    public string PermissionCode { get; set; } = "";
}

public sealed class StaffRole
{
    public long StaffId { get; set; }
    public long RoleId { get; set; }
}

public sealed class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}

public sealed class AuditEvent
{
    public long AuditId { get; set; }
    public long StaffId { get; set; }
    public string ActionCode { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? EntityReference { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = "";
    public string? ResultReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CancellationRequest
{
    public long CancellationId { get; set; }
    public long OrderId { get; set; }
    public long RequestedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CancellationItem
{
    public long CancellationId { get; set; }
    public long UnitId { get; set; }
}

public sealed class ShiftCashEntry
{
    public long CashEntryId { get; set; }
    public string CashEntryCode { get; set; } = "";
    public long ShiftId { get; set; }
    public string EntryType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}

