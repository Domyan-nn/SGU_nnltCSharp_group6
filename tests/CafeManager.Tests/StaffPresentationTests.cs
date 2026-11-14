using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Tests;

// Kiểm thử logic hiển thị và kiểm tra đầu vào của D02/D06 (không cần SQL Server hay Windows Forms).
public sealed class StaffPresentationTests
{
    private static ApiResult<T> Fail<T>(int status, string code, string message = "lỗi", bool unknown = false) =>
        new(false, status, default, new ApiError(code, message), unknown);

    [Theory]
    [InlineData("35000", 35000)]
    [InlineData("35.000", 35000)]
    [InlineData("1.250.000", 1250000)]
    [InlineData("35.000,5", 35000.5)]
    [InlineData("0", 0)]
    [InlineData(" 12000 ", 12000)]
    public void Price_accepts_vietnamese_number_formats(string text, double expected)
    {
        Assert.True(StaffInput.TryParsePrice(text, out var value, out var error), error);
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("", "Nhập giá.")]
    [InlineData("-5000", "Giá không được âm.")]
    [InlineData("abc", "Giá không hợp lệ")]
    [InlineData("35.00", "Giá không hợp lệ")]
    [InlineData("10,123", "Giá không hợp lệ")]
    [InlineData("100.000.001", "Giá tối đa")]
    public void Price_rejects_bad_input_with_a_message_near_the_field(string text, string messageStart)
    {
        Assert.False(StaffInput.TryParsePrice(text, out _, out var error));
        Assert.StartsWith(messageStart, error);
    }

    [Theory]
    [InlineData("20", 20)]
    [InlineData("0,5", 0.5)]
    [InlineData("12,125", 12.125)]
    [InlineData("1.000", 1000)]
    public void Quantity_accepts_comma_decimals_and_dot_thousands(string text, double expected)
    {
        Assert.True(StaffInput.TryParseQuantity(text, out var value, out var error), error);
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("0.5")]   // mơ hồ: không được đoán thành 5
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("1,2345")]
    [InlineData("")]
    [InlineData("1.000.001")]
    public void Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(string text) =>
        Assert.False(StaffInput.TryParseQuantity(text, out _, out _));

    [Fact]
    public void Names_and_reasons_are_trimmed_required_and_length_limited()
    {
        Assert.True(StaffInput.TryName("  Cà phê sữa ", "tên món", out var name, out _)); Assert.Equal("Cà phê sữa", name);
        Assert.False(StaffInput.TryName("   ", "tên món", out _, out var e1)); Assert.Equal("Nhập tên món.", e1);
        Assert.False(StaffInput.TryName(new string('a', 201), "tên món", out _, out var e2)); Assert.Equal("Tên món tối đa 200 ký tự.", e2);
        Assert.False(StaffInput.TryReason("  ", out _, out var e3)); Assert.Equal("Nhập lý do từ chối.", e3);
        Assert.False(StaffInput.TryReason(new string('x', 501), out _, out _));
        Assert.True(StaffInput.TryReason(" hết hàng ", out var reason, out _)); Assert.Equal("hết hàng", reason);
    }

    [Fact]
    public void Picked_local_time_is_sent_as_utc()
    {
        var local = new DateTime(2026, 11, 14, 15, 30, 0, DateTimeKind.Unspecified);
        var utc = StaffInput.ToUtc(local);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime(), utc);
        Assert.Equal(utc, StaffInput.ToUtc(utc));
    }

    [Fact]
    public void Times_are_shown_in_vietnam_time_and_money_in_dong()
    {
        Assert.Equal("14/11/2026 17:05", Display.Time(new DateTime(2026, 11, 14, 10, 5, 0, DateTimeKind.Utc)));
        Assert.Equal("14/11/2026 17:05", Display.Time(new DateTime(2026, 11, 14, 10, 5, 0, DateTimeKind.Unspecified))); // từ CSDL: coi là UTC
        Assert.Equal("", Display.Time(null));
        Assert.Equal("14/11 17:05", Display.TimeShort(new DateTime(2026, 11, 14, 10, 5, 0, DateTimeKind.Utc)));
        Assert.Equal("", Display.TimeShort(null));
        Assert.Equal("35.000 ₫", Display.Money(35000));
        Assert.Equal("1.250.000 ₫", Display.Money(1250000));
        Assert.Equal("12,5", Display.Number(12.5m));
        Assert.Equal("20", Display.Number(20.000m));
    }

    [Fact]
    public void Line_total_matches_server_formula_with_addons_per_unit()
    {
        var line = new OrderLineDto("Cà phê", "Vừa", 2, 30000, [new OrderAddonDto("Thêm sữa", 1, 5000), new OrderAddonDto("Thêm đá", 2, 1000)]);
        Assert.Equal((30000 + 5000 + 2000) * 2, Display.LineTotal(line));
        Assert.Equal("Thêm sữa ×1/ly; Thêm đá ×2/ly", Display.AddonsText(line));
        Assert.Equal("", Display.AddonsText(new OrderLineDto("A", "B", 1, 1, [])));
    }

    [Fact]
    public void Search_ignores_case_and_vietnamese_diacritics()
    {
        Assert.Equal("ca phe sua da", Display.Fold("Cà Phê Sữa Đá"));
        Assert.Equal("d", Display.Fold("Đ"));
        Assert.Equal("", Display.Fold(null));
    }

    [Fact]
    public void Accept_messages_distinguish_stock_state_unknown_and_permission()
    {
        Assert.Contains("phiếu pha chế", StaffMessages.ForAccept(new(true, 200, new AcceptOrderResponse(1), null, false)));
        Assert.Contains("Không đủ nguyên liệu", StaffMessages.ForAccept(Fail<AcceptOrderResponse>(409, ApiErrorCodes.InsufficientStock)));
        var cancelled = StaffMessages.ForAccept(Fail<AcceptOrderResponse>(409, ApiErrorCodes.InvalidState, "Khách đã hủy đơn này."));
        Assert.Contains("Khách đã hủy đơn này.", cancelled); Assert.Contains("tải lại", cancelled);
        var unknown = StaffMessages.ForAccept(Fail<AcceptOrderResponse>(0, "UNKNOWN", "Mất kết nối.", unknown: true));
        Assert.Contains("Mất kết nối.", unknown); Assert.Contains("không tạo phiếu trùng", unknown);
        Assert.Contains("không có quyền", StaffMessages.ForAccept(Fail<AcceptOrderResponse>(403, ApiErrorCodes.Forbidden)));
    }

    [Fact]
    public void Reject_unknown_result_warns_not_to_repeat_blindly()
    {
        var unknown = StaffMessages.ForReject(Fail<Empty>(0, "UNKNOWN", "Hết giờ.", unknown: true));
        Assert.Contains("chỉ từ chối lại nếu đơn vẫn ở trạng thái chờ", unknown);
        Assert.Equal("Đã từ chối đơn và ghi lý do.", StaffMessages.ForReject(new(true, 204, default, null, false)));
    }

    [Fact]
    public void Reload_happens_after_success_unknown_not_found_and_conflict_but_not_validation()
    {
        Assert.True(StaffMessages.ReloadAfter(new ApiResult<Empty>(true, 204, default, null, false)));
        Assert.True(StaffMessages.ReloadAfter(Fail<Empty>(0, "UNKNOWN", unknown: true)));
        Assert.True(StaffMessages.ReloadAfter(Fail<Empty>(404, ApiErrorCodes.NotFound)));
        Assert.True(StaffMessages.ReloadAfter(Fail<Empty>(409, ApiErrorCodes.Conflict)));
        Assert.False(StaffMessages.ReloadAfter(Fail<Empty>(400, ApiErrorCodes.Validation)));
        Assert.False(StaffMessages.ReloadAfter(Fail<Empty>(403, ApiErrorCodes.Forbidden)));
    }

    [Fact]
    public void Menu_messages_explain_overlap_and_unknown()
    {
        Assert.Contains("sau phiên bản hiện tại", StaffMessages.ForMenu(Fail<Empty>(409, ApiErrorCodes.OverlappingInterval, "Đã có giá hiệu lực từ cùng hoặc sau ngày này."), "ok"));
        Assert.Equal("ok", StaffMessages.ForMenu(new ApiResult<Empty>(true, 204, default, null, false), "ok"));
        Assert.Contains("kiểm tra thao tác đã có hiệu lực chưa", StaffMessages.ForMenu(Fail<Empty>(0, "UNKNOWN", "Mất mạng.", unknown: true), "ok"));
    }

    private static StaffMenuResponse Menu() => new(
    [
        new StaffProductDto(1, "Cà phê sữa", 10, "Cà phê", true,
        [
            new StaffVariantDto(11, "Vừa", "Drink", true, 30000, DateTime.UtcNow, [new StaffRecipeLineDto(100, "Cà phê bột", "g", 20)]),
            new StaffVariantDto(12, "Lớn", "Drink", false, null, null, []),
            new StaffVariantDto(13, "Nhỏ", "Drink", false, 25000, DateTime.UtcNow, []),
            new StaffVariantDto(14, "Mini", "Drink", false, null, null, [new StaffRecipeLineDto(100, "Cà phê bột", "g", 12.5m)]),
            new StaffVariantDto(15, "Cũ", "Drink", false, 20000, DateTime.UtcNow, [new StaffRecipeLineDto(100, "Cà phê bột", "g", 15)]),
        ]),
        new StaffProductDto(2, "Trà đào", 20, "Trà", true, []),
        new StaffProductDto(3, "Món cũ", 20, "Trà", false, [new StaffVariantDto(31, "Vừa", "Drink", true, 10000, DateTime.UtcNow, [new StaffRecipeLineDto(100, "Cà phê bột", "g", 1)])]),
    ], [new CategoryLookupDto(10, "Cà phê")], [new IngredientLookupDto(100, "Cà phê bột", "g")]);

    [Fact]
    public void Menu_rows_show_why_a_variant_cannot_be_sold_and_which_actions_apply()
    {
        var rows = MenuRows.Flatten(Menu()).ToDictionary(r => (r.ProductId, r.VariantId));
        Assert.Equal("Đang bán", rows[(1, 11)].Status); Assert.True(rows[(1, 11)].CanDeactivate); Assert.False(rows[(1, 11)].CanActivate);
        Assert.Equal("Cà phê bột 20 g", rows[(1, 11)].RecipeSummary);
        Assert.Equal("Chưa bán: thiếu giá và công thức", rows[(1, 12)].Status); Assert.False(rows[(1, 12)].CanActivate);
        Assert.Equal("Chưa bán: thiếu công thức", rows[(1, 13)].Status); Assert.False(rows[(1, 13)].CanActivate);
        Assert.Equal("Chưa bán: thiếu giá", rows[(1, 14)].Status); Assert.Equal("Cà phê bột 12,5 g", rows[(1, 14)].RecipeSummary);
        Assert.Equal("Chưa bán: sẵn sàng bật bán", rows[(1, 15)].Status); Assert.True(rows[(1, 15)].CanActivate);
        Assert.Equal("Chưa có biến thể", rows[(2, null)].Status); Assert.Null(rows[(2, null)].VariantId);
        Assert.Equal("Món ngừng bán", rows[(3, 31)].Status);
    }

    [Theory]
    [InlineData("ca phe", 5)]
    [InlineData("TRÀ", 2)]
    [InlineData("tra dao", 1)]
    [InlineData("khong co", 0)]
    [InlineData("  ", 7)]
    public void Menu_search_matches_product_variant_and_category_without_diacritics(string search, int expected) =>
        Assert.Equal(expected, MenuRows.Flatten(Menu(), search).Count);
}
