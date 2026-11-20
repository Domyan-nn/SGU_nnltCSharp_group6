namespace CafeManager.Core;

// Hợp đồng API giữa máy chủ (Web), giao diện Web và Desktop. Xem docs/hop-dong-api.md (bảng hợp đồng).
// Quy ước: JSON, tiền VNĐ (decimal), thời gian UTC. Lỗi luôn trả ApiError với mã trạng thái 400/401/403/404/409.
// Đổi hợp đồng sau khi nhóm đã chốt phải báo cả nhóm.

public static class Permissions
{
    public const string MenuManage = "MENU_MANAGE";
    public const string OrderReceive = "ORDER_RECEIVE";
    public const string PaymentCollect = "PAYMENT_COLLECT";     // mở/đóng ca, thu tiền mặt (Thu ngân)
    public const string PreparationWork = "PREPARATION_WORK";   // màn hình Pha chế
    public const string StaffManage = "STAFF_MANAGE";           // quản lý nhân viên và gán vai trò (UC16)

    public static readonly IReadOnlyList<string> All = [MenuManage, OrderReceive, PaymentCollect, PreparationWork, StaffManage];
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
    public const string AccountLocked = "ACCOUNT_LOCKED";
}

public sealed record PriceChange(long VariantId, decimal OldPrice, decimal NewPrice);
public sealed record ApiError(string Code, string Message, IReadOnlyList<PriceChange>? PriceChanges = null);

// Lỗi nghiệp vụ có mã (ApiErrorCodes) để lớp Web đổi thành mã HTTP; không chứa chi tiết nội bộ.
public sealed class AppException(string code, string message, IReadOnlyList<PriceChange>? priceChanges = null) : Exception(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<PriceChange>? PriceChanges { get; } = priceChanges;
}

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
    string? RejectReason, IReadOnlyList<OrderLineDto> Lines, OrderProgressDto? Progress = null);

// Tiến độ pha chế và số đã trả của một đơn (B2 hiển thị). Null khi máy chủ chưa có dữ liệu này.
public sealed record OrderProgressDto(int Waiting, int Preparing, int Ready, int Delivered, decimal Paid);

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

// --- CSRF cho client dùng cookie ---
public sealed record AntiforgeryTokenResponse(string Token);

// --- Danh mục quản trị thực đơn (UC17) ---
public sealed record CategoryLookupDto(long CategoryId, string CategoryName);
public sealed record IngredientLookupDto(long IngredientId, string IngredientName, string UnitName);

public sealed record StaffRecipeLineDto(long IngredientId, string IngredientName, string UnitName, decimal Quantity);

public sealed record StaffVariantDto(
    long VariantId,
    string VariantName,
    string VariantType,
    bool IsActive,
    decimal? CurrentPrice,
    DateTime? PriceEffectiveFrom,
    IReadOnlyList<StaffRecipeLineDto> CurrentRecipe);

public sealed record StaffProductDto(
    long ProductId,
    string ProductName,
    long CategoryId,
    string CategoryName,
    bool IsActive,
    IReadOnlyList<StaffVariantDto> Variants);

public sealed record StaffMenuResponse(
    IReadOnlyList<StaffProductDto> Products,
    IReadOnlyList<CategoryLookupDto> Categories,
    IReadOnlyList<IngredientLookupDto> Ingredients);

// Kết quả tạo mới (món, biến thể) trong quản lý thực đơn.
public sealed record IdResponse(long Id);

// --- Danh mục và nguyên liệu (A1; Desktop C1, C2). Quyền MENU_MANAGE ---
// Xóa trả 409 CONFLICT khi danh mục còn món / nguyên liệu đang nằm trong công thức hoặc đã có phát sinh kho.
public sealed record CategoryDto(long CategoryId, string CategoryName, bool IsActive, int ProductCount);
public sealed record CategoryUpsertRequest(string CategoryName, bool IsActive);

public sealed record IngredientDto(long IngredientId, string IngredientName, string UnitName, bool IsActive,
    decimal QuantityOnHand, decimal QuantityReserved, int RecipeCount);
public sealed record IngredientUpsertRequest(string IngredientName, string UnitName, bool IsActive);

// --- Ca và thu tiền mặt (A2; Desktop A4 Thu ngân). Quyền PAYMENT_COLLECT ---
// Thu tiền luôn ghi vào ca đang mở của chính nhân viên đăng nhập, nên client không gửi mã ca.
public sealed record CounterDto(long CounterId, string CounterCode);
public sealed record ShiftDto(long ShiftId, long CounterId, string CounterCode, string OpenedByName, DateTime OpenedAt,
    decimal OpeningCash, DateTime? ClosedAt, decimal? CountedCash);
public sealed record CurrentShiftResponse(ShiftDto? Shift);
public sealed record OpenShiftRequest(long CounterId, decimal OpeningCash);
public sealed record CloseShiftRequest(decimal CountedCash, string? CloseNote);
public sealed record ShiftSummaryDto(long ShiftId, decimal OpeningCash, decimal CashPayments, decimal CashRefunds,
    decimal OtherIn, decimal OtherOut, decimal ExpectedCash, decimal? CountedCash, decimal? Difference);

public sealed record PayableOrderDto(long OrderId, string OrderCode, string? ContactName, string? PickupCode,
    string Status, decimal Total, decimal Paid, decimal Remaining);
public sealed record CashPaymentRequest(Guid RequestId, long OrderId, decimal AmountReceived);
public sealed record CashPaymentResponse(long PaymentId, decimal AmountDue, decimal AmountReceived, decimal Change);

// --- Pha chế (A3; Desktop C3). Quyền PREPARATION_WORK ---
// Mỗi ly là một PreparationUnit: Waiting -> Preparing -> Ready -> Delivered.
public static class PreparationStatuses
{
    public const string Waiting = "Waiting";
    public const string Preparing = "Preparing";
    public const string Ready = "Ready";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}

public sealed record PreparationUnitDto(long UnitId, long TicketId, long OrderId, string OrderCode, string? PickupCode,
    string ProductName, string VariantName, IReadOnlyList<OrderAddonDto> Addons, int UnitNumber, int LineQuantity,
    string Status, DateTime AcceptedAt);

// --- Nhân viên và phân quyền (A6, UC16; Desktop). Quyền STAFF_MANAGE ---
// Quyền thật của nhân viên là hợp các quyền của những vai trò được gán (StaffRole -> RolePermission). Không xóa nhân viên vì đã có
// chứng từ tham chiếu; chỉ khóa (IsActive = false) và mở lại. Khóa có hiệu lực ngay ở yêu cầu kế tiếp của phiên đang mở.
// 409 CONFLICT: trùng tên đăng nhập; tự khóa tài khoản của mình; làm mất người quản trị (STAFF_MANAGE) đang hoạt động cuối cùng.
public sealed record RoleDto(long RoleId, string RoleName, IReadOnlyList<string> Permissions);
public sealed record StaffAccountDto(long StaffId, string UserName, string FullName, bool IsActive,
    IReadOnlyList<long> RoleIds, IReadOnlyList<string> RoleNames, bool IsCurrentUser);
public sealed record CreateStaffRequest(string UserName, string Password, string FullName, IReadOnlyList<long> RoleIds);
public sealed record UpdateStaffRequest(string FullName, IReadOnlyList<long> RoleIds);

// --- Báo cáo doanh thu (B4; Desktop). Quyền MENU_MANAGE (quản lý) ---
// Doanh thu thuần = tiền đã thu thành công (Payment.Succeeded, theo PaidAt) trừ tiền đã hoàn thành công (RefundTransaction.Succeeded,
// theo ExecutedAt), gom theo NGÀY giờ Việt Nam (UTC+7). Món bán chạy tính trên các đơn có thu tiền trong kỳ, chỉ các lượt đã tiếp nhận.
public sealed record DailyRevenueDto(DateOnly Date, decimal Revenue, int Orders);
public sealed record TopProductDto(string ProductName, int Quantity, decimal Revenue);
public sealed record RevenueReportDto(DateOnly From, DateOnly To, decimal Paid, decimal Refunded, decimal Net, int PaidOrders,
    IReadOnlyList<DailyRevenueDto> Days, IReadOnlyList<TopProductDto> TopProducts);
