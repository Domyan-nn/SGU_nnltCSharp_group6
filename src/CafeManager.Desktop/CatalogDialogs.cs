using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// Hộp thoại thêm/sửa danh mục (C1) và nguyên liệu (C2). Kiểm tra sớm bằng StaffInput; máy chủ vẫn kiểm tra lại.

internal sealed class CategoryDialog : EditDialog
{
    private readonly TextBox _name = new() { Width = 380, MaxLength = 200, AccessibleName = "Tên danh mục" };
    private readonly CheckBox _active = new() { Text = "Đang dùng", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
    public CategoryUpsertRequest? Result { get; private set; }

    public CategoryDialog(CategoryDto? existing) : base(existing is null ? "Thêm danh mục" : "Sửa danh mục")
    {
        if (existing is not null)
        {
            _name.Text = existing.CategoryName;
            _active.Checked = existing.IsActive;
        }
        AddRow(Ui.Labeled("Tên danh mục", _name));
        AddRow(_active);
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryName(_name.Text, "tên danh mục", out var name, out var error)) return Fail(_name, error);
        Result = new CategoryUpsertRequest(name, _active.Checked);
        return true;
    }
}

internal sealed class IngredientDialog : EditDialog
{
    private readonly TextBox _name = new() { Width = 380, MaxLength = 200, AccessibleName = "Tên nguyên liệu" };
    private readonly TextBox _unit = new() { Width = 380, MaxLength = 50, AccessibleName = "Đơn vị tính" };
    private readonly CheckBox _active = new() { Text = "Đang dùng", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
    public IngredientUpsertRequest? Result { get; private set; }

    public IngredientDialog(IngredientDto? existing) : base(existing is null ? "Thêm nguyên liệu" : "Sửa nguyên liệu")
    {
        if (existing is not null)
        {
            _name.Text = existing.IngredientName;
            _unit.Text = existing.UnitName;
            _active.Checked = existing.IsActive;
        }
        AddRow(Ui.Labeled("Tên nguyên liệu", _name));
        AddRow(Ui.Labeled("Đơn vị tính (ví dụ g, ml, cái)", _unit));
        AddRow(_active);
    }

    protected override bool TryAccept()
    {
        if (!StaffInput.TryName(_name.Text, "tên nguyên liệu", out var name, out var error)) return Fail(_name, error);
        if (!StaffInput.TryName(_unit.Text, "đơn vị tính", out var unit, out error)) return Fail(_unit, error);
        if (unit.Length > 50) return Fail(_unit, "Đơn vị tính tối đa 50 ký tự.");
        Result = new IngredientUpsertRequest(name, unit, _active.Checked);
        return true;
    }
}
