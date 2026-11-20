using CafeManager.Core;

namespace CafeManager.Client;

// Lời gọi API dành cho nhân viên trên Desktop (D02 tiếp nhận đơn, D06 quản lý món, danh mục, nguyên liệu, Thu ngân, Pha chế).
// Tách khỏi giao diện để kiểm thử được mà không cần Windows Forms; mọi thao tác đều đi qua ApiClient (cookie + antiforgery).
public sealed class StaffApi(ApiClient api)
{
    // --- UC03: đơn Web đang chờ ---
    public Task<ApiResult<List<StaffOrderDto>>> PendingOrdersAsync(CancellationToken ct = default) =>
        api.GetAsync<List<StaffOrderDto>>("api/staff/orders?status=Pending", ct);

    // Gọi lại trên đơn đã tiếp nhận trả phiếu cũ (máy chủ chống trùng), nên an toàn để thử lại khi kết quả chưa xác định.
    public Task<ApiResult<AcceptOrderResponse>> AcceptAsync(long orderId, CancellationToken ct = default) =>
        api.PostAsync<AcceptOrderResponse>($"api/staff/orders/{orderId}/accept", null, ct);

    // Từ chối KHÔNG tự thử lại: gọi lần hai trả 409 vì đơn đã đổi trạng thái; khi kết quả chưa xác định phải tải lại danh sách.
    public Task<ApiResult<Empty>> RejectAsync(long orderId, string reason, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/orders/{orderId}/reject", new RejectOrderRequest(reason), ct);

    // --- UC17: thực đơn, giá, công thức ---
    public Task<ApiResult<StaffMenuResponse>> MenuAsync(CancellationToken ct = default) =>
        api.GetAsync<StaffMenuResponse>("api/staff/menu", ct);

    public Task<ApiResult<IdResponse>> SaveProductAsync(long? productId, ProductUpsertRequest request, CancellationToken ct = default) =>
        productId is null
            ? api.PostAsync<IdResponse>("api/staff/products", request, ct)
            : api.PutAsync<IdResponse>($"api/staff/products/{productId}", request, ct);

    public Task<ApiResult<IdResponse>> SaveVariantAsync(long? variantId, VariantUpsertRequest request, CancellationToken ct = default) =>
        variantId is null
            ? api.PostAsync<IdResponse>("api/staff/variants", request, ct)
            : api.PutAsync<IdResponse>($"api/staff/variants/{variantId}", request, ct);

    public Task<ApiResult<Empty>> SetPriceAsync(long variantId, decimal unitPrice, DateTime effectiveFromUtc, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/variants/{variantId}/price", new SetPriceRequest(unitPrice, effectiveFromUtc), ct);

    public Task<ApiResult<Empty>> SetRecipeAsync(long variantId, DateTime effectiveFromUtc, IReadOnlyList<RecipeLineRequest> lines, CancellationToken ct = default) =>
        api.PutAsync<Empty>($"api/staff/variants/{variantId}/recipe", new SetRecipeRequest(effectiveFromUtc, lines), ct);

    public Task<ApiResult<Empty>> DeactivateAsync(long variantId, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/variants/{variantId}/deactivate", null, ct);

    // --- A1: danh mục (C1) và nguyên liệu (C2), quyền MENU_MANAGE ---
    public Task<ApiResult<List<CategoryDto>>> CategoriesAsync(string? search = null, CancellationToken ct = default) =>
        api.GetAsync<List<CategoryDto>>("api/staff/categories" + Search(search), ct);

    public Task<ApiResult<IdResponse>> SaveCategoryAsync(long? categoryId, CategoryUpsertRequest request, CancellationToken ct = default) =>
        categoryId is null
            ? api.PostAsync<IdResponse>("api/staff/categories", request, ct)
            : api.PutAsync<IdResponse>($"api/staff/categories/{categoryId}", request, ct);

    // 409 CONFLICT khi danh mục còn món: báo người dùng, không xóa.
    public Task<ApiResult<Empty>> DeleteCategoryAsync(long categoryId, CancellationToken ct = default) =>
        api.DeleteAsync($"api/staff/categories/{categoryId}", ct);

    public Task<ApiResult<List<IngredientDto>>> IngredientsAsync(string? search = null, CancellationToken ct = default) =>
        api.GetAsync<List<IngredientDto>>("api/staff/ingredients" + Search(search), ct);

    public Task<ApiResult<IdResponse>> SaveIngredientAsync(long? ingredientId, IngredientUpsertRequest request, CancellationToken ct = default) =>
        ingredientId is null
            ? api.PostAsync<IdResponse>("api/staff/ingredients", request, ct)
            : api.PutAsync<IdResponse>($"api/staff/ingredients/{ingredientId}", request, ct);

    // 409 CONFLICT khi nguyên liệu đang nằm trong công thức hoặc đã có phát sinh kho.
    public Task<ApiResult<Empty>> DeleteIngredientAsync(long ingredientId, CancellationToken ct = default) =>
        api.DeleteAsync($"api/staff/ingredients/{ingredientId}", ct);

    // --- A2: ca và thu tiền mặt (màn hình Thu ngân), quyền PAYMENT_COLLECT ---
    public Task<ApiResult<List<CounterDto>>> CountersAsync(CancellationToken ct = default) =>
        api.GetAsync<List<CounterDto>>("api/staff/counters", ct);

    // Luôn trả 200; Shift = null khi nhân viên chưa mở ca.
    public Task<ApiResult<CurrentShiftResponse>> CurrentShiftAsync(CancellationToken ct = default) =>
        api.GetAsync<CurrentShiftResponse>("api/staff/shifts/current", ct);

    // 409 CONFLICT khi quầy đã có ca mở hoặc nhân viên đang có ca khác.
    public Task<ApiResult<ShiftDto>> OpenShiftAsync(long counterId, decimal openingCash, CancellationToken ct = default) =>
        api.PostAsync<ShiftDto>("api/staff/shifts", new OpenShiftRequest(counterId, openingCash), ct);

    public Task<ApiResult<ShiftSummaryDto>> ShiftSummaryAsync(long shiftId, CancellationToken ct = default) =>
        api.GetAsync<ShiftSummaryDto>($"api/staff/shifts/{shiftId}/summary", ct);

    // Trả bảng đối soát đã có tiền đếm thực tế và chênh lệch; 409 INVALID_STATE nếu ca đã đóng.
    public Task<ApiResult<ShiftSummaryDto>> CloseShiftAsync(long shiftId, decimal countedCash, string? closeNote, CancellationToken ct = default) =>
        api.PostAsync<ShiftSummaryDto>($"api/staff/shifts/{shiftId}/close", new CloseShiftRequest(countedCash, closeNote), ct);

    // Đơn đã tiếp nhận còn phải thu (Remaining > 0).
    public Task<ApiResult<List<PayableOrderDto>>> PayableOrdersAsync(CancellationToken ct = default) =>
        api.GetAsync<List<PayableOrderDto>>("api/staff/payments/payable", ct);

    // requestId tạo MỘT lần cho mỗi lần bấm Thu tiền; khi kết quả chưa xác định thì gửi lại cùng requestId,
    // máy chủ trả lại đúng phiếu thu cũ chứ không thu hai lần. 400 VALIDATION nếu tiền khách đưa ít hơn số phải trả.
    public Task<ApiResult<CashPaymentResponse>> PayCashAsync(Guid requestId, long orderId, decimal amountReceived, CancellationToken ct = default) =>
        api.PostAsync<CashPaymentResponse>("api/staff/payments/cash", new CashPaymentRequest(requestId, orderId, amountReceived), ct);

    // --- A3: pha chế (C3), quyền PREPARATION_WORK ---
    // Mặc định trả các ly chưa giao (Waiting, Preparing, Ready), cũ nhất trước.
    public Task<ApiResult<List<PreparationUnitDto>>> PreparationQueueAsync(CancellationToken ct = default) =>
        api.GetAsync<List<PreparationUnitDto>>("api/staff/preparation", ct);

    // Ba thao tác dưới KHÔNG tự thử lại: gọi lần hai trả 409 INVALID_STATE vì ly đã đổi trạng thái; khi đó tải lại danh sách.
    public Task<ApiResult<Empty>> StartUnitAsync(long unitId, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/preparation/{unitId}/start", null, ct);

    public Task<ApiResult<Empty>> MarkReadyAsync(long unitId, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/preparation/{unitId}/ready", null, ct);

    public Task<ApiResult<Empty>> DeliverAsync(long unitId, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/preparation/{unitId}/deliver", null, ct);

    // --- B4: báo cáo doanh thu (quyền MENU_MANAGE). Ngày theo giờ Việt Nam, tối đa 366 ngày, top 1..50. ---
    public Task<ApiResult<RevenueReportDto>> RevenueReportAsync(DateOnly from, DateOnly to, int top = 10, CancellationToken ct = default) =>
        api.GetAsync<RevenueReportDto>("api/staff/reports/revenue?from=" + from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            + "&to=" + to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "&top=" + top, ct);

    // --- A6: nhân viên và phân quyền (màn hình Nhân viên), quyền STAFF_MANAGE ---
    // Danh sách kèm vai trò; IsCurrentUser = true ở dòng của chính người đang đăng nhập.
    public Task<ApiResult<List<StaffAccountDto>>> StaffAccountsAsync(string? search = null, CancellationToken ct = default) =>
        api.GetAsync<List<StaffAccountDto>>("api/staff/accounts" + Search(search), ct);

    public Task<ApiResult<List<RoleDto>>> RolesAsync(CancellationToken ct = default) =>
        api.GetAsync<List<RoleDto>>("api/staff/roles", ct);

    // 409 CONFLICT khi trùng tên đăng nhập. Tạo KHÔNG tự thử lại: gọi lần hai trả 409 nếu lần đầu đã thành công.
    public Task<ApiResult<IdResponse>> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default) =>
        api.PostAsync<IdResponse>("api/staff/accounts", request, ct);

    // Đổi họ tên và thay toàn bộ vai trò. 409 khi làm mất quản trị viên đang hoạt động cuối cùng.
    public Task<ApiResult<IdResponse>> UpdateStaffAsync(long staffId, UpdateStaffRequest request, CancellationToken ct = default) =>
        api.PutAsync<IdResponse>($"api/staff/accounts/{staffId}", request, ct);

    // Khóa/mở an toàn để gọi lại (trả 204 dù đã ở trạng thái đó). 409 khi tự khóa mình hoặc khóa quản trị viên cuối cùng.
    public Task<ApiResult<Empty>> SetStaffActiveAsync(long staffId, bool active, CancellationToken ct = default) =>
        api.PostAsync($"api/staff/accounts/{staffId}/" + (active ? "activate" : "deactivate"), null, ct);

    private static string Search(string? text) =>
        string.IsNullOrWhiteSpace(text) ? "" : "?q=" + Uri.EscapeDataString(text.Trim());
}
