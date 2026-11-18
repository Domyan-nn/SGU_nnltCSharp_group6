using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// Hộp thoại của màn hình Thu ngân (A4): mở ca và đóng ca. Kiểm tra sớm bằng CashierInput; máy chủ vẫn kiểm tra lại.

internal sealed class OpenShiftDialog : EditDialog
{
    private readonly ComboBox _counter = new()
    {
        Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(CounterDto.CounterCode), AccessibleName = "Quầy",
    };
    private readonly TextBox _cash = new() { Width = 320, MaxLength = 20, Text = "0", AccessibleName = "Tiền đầu ca" };
    public long CounterId { get; private set; }
    public decimal OpeningCash { get; private set; }

    public OpenShiftDialog(IReadOnlyList<CounterDto> counters) : base("Mở ca")
    {
        Ok.Text = "Mở ca";
        _counter.DataSource = counters.ToList();
        AddRow(Ui.Labeled("Quầy", _counter));
        AddRow(Ui.Labeled("Tiền đầu ca (tiền mặt có trong két lúc mở ca)", _cash));
    }

    protected override bool TryAccept()
    {
        if (_counter.SelectedItem is not CounterDto counter) return Fail(_counter, "Chọn quầy.");
        if (!CashierInput.TryParseCash(_cash.Text, "tiền đầu ca", out var cash, out var error)) return Fail(_cash, error);
        CounterId = counter.CounterId;
        OpeningCash = cash;
        return true;
    }
}

internal sealed class CloseShiftDialog : EditDialog
{
    private readonly TextBox _counted = new() { Width = 380, MaxLength = 20, AccessibleName = "Tiền đếm thực tế" };
    private readonly TextBox _note = new()
    {
        Width = 380, Height = 70, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical, AccessibleName = "Ghi chú đóng ca",
    };
    public decimal CountedCash { get; private set; }
    public string? Note { get; private set; }

    public CloseShiftDialog(ShiftDto shift) : base("Đóng ca")
    {
        Ok.Text = "Đóng ca";
        AddRow(new Label
        {
            Text = $"Đóng ca tại quầy {shift.CounterCode} (mở lúc {Display.Time(shift.OpenedAt)}). " +
                   "Đếm toàn bộ tiền mặt trong két rồi nhập vào đây; hệ thống sẽ đối soát và báo chênh lệch. Đóng ca xong không mở lại được.",
            AutoSize = true, MaximumSize = new Size(400, 0), Margin = new Padding(0, 0, 0, 8),
        });
        AddRow(Ui.Labeled("Tiền đếm thực tế trong két", _counted));
        AddRow(Ui.Labeled("Ghi chú (không bắt buộc)", _note));
    }

    protected override bool TryAccept()
    {
        if (!CashierInput.TryParseCash(_counted.Text, "tiền đếm thực tế", out var counted, out var error)) return Fail(_counted, error);
        var note = _note.Text.Trim();
        if (note.Length > 500) return Fail(_note, "Ghi chú tối đa 500 ký tự.");
        CountedCash = counted;
        Note = note.Length == 0 ? null : note;
        return true;
    }
}
