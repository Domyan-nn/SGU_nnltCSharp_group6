using System.Drawing.Drawing2D;
using System.Text;
using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// B4 – Báo cáo doanh thu (UC15, quyền MENU_MANAGE): chọn khoảng ngày, xem doanh thu thuần theo ngày (biểu đồ cột và bảng),
// món bán chạy, và xuất CSV. Chỉ đọc; số liệu do máy chủ tính (xem StaffReportService). Khi tải lỗi vẫn giữ báo cáo lần trước và báo rõ.
public sealed class RevenueReportForm : Form
{
    private readonly StaffApi _staff;
    private readonly DateTimePicker _from = new() { Name = "from", Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker _to = new() { Name = "to", Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly Button _load = Ui.Button("Xem báo cáo (F5)", primary: true);
    private readonly Button _week = Ui.Button("7 ngày");
    private readonly Button _month = Ui.Button("30 ngày");
    private readonly Button _export = Ui.Button("Xuất CSV");
    private readonly Label _summary = new() { Name = "summary", AutoSize = true, Margin = new Padding(8, 4, 8, 4) };
    private readonly RevenueChart _chart = new() { Name = "chart", Dock = DockStyle.Fill };
    private readonly DataGridView _daily = new() { Name = "daily" };
    private readonly DataGridView _top = new() { Name = "top" };
    private readonly Label _status = Ui.StatusLabel("status");
    private RevenueReportDto? _report;
    private bool _busy;

    public RevenueReportForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Báo cáo doanh thu (B4)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 700);
        MinimumSize = new Size(900, 560);
        KeyPreview = true;

        var today = Display.ToVietnam(DateTime.UtcNow).Date;
        _to.MaxDate = today.AddYears(1);
        _from.MaxDate = today.AddYears(1);
        _to.Value = today;
        _from.Value = today.AddDays(-6);
        _summary.Font = new Font(Font, FontStyle.Bold);
        _summary.Text = "Chọn khoảng ngày rồi bấm Xem báo cáo.";

        Ui.ConfigureGrid(_daily, "Doanh thu theo ngày");
        _daily.Columns.Add(Ui.Column("Ngày", 20, auto: true));
        _daily.Columns.Add(Ui.Column("Doanh thu thuần", 50, alignRight: true));
        _daily.Columns.Add(Ui.Column("Số đơn", 30, alignRight: true, auto: true));

        Ui.ConfigureGrid(_top, "Món bán chạy");
        _top.Columns.Add(Ui.Column("Hạng", 10, alignRight: true, auto: true));
        _top.Columns.Add(Ui.Column("Món", 50, minWidth: 140));
        _top.Columns.Add(Ui.Column("Số ly", 15, alignRight: true, auto: true));
        _top.Columns.Add(Ui.Column("Doanh thu", 35, alignRight: true));

        _load.Click += async (_, _) => await LoadAsync();
        _week.Click += async (_, _) => { SetRange(7); await LoadAsync(); };
        _month.Click += async (_, _) => { SetRange(30); await LoadAsync(); };
        _export.Click += (_, _) => ExportCsv();
        _export.Enabled = false;

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.Add(new Label { Text = "Từ ngày", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        toolbar.Controls.Add(_from);
        toolbar.Controls.Add(new Label { Text = "Đến ngày", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        toolbar.Controls.Add(_to);
        toolbar.Controls.Add(_week);
        toolbar.Controls.Add(_month);
        toolbar.Controls.Add(_load);
        toolbar.Controls.Add(_export);

        var chartBox = new GroupBox { Text = "Doanh thu thuần theo ngày", Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
        chartBox.Controls.Add(_chart);

        var dailyBox = new GroupBox { Text = "Chi tiết theo ngày", Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
        dailyBox.Controls.Add(_daily);
        var topBox = new GroupBox { Text = "Món bán chạy (theo số ly)", Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
        topBox.Controls.Add(_top);
        var tables = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill };
        tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        tables.Controls.Add(dailyBox, 0, 0);
        tables.Controls.Add(topBox, 1, 0);

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(toolbar);
        root.Controls.Add(_summary);
        root.Controls.Add(chartBox);
        root.Controls.Add(tables);
        root.Controls.Add(_status);
        Controls.Add(root);

        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadAsync(); }
        };
        Shown += async (_, _) => await LoadAsync();
    }

    private void SetRange(int days)
    {
        var today = Display.ToVietnam(DateTime.UtcNow).Date;
        _to.Value = today;
        _from.Value = today.AddDays(-(days - 1));
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        _load.Enabled = !busy;
        _week.Enabled = !busy;
        _month.Enabled = !busy;
        _export.Enabled = !busy && _report is not null;
    }

    private async Task LoadAsync()
    {
        if (_busy || IsDisposed) return;
        var from = DateOnly.FromDateTime(_from.Value);
        var to = DateOnly.FromDateTime(_to.Value);
        if (to < from) { Ui.SetStatus(_status, "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.", error: true); _to.Focus(); return; }
        if (to.DayNumber - from.DayNumber + 1 > 366) { Ui.SetStatus(_status, "Khoảng báo cáo tối đa 366 ngày.", error: true); return; }

        SetBusy(true);
        try
        {
            var result = await _staff.RevenueReportAsync(from, to);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                Ui.SetStatus(_status, StaffMessages.ForLoad(result, "báo cáo doanh thu")
                    + (_report is null ? "" : " Đang hiển thị báo cáo của lần tải trước."), error: true);
                return;
            }
            _report = result.Value!;
            Bind(_report);
            Ui.SetStatus(_status, $"Đã tải báo cáo {ReportText.Range(_report.From, _report.To)} lúc {DateTime.Now:HH:mm:ss}.");
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void Bind(RevenueReportDto report)
    {
        _summary.Text = ReportText.Summary(report);
        _chart.SetData(report.Days);

        _daily.Rows.Clear();
        foreach (var d in report.Days)
            _daily.Rows.Add(ReportText.Day(d.Date), Display.Money(d.Revenue), d.Orders);

        _top.Rows.Clear();
        var rank = 1;
        foreach (var p in report.TopProducts)
            _top.Rows.Add(rank++, p.ProductName, p.Quantity, Display.Money(p.Revenue));
        if (report.TopProducts.Count == 0)
            _top.Rows.Add("", "Chưa có món nào được bán trong kỳ này.", "", "");
    }

    private void ExportCsv()
    {
        var report = _report;
        if (report is null || _busy) return;
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất báo cáo doanh thu",
            Filter = "Tệp CSV (*.csv)|*.csv",
            FileName = $"bao-cao-doanh-thu_{report.From:yyyy-MM-dd}_{report.To:yyyy-MM-dd}.csv",
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            // UTF-8 có BOM để Excel đọc đúng tiếng Việt.
            File.WriteAllText(dialog.FileName, ReportCsv.Build(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Ui.SetStatus(_status, $"Đã xuất báo cáo ra {dialog.FileName}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = "Không ghi được tệp: " + ex.Message + " Hãy chọn thư mục khác hoặc đóng tệp nếu đang mở bằng Excel.";
            Ui.SetStatus(_status, message, error: true);
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

// Biểu đồ cột doanh thu thuần theo ngày, vẽ bằng GDI+ (.NET không kèm sẵn thư viện biểu đồ cho Windows Forms).
// Ngày doanh thu âm (hoàn nhiều hơn thu) vẽ cột 0; số liệu chính xác nằm ở bảng bên dưới. Di chuột lên cột để xem chi tiết.
internal sealed class RevenueChart : Control
{
    // Đặt tên khác Left/Top/Right/Bottom để không che thuộc tính cùng tên của Control (cảnh báo CS0108 bị coi là lỗi).
    private const int PadLeft = 104, PadTop = 16, PadRight = 12, PadBottom = 34;
    private readonly ToolTip _tip = new();
    private IReadOnlyList<DailyRevenueDto> _days = [];
    private int _hover = -1;

    public RevenueChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        MinimumSize = new Size(300, 150);
        AccessibleName = "Biểu đồ doanh thu thuần theo ngày";
        AccessibleRole = AccessibleRole.Chart;
    }

    public void SetData(IReadOnlyList<DailyRevenueDto> days)
    {
        _days = days;
        _hover = -1;
        _tip.SetToolTip(this, "");
        Invalidate();
    }

    private Rectangle Plot => new(PadLeft, PadTop, Math.Max(0, Width - PadLeft - PadRight), Math.Max(0, Height - PadTop - PadBottom));

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var plot = Plot;
        var index = -1;
        if (_days.Count > 0 && plot.Width > 0 && plot.Contains(e.Location))
            index = Math.Min(_days.Count - 1, (e.X - plot.Left) * _days.Count / plot.Width);
        if (index == _hover) return;
        _hover = index;
        _tip.SetToolTip(this, index < 0 ? "" : $"{ReportText.Day(_days[index].Date)}: {Display.Money(_days[index].Revenue)}, {_days[index].Orders} đơn");
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hover < 0) return;
        _hover = -1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(SystemColors.Window);
        var plot = Plot;
        if (plot.Width < 60 || plot.Height < 40) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var max = ChartScale.NiceMax(_days.Count == 0 ? 0m : _days.Max(d => Math.Max(0m, d.Revenue)));
        using var gridPen = new Pen(Color.Gainsboro);
        using var axisBrush = new SolidBrush(SystemColors.GrayText);
        using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Bottom - plot.Height * i / 4;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            g.DrawString(Display.Money(max * i / 4), Font, axisBrush, new RectangleF(0, y - 10, PadLeft - 6, 20), right);
        }

        if (_days.Count == 0)
        {
            g.DrawString("Chưa có dữ liệu.", Font, axisBrush, plot, center);
            return;
        }

        var slot = (float)plot.Width / _days.Count;
        var barWidth = Math.Max(2f, slot * 0.7f);
        var labelStep = Math.Max(1, (int)Math.Ceiling(52f / slot));
        using var barBrush = new SolidBrush(SystemColors.Highlight);
        using var hoverBrush = new SolidBrush(ControlPaint.Dark(SystemColors.Highlight));
        for (var i = 0; i < _days.Count; i++)
        {
            var height = (float)(Math.Max(0m, _days[i].Revenue) / max) * plot.Height;
            var x = plot.Left + i * slot + (slot - barWidth) / 2;
            if (height >= 0.5f) g.FillRectangle(i == _hover ? hoverBrush : barBrush, x, plot.Bottom - height, barWidth, height);
            if (i % labelStep == 0)
                g.DrawString(_days[i].Date.ToString("dd/MM"), Font, axisBrush, new RectangleF(x + barWidth / 2 - 26, plot.Bottom + 4, 52, 20), center);
        }

        if (_days.All(d => d.Revenue <= 0))
            g.DrawString("Không có doanh thu trong kỳ này.", Font, axisBrush, plot, center);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
