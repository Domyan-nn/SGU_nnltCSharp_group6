using CafeManager.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Web.Services;

// Đổi lỗi nghiệp vụ (AppException) và lỗi không lường trước của /api/* thành ApiError + mã HTTP đúng hợp đồng.
// Không trả chi tiết nội bộ (ngăn xếp, câu SQL) ra ngoài; lỗi lạ được ghi log ở máy chủ.
public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> log) : IExceptionFilter
{
    public static int StatusFor(string code) => code switch
    {
        ApiErrorCodes.Validation => 400,
        ApiErrorCodes.Unauthorized => 401,
        ApiErrorCodes.Forbidden or ApiErrorCodes.AccountLocked => 403,
        ApiErrorCodes.NotFound => 404,
        ApiErrorCodes.Conflict or ApiErrorCodes.InvalidState or ApiErrorCodes.PriceChanged
            or ApiErrorCodes.InsufficientStock or ApiErrorCodes.OverlappingInterval => 409,
        _ => 400,
    };

    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api")) return;
        switch (context.Exception)
        {
            case AppException ex:
                context.Result = new ObjectResult(new ApiError(ex.Code, ex.Message, ex.PriceChanges)) { StatusCode = StatusFor(ex.Code) };
                break;
            case DbUpdateConcurrencyException:
                context.Result = new ObjectResult(new ApiError(ApiErrorCodes.Conflict, "Dữ liệu vừa được thay đổi ở nơi khác, vui lòng tải lại.")) { StatusCode = 409 };
                break;
            case OperationCanceledException:
                return;
            default:
                log.LogError(context.Exception, "Lỗi không xử lý ở {Path}", context.HttpContext.Request.Path);
                context.Result = new ObjectResult(new ApiError("SERVER_ERROR",
                    "Máy chủ gặp lỗi. Chưa xác định thao tác đã được xử lý hay chưa; hãy tra cứu lại trước khi gửi lại.")) { StatusCode = 500 };
                break;
        }
        context.ExceptionHandled = true;
    }
}
