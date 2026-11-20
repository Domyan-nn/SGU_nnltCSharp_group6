using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Api;

// Báo cáo doanh thu cho quản lý (UC15, B4). Dùng quyền MENU_MANAGE của vai trò Manager. Chỉ đọc.
[ApiController]
[Route("api/staff/reports")]
[Authorize(Policy = Permissions.MenuManage)]
public sealed class StaffReportController(StaffReportService reports) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<RevenueReportDto> Revenue(string? from, string? to, int? top, CancellationToken ct) =>
        await reports.RevenueAsync(from, to, top ?? 10, DateTime.UtcNow, ct);
}
