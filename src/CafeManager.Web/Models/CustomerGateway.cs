using System.Globalization;
using System.Text.RegularExpressions;
using CafeManager.Core;
using Microsoft.AspNetCore.Identity;

namespace CafeManager.Web.Models;

// Cổng gọi nghiệp vụ khách (đăng ký, đăng nhập, gửi đơn) theo hợp đồng API trong Core/ApiContracts.cs.
// Hiện dùng bản GIẢ (FakeCustomerGateway, bộ nhớ) vì API của A chưa giao. Khi A giao, viết một bản thật
// (gọi dịch vụ máy chủ của A) và gán vào CustomerGatewayProvider.Current; Controller/View không phải sửa.

public sealed record GatewayResult<T>(T? Value, ApiError? Error, bool TimedOut)
{
    public bool Ok => Error is null && !TimedOut;

    public static GatewayResult<T> Success(T value) => new(value, null, false);

    public static GatewayResult<T> Fail(string code, string message, IReadOnlyList<PriceChange>? changes = null) =>
        new(default, new ApiError(code, message, changes), false);

    public static GatewayResult<T> Timeout() => new(default, null, true);
}

public interface ICustomerGateway
{
    Task<GatewayResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<GatewayResult<bool>> RegisterAsync(RegisterCustomerRequest request);
    Task<GatewayResult<CreateOrderResponse>> CreateOrderAsync(string userName, CreateOrderRequest request);

    // UC04 (W03): chỉ thao tác trên đơn của chính khách; đơn người khác hoặc không tồn tại đều trả NOT_FOUND.
    Task<GatewayResult<IReadOnlyList<OrderSummaryDto>>> ListOrdersAsync(string userName);
    Task<GatewayResult<OrderDetailDto>> GetOrderAsync(string userName, long orderId);
    Task<GatewayResult<bool>> CancelOrderAsync(string userName, long orderId);
}

public static class CustomerGatewayProvider
{
    public static ICustomerGateway Current { get; set; } = new FakeCustomerGateway();
}

/// <summary>
/// Bản giả để dựng và thử giao diện; KHÔNG phải bằng chứng cho KT. Hành vi mô phỏng máy chủ:
/// tính giá lại từ thực đơn, chống trùng theo RequestId, trả 409 PRICE_CHANGED.
/// Biến môi trường để thử các nhánh khó (đặt trước khi chạy Web):
///   CAFE_FAKE_PRICE_DELTA=1000   máy chủ tính giá cao hơn thực đơn 1.000 đ  -> lần gửi đầu bị 409 PRICE_CHANGED
///   CAFE_FAKE_TIMEOUT_ONCE=1     đơn đầu tiên được tạo nhưng phản hồi bị "mất" -> hiện Tra cứu kết quả
/// Tài khoản có tên bắt đầu bằng "locked." (đăng ký như bình thường) bị coi là khóa khi đăng nhập.
/// </summary>
public sealed class FakeCustomerGateway : ICustomerGateway
{
    private sealed record FakeUser(string UserName, string PasswordHash, string FullName, string Phone);

    private static readonly object Gate = new();
    private static readonly Dictionary<string, FakeUser> Users = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Guid, CreateOrderResponse> OrdersByRequest = new();
    private sealed record FakeOrder(string Owner, OrderDetailDto Detail);
    private static readonly Dictionary<long, FakeOrder> OrdersById = new();
    private static readonly PasswordHasher<string> Hasher = new();
    private static readonly Regex UserNameRegex = new(@"^[A-Za-z0-9._-]+$");
    private static int _orderSeq;
    private static bool _timeoutUsed;

    public Task<GatewayResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        lock (Gate)
        {
            if (!Users.TryGetValue(request.UserName, out var user) ||
                Hasher.VerifyHashedPassword(user.UserName, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Task.FromResult(GatewayResult<LoginResponse>.Fail(ApiErrorCodes.Unauthorized, "Tên đăng nhập hoặc mật khẩu không đúng."));
            if (user.UserName.StartsWith("locked.", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(GatewayResult<LoginResponse>.Fail(ApiErrorCodes.Forbidden, "Tài khoản đã bị khóa."));
            return Task.FromResult(GatewayResult<LoginResponse>.Success(
                new LoginResponse(UserKinds.Customer, user.FullName, Array.Empty<string>())));
        }
    }

    public Task<GatewayResult<bool>> RegisterAsync(RegisterCustomerRequest request)
    {
        var userName = request.UserName.Trim();
        if (userName.Length < 3 || userName.Length > 50 || !UserNameRegex.IsMatch(userName))
            return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.Validation, "Tên đăng nhập 3 đến 50 ký tự gồm chữ, số, dấu chấm, gạch dưới hoặc gạch nối."));
        if (request.Password.Length < 6)
            return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.Validation, "Mật khẩu tối thiểu 6 ký tự."));
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.Validation, "Nhập họ tên."));
        if (!OrderRules.PhoneRegex.IsMatch(request.Phone))
            return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.Validation, "Số điện thoại không hợp lệ."));

        lock (Gate)
        {
            if (Users.ContainsKey(userName))
                return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.Conflict, "Tên đăng nhập đã được dùng."));
            Users[userName] = new FakeUser(userName, Hasher.HashPassword(userName, request.Password), request.FullName.Trim(), request.Phone);
            return Task.FromResult(GatewayResult<bool>.Success(true));
        }
    }

    private static decimal PriceDelta() =>
        decimal.TryParse(Environment.GetEnvironmentVariable("CAFE_FAKE_PRICE_DELTA"), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var d) ? d : 0m;

    public Task<GatewayResult<CreateOrderResponse>> CreateOrderAsync(string userName, CreateOrderRequest request)
    {
        lock (Gate)
        {
            if (!Users.ContainsKey(userName))
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Unauthorized, "Cần đăng nhập."));
            if (OrdersByRequest.TryGetValue(request.RequestId, out var existing))
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Success(existing));
            if (request.Lines.Count == 0)
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Validation, "Đơn không có món."));
            if (request.PickupAt.ToUniversalTime() < DateTime.UtcNow)
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Validation, "Giờ nhận không được ở quá khứ."));

            var delta = PriceDelta();
            var menu = MenuSource.Current;
            var total = 0m;
            var changes = new List<PriceChange>();
            var detailLines = new List<OrderLineDto>();
            foreach (var line in request.Lines)
            {
                var product = menu.FirstOrDefault(p => p.Variants.Any(v => v.VariantId == line.VariantId));
                var variant = product?.Variants.FirstOrDefault(v => v.VariantId == line.VariantId);
                if (product is null || variant is null)
                    return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Validation, "Có món không còn bán."));
                if (line.Quantity < 1)
                    return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Validation, "Số lượng phải là số nguyên dương."));

                var serverPrice = variant.UnitPrice + delta;
                if (serverPrice != line.ExpectedUnitPrice && changes.All(c => c.VariantId != variant.VariantId))
                    changes.Add(new PriceChange(variant.VariantId, line.ExpectedUnitPrice, serverPrice));

                var addonTotal = 0m;
                var addonDtos = new List<OrderAddonDto>();
                foreach (var a in line.Addons ?? Array.Empty<OrderAddonRequest>())
                {
                    var option = variant.Addons.FirstOrDefault(o => o.AddonVariantId == a.AddonVariantId);
                    if (option is null || a.Quantity < 0 || a.Quantity > option.MaxPerUnit)
                        return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.Validation, "Tùy chọn thêm không hợp lệ."));
                    addonTotal += option.UnitPrice * a.Quantity;
                    if (a.Quantity > 0) addonDtos.Add(new OrderAddonDto(option.Name, a.Quantity, option.UnitPrice));
                }
                total += (serverPrice + addonTotal) * line.Quantity;
                detailLines.Add(new OrderLineDto(product.ProductName, variant.VariantName, line.Quantity, serverPrice, addonDtos));
            }
            if (changes.Count > 0)
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Fail(ApiErrorCodes.PriceChanged, "Giá đã thay đổi.", changes));

            var seq = ++_orderSeq;
            var response = new CreateOrderResponse(seq, $"WEB-{DateTime.UtcNow:yyyyMMdd}-{seq:D4}",
                Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture), "Pending", total);
            OrdersByRequest[request.RequestId] = response;
            // CAFE_FAKE_ORDER_STATUS=Serving tạo đơn ở trạng thái đã tiếp nhận để thử "đơn đã tiếp nhận không tự hủy được".
            var status = Environment.GetEnvironmentVariable("CAFE_FAKE_ORDER_STATUS") is { Length: > 0 } custom ? custom : "Pending";
            var summary = new OrderSummaryDto(seq, response.OrderCode, status, DateTime.UtcNow, request.PickupAt, total);
            OrdersById[seq] = new FakeOrder(userName, new OrderDetailDto(summary, response.PickupCode,
                request.ContactName, request.ContactPhone, null, detailLines));

            if (!_timeoutUsed && Environment.GetEnvironmentVariable("CAFE_FAKE_TIMEOUT_ONCE") == "1")
            {
                _timeoutUsed = true; // đơn đã tạo nhưng phản hồi bị mất
                return Task.FromResult(GatewayResult<CreateOrderResponse>.Timeout());
            }
            return Task.FromResult(GatewayResult<CreateOrderResponse>.Success(response));
        }
    }

    public Task<GatewayResult<IReadOnlyList<OrderSummaryDto>>> ListOrdersAsync(string userName)
    {
        lock (Gate)
        {
            IReadOnlyList<OrderSummaryDto> list = OrdersById.Values.Where(o => o.Owner == userName)
                .Select(o => o.Detail.Summary).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.OrderId).ToList();
            return Task.FromResult(GatewayResult<IReadOnlyList<OrderSummaryDto>>.Success(list));
        }
    }

    public Task<GatewayResult<OrderDetailDto>> GetOrderAsync(string userName, long orderId)
    {
        lock (Gate)
        {
            if (!OrdersById.TryGetValue(orderId, out var order) || order.Owner != userName)
                return Task.FromResult(GatewayResult<OrderDetailDto>.Fail(ApiErrorCodes.NotFound, "Không tìm thấy đơn."));
            return Task.FromResult(GatewayResult<OrderDetailDto>.Success(order.Detail));
        }
    }

    public Task<GatewayResult<bool>> CancelOrderAsync(string userName, long orderId)
    {
        lock (Gate)
        {
            if (!OrdersById.TryGetValue(orderId, out var order) || order.Owner != userName)
                return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.NotFound, "Không tìm thấy đơn."));
            if (order.Detail.Summary.Status != "Pending")
                return Task.FromResult(GatewayResult<bool>.Fail(ApiErrorCodes.InvalidState,
                    "Đơn đã được tiếp nhận hoặc đã đóng, không thể tự hủy. Vui lòng liên hệ nhân viên."));
            OrdersById[orderId] = order with { Detail = order.Detail with { Summary = order.Detail.Summary with { Status = "Cancelled" } } };
            return Task.FromResult(GatewayResult<bool>.Success(true));
        }
    }
}
