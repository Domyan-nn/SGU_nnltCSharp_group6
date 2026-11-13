using CafeManager.Core;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// W03 (UC04): đơn của tôi. Chỉ chủ đơn xem và hủy được; đơn người khác hoặc không tồn tại đều trả 404.
// Mã đơn (số) nằm trong đường dẫn nhưng không phải dữ liệu cá nhân; quyền sở hữu do máy chủ kiểm tra, không dựa vào việc "đoán không ra".
[AutoValidateAntiforgeryToken]
public sealed class MyOrdersController : Controller
{
    public const int PageSize = 10;

    [HttpGet("/don-cua-toi")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData["Nav"] = "orders";
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi"));

        var result = await CustomerGatewayProvider.Current.ListOrdersAsync(me.UserName);
        if (result.Error?.Code == ApiErrorCodes.Unauthorized) return Redirect(CustomerSession.LoginUrl("/don-cua-toi"));
        if (!result.Ok || result.Value is null)
        {
            var message = result.TimedOut ? "Không kết nối được máy chủ. Vui lòng tải lại trang sau." : (result.Error?.Message ?? "Không tải được danh sách đơn.");
            return View(new OrderListViewModel(Array.Empty<OrderSummaryDto>(), 1, 1, 0, message));
        }

        var all = result.Value;
        var pageCount = Math.Max(1, (all.Count + PageSize - 1) / PageSize);
        var current = Math.Clamp(page, 1, pageCount);
        var items = all.Skip((current - 1) * PageSize).Take(PageSize).ToList();
        return View(new OrderListViewModel(items, current, pageCount, all.Count, null));
    }

    [HttpGet("/don-cua-toi/{id:long}")]
    public async Task<IActionResult> Detail(long id)
    {
        ViewData["Nav"] = "orders";
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi/" + id));

        var result = await CustomerGatewayProvider.Current.GetOrderAsync(me.UserName, id);
        var failure = Failure(result, id);
        if (failure is not null) return failure;
        return View(result.Value!);
    }

    // Hộp xác nhận không cần JavaScript: trang riêng nêu rõ đơn nào, kèm nút Xác nhận hủy và Không hủy.
    [HttpGet("/don-cua-toi/{id:long}/huy")]
    public async Task<IActionResult> ConfirmCancel(long id)
    {
        ViewData["Nav"] = "orders";
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi/" + id));

        var result = await CustomerGatewayProvider.Current.GetOrderAsync(me.UserName, id);
        var failure = Failure(result, id);
        if (failure is not null) return failure;

        var detail = result.Value!;
        if (detail.Summary.Status != "Pending")
        {
            TempData["Error"] = "Đơn không còn ở trạng thái chờ tiếp nhận nên không tự hủy được. Vui lòng liên hệ nhân viên.";
            return Redirect("/don-cua-toi/" + id);
        }
        return View(detail);
    }

    [HttpPost("/don-cua-toi/{id:long}/huy")]
    public async Task<IActionResult> Cancel(long id)
    {
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi/" + id));

        var gateway = CustomerGatewayProvider.Current;
        var found = await gateway.GetOrderAsync(me.UserName, id);
        var failure = Failure(found, id);
        if (failure is not null) return failure;
        var code = found.Value!.Summary.OrderCode;

        var result = await gateway.CancelOrderAsync(me.UserName, id);
        if (result.Ok)
        {
            TempData["Message"] = $"Đã hủy đơn {code}.";
            return Redirect("/don-cua-toi/" + id);
        }
        if (result.Error?.Code == ApiErrorCodes.NotFound) return OrderNotFound();
        if (result.Error?.Code == ApiErrorCodes.Unauthorized) return Redirect(CustomerSession.LoginUrl("/don-cua-toi/" + id));

        // INVALID_STATE (đã được tiếp nhận) hoặc CONFLICT (vừa được xử lý): không hủy, nói rõ lý do và hiện trạng thái hiện tại.
        TempData["Error"] = result.TimedOut
            ? "Không kết nối được máy chủ. Đơn chưa được hủy, vui lòng thử lại."
            : (result.Error?.Message ?? "Không hủy được đơn.");
        return Redirect("/don-cua-toi/" + id);
    }

    // Trả null nếu lấy đơn thành công; ngược lại trả trang 404 / chuyển đăng nhập / trang lỗi.
    private IActionResult? Failure(GatewayResult<OrderDetailDto> result, long id)
    {
        if (result.Ok && result.Value is not null) return null;
        if (result.Error?.Code == ApiErrorCodes.NotFound) return OrderNotFound();
        if (result.Error?.Code == ApiErrorCodes.Unauthorized) return Redirect(CustomerSession.LoginUrl("/don-cua-toi/" + id));
        TempData["Error"] = result.TimedOut ? "Không kết nối được máy chủ. Vui lòng thử lại." : (result.Error?.Message ?? "Không tải được đơn.");
        return Redirect("/don-cua-toi");
    }

    private IActionResult OrderNotFound()
    {
        ViewData["Nav"] = "orders";
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("OrderNotFound");
    }
}
