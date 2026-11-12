using System.Globalization;

namespace CafeManager.Web.Models;

public static class Money
{
    /// <summary>Hiển thị tiền VNĐ, ví dụ 25.000 đ. Chỉ để hiển thị.</summary>
    public static string Vnd(decimal amount) =>
        amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.') + " đ";
}
