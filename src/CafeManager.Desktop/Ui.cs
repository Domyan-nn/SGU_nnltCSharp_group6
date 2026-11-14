using CafeManager.Client;

namespace CafeManager.Desktop;

// Tiện ích dựng giao diện bằng mã cho D02/D06: nút, lưới, nhãn trạng thái và khung hộp thoại nhập liệu.
internal static class Ui
{
    public static Button Button(string text, EventHandler? onClick = null, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(96, 32), Margin = new Padding(0, 0, 8, 0), AccessibleName = text,
        };
        if (primary) button.Font = new Font(button.Font, FontStyle.Bold);
        if (onClick != null) button.Click += onClick;
        return button;
    }

    public static Control Labeled(string label, Control input)
    {
        var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    public static void ConfigureGrid(DataGridView grid, string accessibleName)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.Dock = DockStyle.Fill;
        grid.AccessibleName = accessibleName;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False; // tiêu đề một dòng, không làm cao hàng tiêu đề
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    }

    // auto = true: cột vừa đủ nội dung (mã đơn, giờ, tiền không bị cắt); false: cột co giãn lấp phần còn lại theo trọng số.
    public static DataGridViewTextBoxColumn Column(string header, float fillWeight, bool alignRight = false, bool auto = false, int minWidth = 60)
    {
        var column = new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = fillWeight, MinimumWidth = minWidth, SortMode = DataGridViewColumnSortMode.NotSortable };
        if (auto) column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        if (alignRight) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    // Nhãn trạng thái: lỗi có chữ "Lỗi" đứng đầu để không chỉ phân biệt bằng màu.
    public static Label StatusLabel(string name) => new()
    {
        Name = name, AutoSize = false, Dock = DockStyle.Fill, Padding = new Padding(2, 4, 2, 0),
        AccessibleRole = AccessibleRole.Alert, TextAlign = ContentAlignment.TopLeft,
    };

    public static void SetStatus(Label label, string text, bool error = false)
    {
        label.Text = error && text.Length > 0 ? "Lỗi: " + text : text;
        label.ForeColor = error ? Color.Firebrick : SystemColors.ControlText;
    }
}

internal sealed record IdChoice(long Value, string Text);
internal sealed record TextChoice(string Value, string Text);

// Khung hộp thoại nhập liệu: nút Lưu/Hủy, ErrorProvider và dòng lỗi chung; lớp con thêm trường và kiểm tra trong TryAccept.
internal abstract class EditDialog : Form
{
    protected readonly ErrorProvider Errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    protected readonly Button Ok = new() { Text = "Lưu", AutoSize = true, MinimumSize = new Size(110, 32), Margin = new Padding(8, 8, 0, 0) };
    protected readonly Button Cancel = new() { Text = "Hủy", AutoSize = true, MinimumSize = new Size(96, 32), DialogResult = DialogResult.Cancel, Margin = new Padding(8, 8, 0, 0) };
    private readonly Label _hint = new() { AutoSize = true, MaximumSize = new Size(460, 0), ForeColor = Color.Firebrick, Margin = new Padding(0, 0, 0, 4) };
    private readonly TableLayoutPanel _body = new() { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };

    protected EditDialog(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);
        AcceptButton = Ok; CancelButton = Cancel;
        Controls.Add(_body);
        Ok.Click += (_, _) =>
        {
            Errors.Clear(); _hint.Text = "";
            if (!TryAccept()) return;
            DialogResult = DialogResult.OK;
            Close();
        };
    }

    protected void AddRow(Control control) => _body.Controls.Add(control);

    // Trả false và báo lỗi gần trường nhập; dùng làm kết quả của TryAccept.
    protected bool Fail(Control field, string message)
    {
        Errors.SetError(field, message);
        _hint.Text = message;
        field.Focus();
        return false;
    }

    protected abstract bool TryAccept();

    protected override void OnLoad(EventArgs e)
    {
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(Cancel);
        buttons.Controls.Add(Ok);
        _body.Controls.Add(_hint);
        _body.Controls.Add(buttons);
        base.OnLoad(e);
    }
}
