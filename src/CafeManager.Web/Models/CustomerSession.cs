namespace CafeManager.Web.Models;

public sealed record CustomerIdentity(string UserName, string DisplayName);

/// <summary>
/// Trạng thái đăng nhập của khách trên Web, giữ trong session. Tạm thời cho đến khi A làm đăng nhập cookie (UC19);
/// khi đó thay phần đọc/ghi ở đây, Controller giữ nguyên.
/// </summary>
public static class CustomerSession
{
    private const string UserKey = "cust.user";
    private const string NameKey = "cust.name";
    private const string LastOrderKey = "last-order";

    public static CustomerIdentity? Current(HttpContext context)
    {
        if (!CartStore.IsAvailable(context)) return null;
        var user = context.Session.GetString(UserKey);
        if (string.IsNullOrEmpty(user)) return null;
        return new CustomerIdentity(user, context.Session.GetString(NameKey) ?? user);
    }

    public static void SignIn(HttpContext context, string userName, string displayName)
    {
        context.Session.SetString(UserKey, userName);
        context.Session.SetString(NameKey, displayName);
    }

    /// <summary>Xóa toàn bộ session (gồm giỏ hàng) để máy dùng chung không lộ dữ liệu.</summary>
    public static void SignOut(HttpContext context) => context.Session.Clear();

    public static string LoginUrl(string returnPath) =>
        "/tai-khoan/dang-nhap?returnUrl=" + Uri.EscapeDataString(returnPath);

    public static void SaveLastOrder(HttpContext context, string json) => context.Session.SetString(LastOrderKey, json);

    public static string? LoadLastOrder(HttpContext context) => context.Session.GetString(LastOrderKey);
}
