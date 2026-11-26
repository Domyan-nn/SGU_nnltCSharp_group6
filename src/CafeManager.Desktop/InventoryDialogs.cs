using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// Hộp thoại của màn hình Kho nguyên liệu (UC13): nhập kho, lập phiếu kiểm kê, từ chối phiếu. Kiểm tra sớm bằng StaffInput;
// máy chủ vẫn kiểm tra lại. Mã yêu cầu chống gửi trùng do form gọi tạo.

internal sealed class StockReceiptDialog : EditDialog
{
    private readonly TextBox _quantity = new() { Width = 380, MaxLength = 20, AccessibleName = "Số lượng nhập" };
    private readonly TextBox _note = new()
    {
        Width = 380, Height = 70, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical, AccessibleName = "Ghi chú nhập kho",
    };
    public decimal Quantity { get; private set; }
    public string? Note { get; private set; }

    public StockReceiptDialog(InventoryItemDto item, decimal? quantity = null, string? note = null) : base("Nhập kho")
    {
        Ok.Text = "Nhập kho";
        AddRow(new Label
        {
            Text = $"Nguyên liệu: {item.IngredientName} (đơn vị {item.UnitName}). Tồn sổ hiện tại {Display.Number(item.QuantityOnHand)} {item.UnitName}, " +
                   $"đang giữ cho đơn {Display.Number(item.QuantityReserved)}. Số lượng nhập được cộng vào tồn và ghi vào sổ kho; không sửa hay xóa được sau khi nhập.",
            AutoSize = true, MaximumSize = new Size(400, 0), Margin = new Padding(0, 0, 0, 8),
        });
        AddRow(Ui.Labeled($"Số lượng nhập ({item.UnitName})", _quantity));
        AddRow(Ui.Labeled("Ghi chú (không bắt buộc, ví dụ nhà cung cấp, số hóa đơn)", _note));
        if (quantity is { } q) _quantity.Text = Display.Number(q);
        if (note is not null) _note.Text = note;
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryParseQuantity(_quantity.Text, out var quantity, out var error, "số lượng nhập")) return Fail(_quantity, error);
        var note = _note.Text.Trim();
        if (note.Length > 500) return Fail(_note, "Ghi chú tối đa 500 ký tự.");
        Quantity = quantity;
        Note = note.Length == 0 ? null : note;
        return true;
    }
}

internal sealed class StockCountDialog : EditDialog
{
    private readonly TextBox _counted = new() { Width = 380, MaxLength = 20, AccessibleName = "Lượng thực đếm" };
    private readonly TextBox _note = new()
    {
        Width = 380, Height = 70, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical, AccessibleName = "Lý do chênh lệch",
    };
    public decimal CountedQty { get; private set; }
    public string? Note { get; private set; }

    public StockCountDialog(InventoryItemDto item) : base("Kiểm kê")
    {
        Ok.Text = "Lập phiếu";
        AddRow(new Label
        {
            Text = $"Nguyên liệu: {item.IngredientName}. Tồn sổ hiện tại {Display.Number(item.QuantityOnHand)} {item.UnitName}. " +
                   "Nhập lượng đếm được thực tế (kể cả phần đang giữ cho đơn chưa pha). Phiếu chờ quản lý khác duyệt; tồn chỉ đổi sau khi duyệt.",
            AutoSize = true, MaximumSize = new Size(400, 0), Margin = new Padding(0, 0, 0, 8),
        });
        AddRow(Ui.Labeled($"Lượng thực đếm ({item.UnitName})", _counted));
        AddRow(Ui.Labeled("Lý do chênh lệch (không bắt buộc, ví dụ đổ vỡ, hết hạn)", _note));
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryParseCountedQuantity(_counted.Text, out var counted, out var error)) return Fail(_counted, error);
        var note = _note.Text.Trim();
        if (note.Length > 500) return Fail(_note, "Ghi chú tối đa 500 ký tự.");
        CountedQty = counted;
        Note = note.Length == 0 ? null : note;
        return true;
    }
}

internal sealed class RejectCountDialog : EditDialog
{
    private readonly TextBox _reason = new()
    {
        Multiline = true, Width = 440, Height = 90, MaxLength = 500, ScrollBars = ScrollBars.Vertical, AccessibleName = "Lý do từ chối phiếu kiểm kê",
    };
    public string Reason { get; private set; } = "";

    public RejectCountDialog(StockCountDto count) : base("Từ chối phiếu kiểm kê")
    {
        Ok.Text = "Từ chối phiếu";
        AddRow(new Label
        {
            Text = $"Từ chối phiếu CNT-{count.CountId} ({count.IngredientName}, chênh lệch {InventoryText.Difference(count.Difference)} {count.UnitName}). " +
                   "Tồn kho giữ nguyên; nhân viên kho có thể kiểm kê lại.",
            AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 0, 0, 8),
        });
        AddRow(Ui.Labeled("Lý do (bắt buộc)", _reason));
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryReason(_reason.Text, out var reason, out var error)) return Fail(_reason, error);
        Reason = reason;
        return true;
    }
}
