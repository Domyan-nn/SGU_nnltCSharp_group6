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

// Danh mục và nguyên liệu (A1; Desktop C1, C2). Quyền MENU_MANAGE. Xóa trả 409 CONFLICT khi còn tham chiếu.
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Permissions.MenuManage)]
public sealed class StaffCatalogController(StaffCatalogService catalog) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IReadOnlyList<CategoryDto>> Categories(string? q, CancellationToken ct) => await catalog.CategoriesAsync(q, ct);

    [HttpPost("categories")]
    public async Task<ActionResult<IdResponse>> CreateCategory(CategoryUpsertRequest request, CancellationToken ct) =>
        StatusCode(201, new IdResponse(await catalog.SaveCategoryAsync(null, request, ct)));

    [HttpPut("categories/{id:long}")]
    public async Task<IdResponse> UpdateCategory(long id, CategoryUpsertRequest request, CancellationToken ct) =>
        new(await catalog.SaveCategoryAsync(id, request, ct));

    [HttpDelete("categories/{id:long}")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken ct)
    {
        await catalog.DeleteCategoryAsync(id, ct);
        return NoContent();
    }

    [HttpGet("ingredients")]
    public async Task<IReadOnlyList<IngredientDto>> Ingredients(string? q, CancellationToken ct) => await catalog.IngredientsAsync(q, ct);

    [HttpPost("ingredients")]
    public async Task<ActionResult<IdResponse>> CreateIngredient(IngredientUpsertRequest request, CancellationToken ct) =>
        StatusCode(201, new IdResponse(await catalog.SaveIngredientAsync(null, request, ct)));

    [HttpPut("ingredients/{id:long}")]
    public async Task<IdResponse> UpdateIngredient(long id, IngredientUpsertRequest request, CancellationToken ct) =>
        new(await catalog.SaveIngredientAsync(id, request, ct));

    [HttpDelete("ingredients/{id:long}")]
    public async Task<IActionResult> DeleteIngredient(long id, CancellationToken ct)
    {
        await catalog.DeleteIngredientAsync(id, ct);
        return NoContent();
    }
}

// Kho nguyên liệu (UC13; màn hình Kho nguyên liệu). Quyền INVENTORY_WORK cho xem tồn, nhập kho, lập phiếu kiểm kê;
// duyệt hoặc từ chối phiếu cần thêm INVENTORY_APPROVE (hai chính sách cùng phải đạt). Người lập phiếu không tự duyệt.
[ApiController]
[Route("api/staff/inventory")]
[Authorize(Policy = Permissions.InventoryWork)]
public sealed class StaffInventoryController(StaffInventoryService inventory) : ApiBase
{
    [HttpGet]
    public async Task<IReadOnlyList<InventoryItemDto>> Items(string? q, CancellationToken ct) => await inventory.ItemsAsync(q, ct);

    // Nhập kho: cộng tồn, ghi lô và sổ kho; gửi lại cùng RequestId trả phiếu cũ.
    [HttpPost("{ingredientId:long}/receipts")]
    public async Task<ActionResult<StockReceiptResponse>> Receive(long ingredientId, StockReceiptRequest request, CancellationToken ct) =>
        StatusCode(201, await inventory.ReceiveAsync(UserId, ingredientId, request, DateTime.UtcNow, ct));

    [HttpPost("{ingredientId:long}/counts")]
    public async Task<ActionResult<StockCountDto>> CreateCount(long ingredientId, CreateStockCountRequest request, CancellationToken ct) =>
        StatusCode(201, await inventory.CreateCountAsync(UserId, ingredientId, request, DateTime.UtcNow, ct));

    [HttpGet("counts")]
    public async Task<IReadOnlyList<StockCountDto>> Counts(string? status, CancellationToken ct) => await inventory.CountsAsync(status, ct);

    [HttpPost("counts/{countId:long}/approve")]
    [Authorize(Policy = Permissions.InventoryApprove)]
    public async Task<StockCountDto> Approve(long countId, CancellationToken ct) =>
        await inventory.ApproveAsync(UserId, countId, DateTime.UtcNow, ct);

    [HttpPost("counts/{countId:long}/reject")]
    [Authorize(Policy = Permissions.InventoryApprove)]
    public async Task<StockCountDto> Reject(long countId, RejectStockCountRequest request, CancellationToken ct) =>
        await inventory.RejectAsync(UserId, countId, request, DateTime.UtcNow, ct);
}

// Ca và thu tiền mặt (A2; màn hình Thu ngân). Quyền PAYMENT_COLLECT. Thu tiền luôn ghi vào ca đang mở của người đăng nhập.
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Permissions.PaymentCollect)]
public sealed class StaffCashierController(StaffCashierService cashier) : ApiBase
{
    [HttpGet("counters")]
    public async Task<IReadOnlyList<CounterDto>> Counters(CancellationToken ct) => await cashier.CountersAsync(ct);

    [HttpGet("shifts/current")]
    public async Task<CurrentShiftResponse> CurrentShift(CancellationToken ct) => await cashier.CurrentShiftAsync(UserId, ct);

    [HttpPost("shifts")]
    public async Task<ActionResult<ShiftDto>> OpenShift(OpenShiftRequest request, CancellationToken ct) =>
        StatusCode(201, await cashier.OpenShiftAsync(UserId, request, DateTime.UtcNow, ct));

    [HttpGet("shifts/{id:long}/summary")]
    public async Task<ShiftSummaryDto> Summary(long id, CancellationToken ct) => await cashier.ShiftSummaryAsync(UserId, id, ct);

    [HttpPost("shifts/{id:long}/close")]
    public async Task<ShiftSummaryDto> Close(long id, CloseShiftRequest request, CancellationToken ct) =>
        await cashier.CloseShiftAsync(UserId, id, request, DateTime.UtcNow, ct);

    [HttpGet("payments/payable")]
    public async Task<IReadOnlyList<PayableOrderDto>> Payable(CancellationToken ct) => await cashier.PayableOrdersAsync(ct);

    [HttpPost("payments/cash")]
    public async Task<ActionResult<CashPaymentResponse>> PayCash(CashPaymentRequest request, CancellationToken ct) =>
        StatusCode(201, await cashier.PayCashAsync(UserId, request, ct));
}

// Pha chế (A3; màn hình Pha chế). Quyền PREPARATION_WORK. Mỗi ly đi một nấc: Waiting -> Preparing -> Ready -> Delivered.
[ApiController]
[Route("api/staff/preparation")]
[Authorize(Policy = Permissions.PreparationWork)]
public sealed class StaffPreparationController(StaffPreparationService preparation) : ApiBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PreparationUnitDto>> Queue(CancellationToken ct) => await preparation.QueueAsync(ct);

    [HttpPost("{unitId:long}/start")]
    public async Task<IActionResult> Start(long unitId, CancellationToken ct)
    {
        await preparation.StartAsync(UserId, unitId, DateTime.UtcNow, ct);
        return NoContent();
    }

    [HttpPost("{unitId:long}/ready")]
    public async Task<IActionResult> Ready(long unitId, CancellationToken ct)
    {
        await preparation.MarkReadyAsync(UserId, unitId, ct);
        return NoContent();
    }

    [HttpPost("{unitId:long}/deliver")]
    public async Task<IActionResult> Deliver(long unitId, CancellationToken ct)
    {
        await preparation.DeliverAsync(UserId, unitId, DateTime.UtcNow, ct);
        return NoContent();
    }
}

// Nhân viên và phân quyền (A6, UC16; màn hình Nhân viên). Quyền STAFF_MANAGE, kiểm tra trong CSDL ở mỗi yêu cầu.
// Không có thao tác xóa: nhân viên chỉ bị khóa (deactivate) hoặc mở lại (activate).
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Permissions.StaffManage)]
public sealed class StaffAccountsController(StaffAccountService accounts) : ApiBase
{
    [HttpGet("accounts")]
    public async Task<IReadOnlyList<StaffAccountDto>> List(string? q, CancellationToken ct) => await accounts.ListAsync(UserId, q, ct);

    [HttpGet("roles")]
    public async Task<IReadOnlyList<RoleDto>> Roles(CancellationToken ct) => await accounts.RolesAsync(ct);

    [HttpPost("accounts")]
    public async Task<ActionResult<IdResponse>> Create(CreateStaffRequest request, CancellationToken ct) =>
        StatusCode(201, new IdResponse(await accounts.CreateAsync(UserId, request, DateTime.UtcNow, ct)));

    [HttpPut("accounts/{id:long}")]
    public async Task<IdResponse> Update(long id, UpdateStaffRequest request, CancellationToken ct)
    {
        await accounts.UpdateAsync(UserId, id, request, DateTime.UtcNow, ct);
        return new IdResponse(id);
    }

    [HttpPost("accounts/{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        await accounts.SetActiveAsync(UserId, id, true, DateTime.UtcNow, ct);
        return NoContent();
    }

    [HttpPost("accounts/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await accounts.SetActiveAsync(UserId, id, false, DateTime.UtcNow, ct);
        return NoContent();
    }
}
