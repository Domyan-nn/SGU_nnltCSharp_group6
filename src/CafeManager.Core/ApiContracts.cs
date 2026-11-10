namespace CafeManager.Core;

// Hợp đồng API giữa máy chủ (Web), giao diện Web và Desktop. Xem docs/nhiem-vu/A-ngo-nguyen.md (bảng hợp đồng).
// Quy ước: JSON, tiền VNĐ (decimal), thời gian UTC. Lỗi luôn trả ApiError với mã trạng thái 400/401/403/404/409.
// Đổi hợp đồng sau khi nhóm đã chốt phải báo cả nhóm.

public static class Permissions
{
    public const string MenuManage = "MENU_MANAGE";
    public const string OrderReceive = "ORDER_RECEIVE";
}

public static class UserKinds
{
    public const string Staff = "Staff";
    public const string Customer = "Customer";
}

public static class ApiErrorCodes
{
    public const string Validation = "VALIDATION";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InvalidState = "INVALID_STATE";
    public const string PriceChanged = "PRICE_CHANGED";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string OverlappingInterval = "OVERLAPPING_INTERVAL";
}

public sealed record PriceChange(long VariantId, decimal OldPrice, decimal NewPrice);
public sealed record ApiError(string Code, string Message, IReadOnlyList<PriceChange>? PriceChanges = null);

// --- Xác thực (UC18, UC19) ---
public sealed record LoginRequest(string UserName, string Password);
public sealed record LoginResponse(string UserKind, string DisplayName, IReadOnlyList<string> Permissions);
public sealed record RegisterCustomerRequest(string UserName, string Password, string FullName, string Phone);

// --- Thực đơn (UC01), GET /api/menu ---
public sealed record AddonOptionDto(long AddonVariantId, string Name, decimal UnitPrice, int MaxPerUnit);
public sealed record MenuVariantDto(long VariantId, string VariantName, decimal UnitPrice, IReadOnlyList<AddonOptionDto> Addons);
public sealed record MenuProductDto(long ProductId, string ProductName, long CategoryId, string CategoryName, IReadOnlyList<MenuVariantDto> Variants);

// --- Đơn của khách (UC02, UC04) ---
public sealed record OrderAddonRequest(long AddonVariantId, int Quantity);
public sealed record OrderLineRequest(long VariantId, int Quantity, decimal ExpectedUnitPrice, IReadOnlyList<OrderAddonRequest>? Addons);
public sealed record CreateOrderRequest(Guid RequestId, DateTime PickupAt, string? ContactName, string? ContactPhone,
    IReadOnlyList<OrderLineRequest> Lines);
public sealed record CreateOrderResponse(long OrderId, string OrderCode, string PickupCode, string Status, decimal Total);

public sealed record OrderAddonDto(string Name, int QuantityPerUnit, decimal UnitPrice);
public sealed record OrderLineDto(string ProductName, string VariantName, int Quantity, decimal UnitPrice, IReadOnlyList<OrderAddonDto> Addons);
public sealed record OrderSummaryDto(long OrderId, string OrderCode, string Status, DateTime CreatedAt, DateTime? PickupAt, decimal Total);
public sealed record OrderDetailDto(OrderSummaryDto Summary, string? PickupCode, string? ContactName, string? ContactPhone,
    string? RejectReason, IReadOnlyList<OrderLineDto> Lines);

// --- Thu ngân tiếp nhận đơn Web (UC03) ---
public sealed record StaffOrderDto(OrderDetailDto Detail, long RoundId);
public sealed record AcceptOrderResponse(long TicketId);
public sealed record RejectOrderRequest(string Reason);

// --- Quản lý thực đơn (UC17) ---
public sealed record ProductUpsertRequest(string ProductName, long CategoryId, bool IsActive);
public sealed record VariantUpsertRequest(long ProductId, string VariantName, string VariantType, bool IsActive);
public sealed record SetPriceRequest(decimal UnitPrice, DateTime EffectiveFrom);
public sealed record RecipeLineRequest(long IngredientId, decimal Quantity);
public sealed record SetRecipeRequest(DateTime EffectiveFrom, IReadOnlyList<RecipeLineRequest> Lines);
