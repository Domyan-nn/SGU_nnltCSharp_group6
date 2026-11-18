using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Tests;

// Kiểm thử logic hiển thị và kiểm tra đầu vào của màn hình Thu ngân (A4), Pha chế (C3), danh mục/nguyên liệu (C1, C2).
// Không cần SQL Server hay Windows Forms.
public sealed class CashierPresentationTests
{
    private static ApiResult<T> Fail<T>(int status, string code, string message = "lỗi", bool unknown = false) =>
        new(false, status, default, new ApiError(code, message), unknown);

    private static ApiResult<T> Ok<T>(T value) => new(true, 200, value, null, false);

    [Theory]
    [InlineData("0", 0)]
    [InlineData("500.000", 500000)]
    [InlineData("1250000", 1250000)]
    [InlineData(" 80.000 ", 80000)]
    public void Cash_accepts_zero_and_vietnamese_number_formats(string text, double expected)
    {
        Assert.True(CashierInput.TryParseCash(text, "tiền đầu ca", out var value, out var error), error);
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-5000")]
    [InlineData("abc")]
    [InlineData("1.5.0")]
    public void Cash_rejects_blank_negative_and_malformed_input(string text)
    {
        Assert.False(CashierInput.TryParseCash(text, "tiền đầu ca", out var value, out var error));
        Assert.Equal(0m, value);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Cash_error_names_the_field()
    {
        CashierInput.TryParseCash("", "tiền đầu ca", out _, out var empty);
        CashierInput.TryParseCash("abc", "tiền đếm thực tế", out _, out var bad);
        Assert.Equal("Nhập tiền đầu ca.", empty);
        Assert.StartsWith("Tiền đếm thực tế không hợp lệ", bad);
    }

    [Theory]
    [InlineData("65.000", 65000, 65000)]   // đưa đủ
    [InlineData("100.000", 65000, 100000)] // đưa dư, có tiền thối
    [InlineData("65000", 65000, 65000)]
    public void Received_accepts_exact_or_more_than_remaining(string text, double remaining, double expected)
    {
        Assert.True(CashierInput.TryParseReceived(text, (decimal)remaining, out var received, out var error), error);
        Assert.Equal((decimal)expected, received);
    }

    [Fact]
    public void Received_rejects_less_than_remaining_and_says_how_much_is_missing()
    {
        Assert.False(CashierInput.TryParseReceived("50.000", 65000m, out var received, out var error));
        Assert.Equal(0m, received);
        Assert.StartsWith("Khách đưa chưa đủ", error);
        Assert.Contains("15.000", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void Received_rejects_blank_or_malformed_input(string text) =>
        Assert.False(CashierInput.TryParseReceived(text, 65000m, out _, out _));

    [Fact]
    public void Change_preview_shows_change_shortfall_or_a_dash()
    {
        Assert.Equal("Tiền thối: 35.000 ₫", CashierInput.PreviewChange("100.000", 65000m));
        Assert.Equal("Tiền thối: 0 ₫", CashierInput.PreviewChange("65.000", 65000m));
        Assert.StartsWith("Còn thiếu: 15.000", CashierInput.PreviewChange("50.000", 65000m));
        Assert.Equal("Tiền thối: —", CashierInput.PreviewChange("", 65000m));
        Assert.Equal("Tiền thối: —", CashierInput.PreviewChange("abc", 65000m));
    }

    [Fact]
    public void Shift_difference_is_worded_as_short_over_or_matching()
    {
        Assert.Equal("chưa có", CashierText.Difference(null));
        Assert.StartsWith("khớp", CashierText.Difference(0m));
        Assert.Equal("thừa 10.000 ₫", CashierText.Difference(10000m));
        Assert.Equal("thiếu 10.000 ₫", CashierText.Difference(-10000m));
    }

    [Fact]
    public void Shift_summary_lists_expected_counted_and_difference()
    {
        var summary = new ShiftSummaryDto(7, 500000m, 195000m, 0m, 0m, 0m, 695000m, 685000m, -10000m);
        var text = CashierText.ShiftSummary(summary);
        Assert.Contains("Tiền đầu ca: 500.000 ₫", text);
        Assert.Contains("Thu tiền mặt: 195.000 ₫", text);
        Assert.Contains("Tiền mặt dự kiến trong két: 695.000 ₫", text);
        Assert.Contains("Tiền đếm thực tế: 685.000 ₫", text);
        Assert.EndsWith("Chênh lệch: thiếu 10.000 ₫", text);
        Assert.DoesNotContain("Thu khác", text);
    }

    [Fact]
    public void Shift_line_says_when_no_shift_is_open()
    {
        Assert.StartsWith("Chưa mở ca", CashierText.ShiftLine(null));
        var shift = new ShiftDto(3, 1, "Q1", "Thu ngân", new DateTime(2026, 11, 18, 1, 0, 0, DateTimeKind.Utc), 500000m, null, null);
        var line = CashierText.ShiftLine(shift);
        Assert.Contains("Ca #3", line);
        Assert.Contains("quầy Q1", line);
        Assert.Contains("08:00", line); // 01:00 UTC = 08:00 giờ Việt Nam
    }

    [Fact]
    public void Payment_messages_never_report_an_unknown_result_as_success()
    {
        var done = Ok(new CashPaymentResponse(9, 65000m, 100000m, 35000m));
        Assert.Equal("Đã thu 65.000 ₫ cho đơn DH01; tiền thối 35.000 ₫.", CashierMessages.ForPayment(done, "DH01"));

        var unknown = CashierMessages.ForPayment(Fail<CashPaymentResponse>(0, "", "Mất kết nối.", unknown: true), "DH01");
        Assert.StartsWith("Mất kết nối.", unknown);
        Assert.Contains("không thu hai lần", unknown);
        Assert.DoesNotContain("Đã thu", unknown);

        Assert.Contains("đã được tải lại", CashierMessages.ForPayment(Fail<CashPaymentResponse>(409, ApiErrorCodes.InvalidState, "Chưa mở ca."), "DH01"));
        Assert.Equal("Tài khoản không có quyền thu tiền.", CashierMessages.ForPayment(Fail<CashPaymentResponse>(403, ApiErrorCodes.Forbidden), "DH01"));
        Assert.Equal("Số tiền chưa đủ.", CashierMessages.ForPayment(Fail<CashPaymentResponse>(400, ApiErrorCodes.Validation, "Số tiền chưa đủ."), "DH01"));
    }

    [Fact]
    public void Shift_messages_cover_open_and_close()
    {
        Assert.Equal("Đã mở ca.", CashierMessages.ForOpenShift(Ok(new ShiftDto(1, 1, "Q1", "A", DateTime.UtcNow, 0m, null, null))));
        Assert.Contains("đã được tải lại", CashierMessages.ForOpenShift(Fail<ShiftDto>(409, ApiErrorCodes.Conflict, "Quầy đã có ca mở.")));
        Assert.Equal("Đã đóng ca và đối soát.", CashierMessages.ForCloseShift(Ok(new ShiftSummaryDto(1, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m))));
        Assert.Contains("đã được tải lại", CashierMessages.ForCloseShift(Fail<ShiftSummaryDto>(409, ApiErrorCodes.InvalidState, "Ca đã đóng.")));
        Assert.Contains("Danh sách đã được tải lại", CashierMessages.ForOpenShift(Fail<ShiftDto>(0, "", "Hết giờ.", unknown: true)));
    }

    [Theory]
    [InlineData(PreparationStatuses.Waiting, "Chờ làm", "Bắt đầu làm")]
    [InlineData(PreparationStatuses.Preparing, "Đang làm", "Làm xong (sẵn sàng)")]
    [InlineData(PreparationStatuses.Ready, "Sẵn sàng", "Đã giao cho khách")]
    [InlineData(PreparationStatuses.Delivered, "Đã giao", null)]
    [InlineData(PreparationStatuses.Cancelled, "Đã hủy", null)]
    public void Preparation_status_has_a_label_and_one_next_action(string status, string label, string? action)
    {
        Assert.Equal(label, PreparationText.Status(status));
        Assert.Equal(action, PreparationText.NextAction(status));
    }

    [Fact]
    public void Preparation_row_texts_show_cup_number_and_addons()
    {
        var unit = new PreparationUnitDto(5, 2, 1, "DH01", "A12", "Cà phê sữa", "Size M",
            [new OrderAddonDto("Trân châu", 2, 5000m)], 2, 3, PreparationStatuses.Waiting, DateTime.UtcNow);
        Assert.Equal("2/3", PreparationText.Cups(unit));
        Assert.Equal("Trân châu ×2", PreparationText.Addons(unit));
        var plain = unit with { Addons = [] };
        Assert.Equal("", PreparationText.Addons(plain));
    }

    [Fact]
    public void Preparation_action_messages_reload_on_conflict_and_never_hide_unknown_results()
    {
        Assert.Equal("Đã bắt đầu làm ly này.", PreparationText.ForAction(Ok(default(Empty)), PreparationText.SuccessText(PreparationStatuses.Waiting)));
        var conflict = PreparationText.ForAction(Fail<Empty>(409, ApiErrorCodes.InvalidState, "Ly đã đổi trạng thái."), "x");
        Assert.Contains("Danh sách đã được tải lại", conflict);
        var unknown = PreparationText.ForAction(Fail<Empty>(0, "", "Mất kết nối.", unknown: true), "x");
        Assert.Contains("kiểm tra trạng thái ly", unknown);
        Assert.True(StaffMessages.ReloadAfter(Fail<Empty>(409, ApiErrorCodes.InvalidState)));
        Assert.False(StaffMessages.ReloadAfter(Fail<Empty>(400, ApiErrorCodes.Validation)));
    }

    [Fact]
    public void Deleting_a_used_catalog_item_explains_how_to_stop_using_it_instead()
    {
        var conflict = CatalogMessages.ForDelete(Fail<Empty>(409, ApiErrorCodes.Conflict, "Danh mục còn 3 món."), "danh mục \"Cà phê\"");
        Assert.StartsWith("Không xóa được danh mục \"Cà phê\"", conflict);
        Assert.Contains("Đang dùng", conflict);
        Assert.Equal("Đã xóa nguyên liệu \"Sữa\".", CatalogMessages.ForDelete(Ok(default(Empty)), "nguyên liệu \"Sữa\""));
        Assert.Contains("còn 3 món", CatalogMessages.BlockedByProducts("Cà phê", 3));
        Assert.Contains("2 công thức", CatalogMessages.BlockedByRecipes("Sữa", 2));
    }
}
