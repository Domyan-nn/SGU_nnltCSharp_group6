using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Api;

// Thu ngân tiếp nhận hoặc từ chối đơn Web (UC03). Quyền ORDER_RECEIVE được kiểm tra trong CSDL ở mỗi yêu cầu.
[ApiController]
[Route("api/staff/orders")]
[Authorize(Policy = Permissions.OrderReceive)]
public sealed class StaffOrdersController(StaffOrderService staff) : ApiBase
{
    [HttpGet]
    public async Task<IReadOnlyList<StaffOrderDto>> List(string? status, CancellationToken ct) => await staff.ListAsync(status, ct);

    [HttpPost("{id:long}/accept")]
    public async Task<AcceptOrderResponse> Accept(long id, CancellationToken ct) => await staff.AcceptAsync(UserId, id, DateTime.UtcNow, ct);

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, RejectOrderRequest request, CancellationToken ct)
    {
        await staff.RejectAsync(UserId, id, request?.Reason, DateTime.UtcNow, ct);
        return NoContent();
    }
}

// Quản lý thực đơn, giá và công thức (UC17). Quyền MENU_MANAGE.
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Permissions.MenuManage)]
public sealed class StaffMenuController(StaffMenuService menu) : ControllerBase
{
    [HttpGet("menu")]
    public async Task<StaffMenuResponse> Get(CancellationToken ct) => await menu.GetAsync(DateTime.UtcNow, ct);

    [HttpPost("products")]
    public async Task<ActionResult<IdResponse>> CreateProduct(ProductUpsertRequest request, CancellationToken ct) =>
        StatusCode(201, new IdResponse(await menu.UpsertProductAsync(null, request, ct)));

    [HttpPut("products/{id:long}")]
    public async Task<IdResponse> UpdateProduct(long id, ProductUpsertRequest request, CancellationToken ct) =>
        new(await menu.UpsertProductAsync(id, request, ct));

    [HttpPost("variants")]
    public async Task<ActionResult<IdResponse>> CreateVariant(VariantUpsertRequest request, CancellationToken ct) =>
        StatusCode(201, new IdResponse(await menu.UpsertVariantAsync(null, request, DateTime.UtcNow, ct)));

    [HttpPut("variants/{id:long}")]
    public async Task<IdResponse> UpdateVariant(long id, VariantUpsertRequest request, CancellationToken ct) =>
        new(await menu.UpsertVariantAsync(id, request, DateTime.UtcNow, ct));

    [HttpPost("variants/{id:long}/price")]
    public async Task<IActionResult> SetPrice(long id, SetPriceRequest request, CancellationToken ct)
    {
        await menu.SetPriceAsync(id, request, DateTime.UtcNow, ct);
        return NoContent();
    }

    [HttpPut("variants/{id:long}/recipe")]
    public async Task<IActionResult> SetRecipe(long id, SetRecipeRequest request, CancellationToken ct)
    {
        await menu.SetRecipeAsync(id, request, DateTime.UtcNow, ct);
        return NoContent();
    }

    [HttpPost("variants/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await menu.DeactivateVariantAsync(id, ct);
        return NoContent();
    }
}
