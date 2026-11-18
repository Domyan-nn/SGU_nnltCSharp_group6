using System.Text;
using CafeManager.Core;

namespace CafeManager.Client;

// Logic hiển thị và kiểm tra đầu vào cho màn hình Thu ngân (A4) và Pha chế (C3) trên Desktop,
// tách khỏi Windows Forms để kiểm thử được (cùng cách làm với StaffPresentation.cs).

public static class CashierInput
{
    // Tiền mặt (tiền đầu ca, tiền đếm khi đóng ca): số không âm, cho phép 0.
    public static bool TryParseCash(string? text, string label, out decimal value, out string error)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) { error = $"Nhập {label}."; return false; }
        if (!StaffInput.TryParsePrice(text, out var parsed, out _))
        {
            error = $"{char.ToUpper(label[0])}{label[1..]} không hợp lệ: nhập số không âm, dùng dấu chấm ngăn nghìn (ví dụ 500.000).";
            return false;
        }
        value = parsed; error = ""; return true;
    }

    // Tiền khách đưa khi thu một đơn: phải đủ số còn phải thu (máy chủ cũng kiểm tra lại).
    public static bool TryParseReceived(string? text, decimal remaining, out decimal received, out string error)
    {
        received = 0;
        if (string.IsNullOrWhiteSpace(text)) { error = "Nhập số tiền khách đưa."; return false; }
        if (!StaffInput.TryParsePrice(text, out var parsed, out _))
        {
            error = "Số tiền khách đưa không hợp lệ: nhập số không âm, dùng dấu chấm ngăn nghìn (ví dụ 50.000).";
            return false;
        }
        if (parsed < remaining)
        {
            error = $"Khách đưa chưa đủ: còn thiếu {Display.Money(remaining - parsed)}.";
            return false;
        }
        received = parsed; error = ""; return true;
    }

    // Dòng xem trước cạnh ô nhập: tiền thối, hoặc số còn thiếu; "—" khi chưa nhập được số hợp lệ.
    public static string PreviewChange(string? text, decimal remaining)
    {
        if (!StaffInput.TryParsePrice(text, out var received, out _)) return "Tiền thối: —";
        return received < remaining
            ? "Còn thiếu: " + Display.Money(remaining - received)
            : "Tiền thối: " + Display.Money(received - remaining);
    }
}

public static class CashierText
{
    public static string Difference(decimal? difference)
    {
        if (difference is null) return "chưa có";
        if (difference.Value == 0) return "khớp (không chênh lệch)";
        return difference.Value > 0 ? "thừa " + Display.Money(difference.Value) : "thiếu " + Display.Money(-difference.Value);
    }

    public static string ShiftSummary(ShiftSummaryDto s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tiền đầu ca: {Display.Money(s.OpeningCash)}");
        sb.AppendLine($"Thu tiền mặt: {Display.Money(s.CashPayments)}");
        sb.AppendLine($"Hoàn tiền: {Display.Money(s.CashRefunds)}");
        if (s.OtherIn != 0 || s.OtherOut != 0)
            sb.AppendLine($"Thu khác: {Display.Money(s.OtherIn)}; chi khác: {Display.Money(s.OtherOut)}");
        sb.AppendLine($"Tiền mặt dự kiến trong két: {Display.Money(s.ExpectedCash)}");
        sb.AppendLine($"Tiền đếm thực tế: {(s.CountedCash is null ? "chưa có" : Display.Money(s.CountedCash.Value))}");
        sb.Append($"Chênh lệch: {Difference(s.Difference)}");
        return sb.ToString();
    }

    public static string ShiftLine(ShiftDto? shift) => shift is null
        ? "Chưa mở ca. Bấm \"Mở ca\" để bắt đầu thu tiền."
        : $"Ca #{shift.ShiftId} – quầy {shift.CounterCode} – {shift.OpenedByName} mở lúc {Display.Time(shift.OpenedAt)} – tiền đầu ca {Display.Money(shift.OpeningCash)}";
}

// Thông báo cho thu ngân. Quy tắc giống StaffMessages: kết quả chưa xác định (mất mạng, hết giờ, lỗi 5xx)
// không được báo là thành công hay thất bại chắc chắn.
public static class CashierMessages
{
    public static string ForOpenShift(ApiResult<ShiftDto> r) => r switch
    {
        { Ok: true } => "Đã mở ca.",
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; kiểm tra ca đã mở chưa trước khi mở lại.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền mở ca.",
        _ => r.Message,
    };

    public static string ForCloseShift(ApiResult<ShiftSummaryDto> r) => r switch
    {
        { Ok: true } => "Đã đóng ca và đối soát.",
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; nếu ca vẫn đang mở thì đóng lại.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền đóng ca.",
        _ => r.Message,
    };

    public static string ForPayment(ApiResult<CashPaymentResponse> r, string orderCode) => r switch
    {
        { Ok: true } => $"Đã thu {Display.Money(r.Value!.AmountDue)} cho đơn {orderCode}; tiền thối {Display.Money(r.Value.Change)}.",
        { Unknown: true } => r.Message + " Chưa biết đã thu tiền chưa. Bấm Thu tiền mặt lại với cùng số tiền (hệ thống không thu hai lần) hoặc xem danh sách đã tải lại.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 404 } => "Không tìm thấy đơn. Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền thu tiền.",
        _ => r.Message,
    };
}

public static class PreparationText
{
    public static string Status(string status) => status switch
    {
        PreparationStatuses.Waiting => "Chờ làm",
        PreparationStatuses.Preparing => "Đang làm",
        PreparationStatuses.Ready => "Sẵn sàng",
        PreparationStatuses.Delivered => "Đã giao",
        PreparationStatuses.Cancelled => "Đã hủy",
        _ => status,
    };

    // Tên nút cho thao tác kế tiếp của một ly; null khi không còn thao tác (đã giao/đã hủy).
    public static string? NextAction(string status) => status switch
    {
        PreparationStatuses.Waiting => "Bắt đầu làm",
        PreparationStatuses.Preparing => "Làm xong (sẵn sàng)",
        PreparationStatuses.Ready => "Đã giao cho khách",
        _ => null,
    };

    public static string SuccessText(string status) => status switch
    {
        PreparationStatuses.Waiting => "Đã bắt đầu làm ly này.",
        PreparationStatuses.Preparing => "Ly đã làm xong, sẵn sàng giao.",
        PreparationStatuses.Ready => "Đã giao ly cho khách.",
        _ => "Đã cập nhật.",
    };

    public static string Cups(PreparationUnitDto unit) => $"{unit.UnitNumber}/{unit.LineQuantity}";

    public static string Addons(PreparationUnitDto unit) =>
        unit.Addons.Count == 0 ? "" : string.Join("; ", unit.Addons.Select(a => $"{a.Name} ×{a.QuantityPerUnit}"));

    // Ba thao tác không tự thử lại: gọi lần hai trả 409 vì ly đã đổi trạng thái.
    public static string ForAction(ApiResult<Empty> r, string successText) => r switch
    {
        { Ok: true } => successText,
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; kiểm tra trạng thái ly trước khi thao tác tiếp.",
        { StatusCode: 409 } => r.Message + " Danh sách đã được tải lại.",
        { StatusCode: 404 } => "Không tìm thấy ly này. Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền thao tác pha chế.",
        _ => r.Message,
    };
}

// Xóa danh mục / nguyên liệu (C1, C2): 409 nghĩa là còn được sử dụng, hướng dẫn ngừng sử dụng thay vì xóa.
public static class CatalogMessages
{
    public static string ForDelete(ApiResult<Empty> r, string what) => r switch
    {
        { Ok: true } => $"Đã xóa {what}.",
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; kiểm tra mục đã bị xóa chưa trước khi xóa lại.",
        { StatusCode: 409 } => $"Không xóa được {what}: {r.Message} Có thể bỏ chọn \"Đang dùng\" để ngừng sử dụng.",
        { StatusCode: 404 } => "Không tìm thấy mục này (có thể vừa bị xóa). Danh sách đã được tải lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền quản lý danh mục.",
        _ => r.Message,
    };

    public static string BlockedByProducts(string name, int productCount) =>
        $"Danh mục \"{name}\" còn {productCount} món nên không thể xóa. Chuyển món sang danh mục khác, hoặc bỏ chọn \"Đang dùng\" để ngừng sử dụng.";

    public static string BlockedByRecipes(string name, int recipeCount) =>
        $"Nguyên liệu \"{name}\" đang nằm trong {recipeCount} công thức nên không thể xóa. Bỏ chọn \"Đang dùng\" để ngừng sử dụng.";
}
