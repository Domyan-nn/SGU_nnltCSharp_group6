using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Api;

// Đơn của khách (UC02, UC04). Chỉ chủ đơn mới xem và hủy được; đơn người khác trả 404.
[ApiController]
[Route("api/orders")]
[Authorize(Policy = "Customer")]
public sealed class OrdersController(CustomerOrderService orders) : ApiBase
{
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create(CreateOrderRequest request, CancellationToken ct) =>
        StatusCode(201, await orders.CreateAsync(UserId, request, DateTime.UtcNow, ct));

    [HttpGet("mine")]
    public async Task<IReadOnlyList<OrderSummaryDto>> Mine(CancellationToken ct) => await orders.ListMineAsync(UserId, ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDetailDto>> Get(long id, CancellationToken ct) =>
        await orders.GetMineAsync(UserId, id, ct) is { } detail ? detail : NotFound(new ApiError(ApiErrorCodes.NotFound, "Không tìm thấy đơn."));

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await orders.CancelAsync(UserId, id, ct);
        return NoContent();
    }
}
