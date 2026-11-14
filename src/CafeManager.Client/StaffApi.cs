using CafeManager.Core;

namespace CafeManager.Client;

// Lời gọi API dành cho nhân viên trên Desktop (D02 tiếp nhận đơn, D06 quản lý món).
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
}
