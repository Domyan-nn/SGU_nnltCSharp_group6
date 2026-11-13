using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace CafeManager.Web.Services;

// Cổng thật cho giao diện khách (thay FakeCustomerGateway của B): gọi dịch vụ máy chủ trong cùng tiến trình.
// Lỗi nghiệp vụ được đổi về GatewayResult với mã trong ApiErrorCodes; tài khoản bị khóa trả mã Forbidden như bản giả.
public sealed class RealCustomerGateway(IServiceScopeFactory scopes) : ICustomerGateway
{
    private static GatewayResult<T> Fail<T>(AppException ex)
    {
        var code = ex.Code == ApiErrorCodes.AccountLocked ? ApiErrorCodes.Forbidden : ex.Code;
        return GatewayResult<T>.Fail(code, ex.Message, ex.PriceChanges);
    }

    public async Task<GatewayResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        using var scope = scopes.CreateScope();
        try
        {
            var session = await scope.ServiceProvider.GetRequiredService<AccountService>().LoginAsync(request);
            return GatewayResult<LoginResponse>.Success(session.ToResponse());
        }
        catch (AppException ex) { return Fail<LoginResponse>(ex); }
    }

    public async Task<GatewayResult<bool>> RegisterAsync(RegisterCustomerRequest request)
    {
        using var scope = scopes.CreateScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<AccountService>().RegisterCustomerAsync(request);
            return GatewayResult<bool>.Success(true);
        }
        catch (AppException ex) { return Fail<bool>(ex); }
    }

    public async Task<GatewayResult<CreateOrderResponse>> CreateOrderAsync(string userName, CreateOrderRequest request)
    {
        using var scope = scopes.CreateScope();
        try
        {
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByNameAsync(userName);
            if (user == null) return GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Unauthorized, "Cần đăng nhập.");
            var response = await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CreateAsync(user.Id, request, DateTime.UtcNow);
            return GatewayResult<CreateOrderResponse>.Success(response);
        }
        catch (AppException ex) { return Fail<CreateOrderResponse>(ex); }
    }

    // --- W03 (B, T6): thêm 3 phương thức cho UC04; dịch vụ CustomerOrderService của A đã kiểm tra chủ đơn. ---
    private static async Task<string?> FindUserIdAsync(IServiceScope scope, string userName) =>
        (await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByNameAsync(userName))?.Id;

    public async Task<GatewayResult<IReadOnlyList<OrderSummaryDto>>> ListOrdersAsync(string userName)
    {
        using var scope = scopes.CreateScope();
        try
        {
            var userId = await FindUserIdAsync(scope, userName);
            if (userId == null) return GatewayResult<IReadOnlyList<OrderSummaryDto>>.Fail(ApiErrorCodes.Unauthorized, "Cần đăng nhập.");
            var list = await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().ListMineAsync(userId);
            return GatewayResult<IReadOnlyList<OrderSummaryDto>>.Success(list);
        }
        catch (AppException ex) { return Fail<IReadOnlyList<OrderSummaryDto>>(ex); }
    }

    public async Task<GatewayResult<OrderDetailDto>> GetOrderAsync(string userName, long orderId)
    {
        using var scope = scopes.CreateScope();
        try
        {
            var userId = await FindUserIdAsync(scope, userName);
            if (userId == null) return GatewayResult<OrderDetailDto>.Fail(ApiErrorCodes.Unauthorized, "Cần đăng nhập.");
            var detail = await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().GetMineAsync(userId, orderId);
            return detail is null
                ? GatewayResult<OrderDetailDto>.Fail(ApiErrorCodes.NotFound, "Không tìm thấy đơn.")
                : GatewayResult<OrderDetailDto>.Success(detail);
        }
        catch (AppException ex) { return Fail<OrderDetailDto>(ex); }
    }

    public async Task<GatewayResult<bool>> CancelOrderAsync(string userName, long orderId)
    {
        using var scope = scopes.CreateScope();
        try
        {
            var userId = await FindUserIdAsync(scope, userName);
            if (userId == null) return GatewayResult<bool>.Fail(ApiErrorCodes.Unauthorized, "Cần đăng nhập.");
            await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CancelAsync(userId, orderId);
            return GatewayResult<bool>.Success(true);
        }
        catch (AppException ex) { return Fail<bool>(ex); }
    }
}

// Thực đơn thật cho MenuSource: đọc CSDL, lưu đệm ngắn (10 giây) để mỗi lần xem trang không phải truy vấn lại.
public sealed class DbMenuSource(IServiceScopeFactory scopes, IMemoryCache cache)
{
    public IReadOnlyList<MenuProductDto> Get() => cache.GetOrCreate("menu", entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MenuQueryService>().GetAsync(DateTime.UtcNow).GetAwaiter().GetResult();
    })!;
}
