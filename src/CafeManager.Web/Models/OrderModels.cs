using System.Globalization;
using System.Text.RegularExpressions;
using CafeManager.Core;

namespace CafeManager.Web.Models;

public static class OrderRules
{
    /// <summary>GIẢ ĐỊNH, cần nhóm xác nhận: tài liệu chưa nêu giờ phục vụ. Đổi tại đây.</summary>
    public static readonly TimeSpan OpenFrom = new(7, 0, 0);
    public static readonly TimeSpan OpenTo = new(22, 0, 0);
    public static string ServingHoursText => OpenFrom.ToString(@"hh\:mm") + "–" + OpenTo.ToString(@"hh\:mm");

    /// <summary>Số điện thoại Việt Nam 10 số bắt đầu bằng 0, hoặc +84 và 9 số (giả định, đổi nếu nhóm chốt khác).</summary>
    public static readonly Regex PhoneRegex = new(@"^(0|\+84)\d{9}$");

    // Việt Nam không có giờ mùa hè: dùng độ lệch cố định UTC+7, tránh phụ thuộc tên múi giờ của hệ điều hành.
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public const string LocalFormat = "yyyy-MM-dd'T'HH:mm";

    public static DateTime NowLocal() => DateTime.UtcNow + VietnamOffset;

    /// <summary>Hiển thị thời điểm lưu ở UTC theo giờ Việt Nam (CSDL trả Kind không xác định thì coi là UTC).</summary>
    public static string LocalText(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return (utc + VietnamOffset).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Kiểm tra giờ nhận người dùng nhập (giờ Việt Nam, định dạng datetime-local). Trả lỗi hoặc null; utc là giờ UTC để gửi API.</summary>
    public static string? ValidatePickup(string? text, out DateTime utc)
    {
        utc = default;
        if (!DateTime.TryParseExact(text, new[] { LocalFormat, "yyyy-MM-dd'T'HH:mm:ss" }, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return "Chọn giờ nhận hợp lệ.";
        if (local < NowLocal())
            return "Giờ nhận không được ở quá khứ.";
        var time = local.TimeOfDay;
        if (time < OpenFrom || time > OpenTo)
            return "Giờ nhận phải trong giờ phục vụ (" + ServingHoursText + ").";
        utc = DateTime.SpecifyKind(local - VietnamOffset, DateTimeKind.Utc);
        return null;
    }
}

public static class OrderStatusText
{
    public static string Vietnamese(string status) => status switch
    {
        "Pending" => "Chờ tiếp nhận",
        "Serving" => "Đang phục vụ",
        "Settling" => "Đang thanh toán",
        "Closed" => "Hoàn tất",
        "Cancelled" => "Đã hủy",
        "Rejected" => "Bị từ chối",
        _ => status,
    };

    /// <summary>Lớp CSS cho nhãn trạng thái. Nhãn luôn có chữ; màu chỉ bổ trợ.</summary>
    public static string Css(string status) => (status is "Cancelled" or "Rejected") ? "warn" : "ok";
}

public sealed record PriceChangeView(string Label, decimal OldPrice, decimal NewPrice);

public sealed class CheckoutViewModel
{
    public CartPageModel Cart { get; init; } = new(Array.Empty<CartLineView>());
    public Guid RequestId { get; init; }
    public string PickupAtLocal { get; init; } = "";
    public string MinPickupLocal { get; init; } = "";
    public string ContactName { get; init; } = "";
    public string ContactPhone { get; init; } = "";
    /// <summary>Giá biến thể khách đã thấy (mã dòng giỏ -> giá). Gửi kèm để máy chủ phát hiện giá đổi.</summary>
    public IReadOnlyDictionary<string, decimal> ExpectedPrices { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public string? PickupError { get; init; }
    public string? NameError { get; init; }
    public string? PhoneError { get; init; }
    public IReadOnlyList<PriceChangeView> Changes { get; init; } = Array.Empty<PriceChangeView>();
    public decimal NewSubtotal { get; init; }
}

public sealed record OrderListViewModel(IReadOnlyList<OrderSummaryDto> Items, int Page, int PageCount, int TotalCount, string? Error);
