using System.Globalization;
using System.Text;
using CafeManager.Core;

namespace CafeManager.Client;

// Logic hiển thị và xuất tệp cho màn hình Báo cáo doanh thu (B4), tách khỏi Windows Forms để kiểm thử được
// (cùng cách làm với StaffPresentation.cs và CashierPresentation.cs).

public static class ReportText
{
    public static string Day(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string Range(DateOnly from, DateOnly to) => from == to ? Day(from) : Day(from) + " – " + Day(to);

    public static string Summary(RevenueReportDto r) =>
        $"Doanh thu thuần: {Display.Money(r.Net)}   ·   Đã thu: {Display.Money(r.Paid)}   ·   Đã hoàn: {Display.Money(r.Refunded)}   ·   Số đơn đã thu: {r.PaidOrders}";
}

// Trục tung "tròn" cho biểu đồ cột: làm tròn lên bội 1, 2 hoặc 5 của lũy thừa 10.
public static class ChartScale
{
    public const decimal EmptyMax = 100_000m;

    public static decimal NiceMax(decimal max)
    {
        if (max <= 0) return EmptyMax;
        var exponent = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)max)));
        var factor = max / exponent;
        var nice = factor <= 1 ? 1 : factor <= 2 ? 2 : factor <= 5 ? 5 : 10;
        return nice * exponent;
    }
}

// Tệp CSV mở được bằng Excel: dòng "sep=;" để Excel nhận đúng dấu phân cách ở mọi ngôn ngữ máy,
// ngày dạng ISO (yyyy-MM-dd), số dùng dấu phẩy thập phân kiểu Việt Nam. Người gọi ghi tệp bằng UTF-8 có BOM để giữ tiếng Việt.
public static class ReportCsv
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Build(RevenueReportDto report)
    {
        var sb = new StringBuilder();
        sb.Append("sep=;\r\n");
        Row(sb, "Báo cáo doanh thu", Iso(report.From) + " đến " + Iso(report.To));
        Row(sb, "Doanh thu thuần", Number(report.Net));
        Row(sb, "Đã thu", Number(report.Paid));
        Row(sb, "Đã hoàn", Number(report.Refunded));
        Row(sb, "Số đơn đã thu", report.PaidOrders.ToString(CultureInfo.InvariantCulture));
        sb.Append("\r\n");
        Row(sb, "Doanh thu theo ngày");
        Row(sb, "Ngày", "Doanh thu thuần", "Số đơn");
        foreach (var d in report.Days) Row(sb, Iso(d.Date), Number(d.Revenue), d.Orders.ToString(CultureInfo.InvariantCulture));
        sb.Append("\r\n");
        Row(sb, "Món bán chạy");
        Row(sb, "Hạng", "Món", "Số ly", "Doanh thu");
        var rank = 1;
        foreach (var p in report.TopProducts)
            Row(sb, (rank++).ToString(CultureInfo.InvariantCulture), p.ProductName, p.Quantity.ToString(CultureInfo.InvariantCulture), Number(p.Revenue));
        return sb.ToString();
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Number(decimal value) => value.ToString("0.##", Vi);

    private static void Row(StringBuilder sb, params string[] cells)
    {
        sb.Append(string.Join(';', cells.Select(Cell))).Append("\r\n");
    }

    // Bao dấu nháy khi có dấu chấm phẩy, nháy kép hoặc xuống dòng; chặn công thức Excel (=, +, -, @) bằng dấu nháy đơn ở đầu.
    public static string Cell(string text)
    {
        var t = text;
        if (t.Length > 0 && (t[0] is '=' or '+' or '-' or '@' or '\t' or '\r')) t = "'" + t;
        if (t.IndexOfAny([';', '"', '\r', '\n']) >= 0) t = "\"" + t.Replace("\"", "\"\"") + "\"";
        return t;
    }
}
