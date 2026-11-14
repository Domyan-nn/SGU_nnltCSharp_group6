using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CafeManager.Core;

namespace CafeManager.Client;

// Logic hiển thị và kiểm tra đầu vào cho D02/D06, tách khỏi Windows Forms để kiểm thử được.

// Hiển thị: giờ Việt Nam cố định UTC+7 (cùng giả định với máy chủ và giao diện Web); tiền VNĐ.
public static class Display
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    // API trả thời điểm UTC; Kind không xác định (đọc từ CSDL) được coi là UTC.
    public static DateTime ToVietnam(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc + VietnamOffset;
    }

    public static string Time(DateTime? value) => value is null ? "" : ToVietnam(value.Value).ToString("dd/MM/yyyy HH:mm", Vi);

    // Dạng ngắn cho lưới (không năm): đơn Web chỉ liên quan vài ngày gần nhất.
    public static string TimeShort(DateTime? value) => value is null ? "" : ToVietnam(value.Value).ToString("dd/MM HH:mm", Vi);

    public static string Money(decimal value) => string.Format(Vi, value % 1 == 0 ? "{0:N0} ₫" : "{0:N2} ₫", value);

    public static string Number(decimal value) => value.ToString("0.###", Vi);

    public static string OrderStatus(string status) => status switch
    {
        "Pending" => "Chờ tiếp nhận",
        "Serving" => "Đang phục vụ",
        "Settling" => "Đang thanh toán",
        "Closed" => "Hoàn tất",
        "Cancelled" => "Đã hủy",
        "Rejected" => "Bị từ chối",
        _ => status,
    };

    // Tiền một dòng: (đơn giá + tùy chọn theo từng ly) × số lượng; cùng công thức máy chủ dùng để tính tổng đơn.
    public static decimal LineTotal(OrderLineDto line) =>
        (line.UnitPrice + line.Addons.Sum(a => a.UnitPrice * a.QuantityPerUnit)) * line.Quantity;

    public static string AddonsText(OrderLineDto line) =>
        line.Addons.Count == 0 ? "" : string.Join("; ", line.Addons.Select(a => $"{a.Name} ×{a.QuantityPerUnit}/ly"));

    // Chuẩn hóa để tìm kiếm không phân biệt hoa thường và dấu tiếng Việt.
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}

// Thông báo cho thu ngân/quản lý. Quy tắc: kết quả chưa xác định (mất mạng, hết giờ, lỗi 5xx) không được báo là thành công
// hay thất bại chắc chắn; phải tải lại để xem trạng thái thật trước khi thao tác tiếp.
public static class StaffMessages
{
    public static string ForAccept(ApiResult<AcceptOrderResponse> r) => r switch
    {
        { Ok: true } => "Đã tiếp nhận đơn; phiếu pha chế đã được tạo.",
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; nếu đơn vẫn chờ, bấm Tiếp nhận lại (hệ thống không tạo phiếu trùng).",
        { Error.Code: ApiErrorCodes.InsufficientStock } => "Không đủ nguyên liệu để tiếp nhận đơn này. Đơn vẫn chờ; hãy bổ sung tồn kho hoặc từ chối đơn.",
        { Error.Code: ApiErrorCodes.InvalidState } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền tiếp nhận đơn.",
        { StatusCode: 404 } => "Không tìm thấy đơn. Danh sách đã được tải lại.",
        _ => r.Message,
    };

    public static string ForReject(ApiResult<Empty> r) => r switch
    {
        { Ok: true } => "Đã từ chối đơn và ghi lý do.",
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; chỉ từ chối lại nếu đơn vẫn ở trạng thái chờ.",
        { Error.Code: ApiErrorCodes.InvalidState } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền từ chối đơn.",
        { StatusCode: 404 } => "Không tìm thấy đơn. Danh sách đã được tải lại.",
        _ => r.Message,
    };

    // Có nên tải lại danh sách sau thao tác này: đã đổi (Ok), chưa rõ (Unknown), hoặc đơn đã thay đổi/biến mất (404, 409).
    public static bool ReloadAfter<T>(ApiResult<T> r) => r.Ok || r.Unknown || r.StatusCode is 404 or 409;

    public static string ForMenu<T>(ApiResult<T> r, string successText) => r switch
    {
        { Ok: true } => successText,
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; hãy kiểm tra thao tác đã có hiệu lực chưa trước khi làm lại.",
        { Error.Code: ApiErrorCodes.OverlappingInterval } => r.Message + " Chọn ngày hiệu lực sau phiên bản hiện tại.",
        { StatusCode: 403 } => "Tài khoản không có quyền quản lý thực đơn.",
        { StatusCode: 404 } => "Không tìm thấy mục này (có thể vừa bị thay đổi). Danh sách đã được tải lại.",
        _ => r.Message,
    };

    public static string ForLoad<T>(ApiResult<T> r, string what) => r switch
    {
        { Unknown: true } => r.Message,
        { StatusCode: 403 } => "Tài khoản không có quyền xem " + what + ".",
        _ => "Không tải được " + what + ": " + r.Message,
    };
}

// Kiểm tra đầu vào trước khi gọi API; máy chủ vẫn kiểm tra lại (đây chỉ để báo lỗi sớm, gần trường nhập).
// Định dạng số theo cách viết Việt Nam: dấu chấm ngăn nghìn, dấu phẩy cho phần thập phân. Chuỗi mơ hồ (ví dụ "0.5") bị từ chối
// thay vì đoán, để không âm thầm đổi 0,5 thành 5.
public static class StaffInput
{
    public const decimal MaxPrice = 100_000_000m;
    public const decimal MaxQuantity = 1_000_000m;
    private static readonly Regex PriceShape = new(@"^(\d{1,3}(\.\d{3})+|\d+)(,\d{1,2})?$", RegexOptions.Compiled);
    private static readonly Regex QuantityShape = new(@"^(\d{1,3}(\.\d{3})+|\d+)(,\d{1,3})?$", RegexOptions.Compiled);

    private static decimal Parse(string text) => decimal.Parse(text.Replace(".", "").Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);

    public static bool TryParsePrice(string? text, out decimal value, out string error)
    {
        value = 0; text = text?.Trim();
        if (string.IsNullOrEmpty(text)) { error = "Nhập giá."; return false; }
        if (text.StartsWith('-')) { error = "Giá không được âm."; return false; }
        if (!PriceShape.IsMatch(text)) { error = "Giá không hợp lệ: nhập số, dùng dấu chấm ngăn nghìn và dấu phẩy cho phần thập phân (ví dụ 35.000)."; return false; }
        value = Parse(text);
        if (value > MaxPrice) { error = "Giá tối đa 100.000.000."; return false; }
        error = ""; return true;
    }

    public static bool TryParseQuantity(string? text, out decimal value, out string error)
    {
        value = 0; text = text?.Trim();
        if (string.IsNullOrEmpty(text)) { error = "Nhập định lượng."; return false; }
        if (text.StartsWith('-')) { error = "Định lượng phải lớn hơn 0."; return false; }
        if (!QuantityShape.IsMatch(text)) { error = "Định lượng không hợp lệ: dùng dấu phẩy cho phần thập phân, tối đa 3 chữ số (ví dụ 12,5)."; return false; }
        value = Parse(text);
        if (value <= 0) { error = "Định lượng phải lớn hơn 0."; return false; }
        if (value > MaxQuantity) { error = "Định lượng tối đa 1.000.000."; return false; }
        error = ""; return true;
    }

    public static bool TryName(string? text, string label, out string name, out string error)
    {
        name = text?.Trim() ?? "";
        if (name.Length == 0) { error = $"Nhập {label}."; return false; }
        if (name.Length > 200) { error = $"{char.ToUpper(label[0])}{label[1..]} tối đa 200 ký tự."; return false; }
        error = ""; return true;
    }

    public static bool TryReason(string? text, out string reason, out string error)
    {
        reason = text?.Trim() ?? "";
        if (reason.Length == 0) { error = "Nhập lý do từ chối."; return false; }
        if (reason.Length > 500) { error = "Lý do tối đa 500 ký tự."; return false; }
        error = ""; return true;
    }

    // Ngày hiệu lực người dùng chọn theo giờ máy (Kind không xác định coi là giờ máy) → UTC để gửi API.
    public static DateTime ToUtc(DateTime picked) =>
        picked.Kind == DateTimeKind.Utc ? picked : DateTime.SpecifyKind(picked, picked.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : picked.Kind).ToUniversalTime();
}

// Một dòng trong lưới quản lý món: mỗi biến thể một dòng; món chưa có biến thể vẫn hiện (VariantId = null).
public sealed record MenuRow(
    long ProductId, string ProductName, long CategoryId, string CategoryName, bool ProductActive,
    long? VariantId, string VariantName, string VariantType, bool VariantActive,
    decimal? Price, DateTime? PriceFrom, IReadOnlyList<StaffRecipeLineDto> Recipe,
    string Status, bool CanActivate, bool CanDeactivate)
{
    public string RecipeSummary => Recipe.Count == 0 ? "" :
        string.Join("; ", Recipe.Select(l => $"{l.IngredientName} {Display.Number(l.Quantity)} {l.UnitName}"));
}

public static class MenuRows
{
    public static IReadOnlyList<MenuRow> Flatten(StaffMenuResponse menu, string? search = null)
    {
        var needle = Display.Fold(search?.Trim());
        var rows = new List<MenuRow>();
        foreach (var p in menu.Products)
        {
            if (p.Variants.Count == 0)
                rows.Add(new MenuRow(p.ProductId, p.ProductName, p.CategoryId, p.CategoryName, p.IsActive, null, "", "", false, null, null, [],
                    p.IsActive ? "Chưa có biến thể" : "Món ngừng bán", false, false));
            foreach (var v in p.Variants)
            {
                var hasPrice = v.CurrentPrice is not null;
                var hasRecipe = v.CurrentRecipe.Count > 0;
                var status = !p.IsActive ? "Món ngừng bán"
                    : v.IsActive ? "Đang bán"
                    : !hasPrice && !hasRecipe ? "Chưa bán: thiếu giá và công thức"
                    : !hasPrice ? "Chưa bán: thiếu giá"
                    : !hasRecipe ? "Chưa bán: thiếu công thức"
                    : "Chưa bán: sẵn sàng bật bán";
                rows.Add(new MenuRow(p.ProductId, p.ProductName, p.CategoryId, p.CategoryName, p.IsActive, v.VariantId, v.VariantName, v.VariantType,
                    v.IsActive, v.CurrentPrice, v.PriceEffectiveFrom, v.CurrentRecipe, status,
                    CanActivate: !v.IsActive && hasPrice && hasRecipe, CanDeactivate: v.IsActive));
            }
        }
        if (needle.Length == 0) return rows;
        return rows.Where(r => Display.Fold(r.ProductName).Contains(needle) || Display.Fold(r.VariantName).Contains(needle)
            || Display.Fold(r.CategoryName).Contains(needle)).ToList();
    }
}
