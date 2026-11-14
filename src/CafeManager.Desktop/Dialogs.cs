using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// Hộp thoại nhập liệu của D02/D06. Kiểm tra sớm bằng StaffInput để báo lỗi gần trường; máy chủ vẫn kiểm tra lại.

// D02: lý do từ chối đơn (bắt buộc, tối đa 500 ký tự).
internal sealed class RejectDialog : EditDialog
{
    private readonly TextBox _reason = new()
    {
        Multiline = true, Width = 440, Height = 90, MaxLength = 500, ScrollBars = ScrollBars.Vertical, AccessibleName = "Lý do từ chối",
    };
    public string Reason { get; private set; } = "";

    public RejectDialog(string orderCode) : base("Từ chối đơn " + orderCode)
    {
        Ok.Text = "Từ chối đơn";
        AddRow(new Label
        {
            Text = $"Từ chối đơn {orderCode}. Khách sẽ thấy lý do này trong mục Đơn của tôi.",
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

// D06: thêm hoặc sửa món (tên, danh mục, đang bán). Không có thao tác xóa: món đã có chứng từ chỉ ngừng bán.
internal sealed class ProductDialog : EditDialog
{
    private readonly TextBox _name = new() { Width = 380, MaxLength = 200, AccessibleName = "Tên món" };
    private readonly ComboBox _category = new() { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(CategoryLookupDto.CategoryName), AccessibleName = "Danh mục" };
    private readonly CheckBox _active = new() { Text = "Đang bán món này", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
    public ProductUpsertRequest? Result { get; private set; }

    public ProductDialog(IReadOnlyList<CategoryLookupDto> categories, MenuRow? existing)
        : base(existing is null ? "Thêm món" : "Sửa món")
    {
        _category.DataSource = categories.ToList();
        if (existing is not null)
        {
            _name.Text = existing.ProductName;
            _category.SelectedItem = categories.FirstOrDefault(c => c.CategoryId == existing.CategoryId);
            _active.Checked = existing.ProductActive;
        }
        AddRow(Ui.Labeled("Tên món", _name));
        AddRow(Ui.Labeled("Danh mục", _category));
        AddRow(_active);
        AddRow(new Label
        {
            Text = "Hệ thống không xóa món. Bỏ chọn \"Đang bán\" để ngừng bán; lịch sử đơn cũ không đổi.",
            AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 0, 0, 8),
        });
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryName(_name.Text, "tên món", out var name, out var error)) return Fail(_name, error);
        if (_category.SelectedItem is not CategoryLookupDto category) return Fail(_category, "Chọn danh mục (chưa có danh mục nào thì cần tạo trong CSDL trước).");
        Result = new ProductUpsertRequest(name, category.CategoryId, _active.Checked);
        return true;
    }
}

// D06: thêm biến thể (kích cỡ/loại) hoặc đổi tên. Biến thể mới luôn chưa bán cho đến khi có giá và công thức.
internal sealed class VariantDialog : EditDialog
{
    private readonly TextBox _name = new() { Width = 380, MaxLength = 200, AccessibleName = "Tên biến thể" };
    private readonly ComboBox _type = new() { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(TextChoice.Text), AccessibleName = "Loại biến thể" };
    public string VariantName { get; private set; } = "";
    public string VariantType { get; private set; } = "Drink";

    public VariantDialog(string productName, MenuRow? existing)
        : base(existing?.VariantId is null ? "Thêm biến thể" : "Đổi tên biến thể")
    {
        _type.DataSource = new List<TextChoice> { new("Drink", "Đồ uống (bán riêng)"), new("Addon", "Tùy chọn thêm (topping)") };
        if (existing?.VariantId is not null)
        {
            _name.Text = existing.VariantName;
            _type.SelectedItem = ((List<TextChoice>)_type.DataSource).First(t => t.Value == existing.VariantType);
            _type.Enabled = false; // không đổi được loại của biến thể đã tạo
        }
        AddRow(new Label { Text = "Món: " + productName, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) });
        AddRow(Ui.Labeled("Tên biến thể (ví dụ Vừa, Lớn, Ít đường)", _name));
        AddRow(Ui.Labeled("Loại", _type));
        if (existing?.VariantId is null)
            AddRow(new Label
            {
                Text = "Biến thể mới chưa bán được: cần đặt giá và công thức, sau đó bấm Bật bán.",
                AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 0, 0, 8),
            });
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryName(_name.Text, "tên biến thể", out var name, out var error)) return Fail(_name, error);
        VariantName = name;
        VariantType = ((TextChoice)_type.SelectedItem!).Value;
        return true;
    }
}

// D06: giá mới theo ngày hiệu lực. Giá cũ giữ nguyên cho đến lúc đó (lịch sử giá không bị sửa).
internal sealed class PriceDialog : EditDialog
{
    private readonly TextBox _price = new() { Width = 200, AccessibleName = "Giá bán" };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 200, Value = DateTime.Now, AccessibleName = "Hiệu lực từ" };
    public decimal Price { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }

    public PriceDialog(string label, decimal? current) : base("Đổi giá")
    {
        if (current is not null) _price.Text = Display.Number(current.Value);
        AddRow(new Label { Text = label, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) });
        AddRow(new Label { Text = current is null ? "Chưa có giá." : "Giá hiện hành: " + Display.Money(current.Value), AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
        AddRow(Ui.Labeled("Giá mới (VNĐ), ví dụ 35.000", _price));
        AddRow(Ui.Labeled("Hiệu lực từ (giờ máy này)", _from));
        AddRow(new Label
        {
            Text = "Không chọn ngày trong quá khứ. Không đặt trùng hoặc trước phiên bản giá gần nhất; nếu bị chặn, chọn thời điểm muộn hơn.",
            AutoSize = true, MaximumSize = new Size(440, 0), Margin = new Padding(0, 0, 0, 8),
        });
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryParsePrice(_price.Text, out var price, out var error)) return Fail(_price, error);
        Price = price;
        EffectiveFromUtc = StaffInput.ToUtc(_from.Value);
        return true;
    }
}

// D06: công thức mới (nguyên liệu và định lượng cho MỘT ly/phần) theo ngày hiệu lực.
internal sealed class RecipeDialog : EditDialog
{
    private readonly DataGridView _grid = new()
    {
        Width = 480, Height = 190, AllowUserToAddRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.CellSelect,
        BackgroundColor = SystemColors.Window, AccessibleName = "Công thức",
    };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 200, Value = DateTime.Now, AccessibleName = "Hiệu lực từ" };
    public DateTime EffectiveFromUtc { get; private set; }
    public List<RecipeLineRequest> Lines { get; private set; } = [];

    public RecipeDialog(string label, IReadOnlyList<IngredientLookupDto> ingredients, IReadOnlyList<StaffRecipeLineDto> current) : base("Công thức")
    {
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            HeaderText = "Nguyên liệu", FillWeight = 62, DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            DataSource = ingredients.Select(i => new IdChoice(i.IngredientId, $"{i.IngredientName} ({i.UnitName})")).ToList(),
            ValueMember = nameof(IdChoice.Value), DisplayMember = nameof(IdChoice.Text),
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Định lượng", FillWeight = 38 });
        _grid.DataError += (_, e) => e.ThrowException = false;
        foreach (var line in current) _grid.Rows.Add(line.IngredientId, Display.Number(line.Quantity));
        if (_grid.Rows.Count == 0) _grid.Rows.Add();

        var tools = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 4, 0, 8) };
        tools.Controls.Add(Ui.Button("Thêm dòng", (_, _) => { _grid.Rows.Add(); }));
        tools.Controls.Add(Ui.Button("Xóa dòng", (_, _) =>
        {
            if (_grid.CurrentRow is { IsNewRow: false } row) _grid.Rows.Remove(row);
        }));

        AddRow(new Label { Text = label, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) });
        AddRow(new Label
        {
            Text = "Định lượng cho một ly/phần. Dùng dấu phẩy cho số lẻ (ví dụ 12,5); mỗi nguyên liệu chỉ một dòng.",
            AutoSize = true, MaximumSize = new Size(480, 0), Margin = new Padding(0, 0, 0, 4),
        });
        AddRow(_grid);
        AddRow(tools);
        AddRow(Ui.Labeled("Hiệu lực từ (giờ máy này)", _from));
    }

    protected override bool TryAccept()
    {
        _grid.EndEdit();
        foreach (DataGridViewRow row in _grid.Rows) row.ErrorText = "";
        var lines = new List<RecipeLineRequest>();
        var seen = new HashSet<long>();
        string? firstError = null;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var selected = row.Cells[0].Value;
            var qtyText = row.Cells[1].Value?.ToString();
            if (selected is null && string.IsNullOrWhiteSpace(qtyText)) continue; // dòng trống bị bỏ qua
            string? error = null;
            long id = 0;
            decimal quantity = 0;
            if (selected is not long picked) error = "Chọn nguyên liệu.";
            else if (!seen.Add(id = picked)) error = "Nguyên liệu này đã có ở dòng khác.";
            else if (!StaffInput.TryParseQuantity(qtyText, out quantity, out var qtyError)) error = qtyError;
            if (error is not null) { row.ErrorText = error; firstError ??= error; continue; }
            lines.Add(new RecipeLineRequest(id, quantity));
        }
        if (firstError is not null) return Fail(_grid, firstError);
        if (lines.Count == 0) return Fail(_grid, "Công thức cần ít nhất một nguyên liệu.");
        Lines = lines;
        EffectiveFromUtc = StaffInput.ToUtc(_from.Value);
        return true;
    }
}
