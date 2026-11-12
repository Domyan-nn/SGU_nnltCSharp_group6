# Đề nghị A sửa `src/CafeManager.Web/Program.cs` (3 dòng) – giỏ hàng session của B (T4)

Giỏ hàng (W01) lưu trong session theo đề bài. B không được sửa `Program.cs`, nên nhờ A thêm:

```csharp
// trước builder.Build() (cạnh AddControllersWithViews)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.Name = ".CafeManager.Cart";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// sau app.UseStaticFiles(); và trước app.MapControllers();
app.UseSession();
```

Chưa thêm thì Web vẫn chạy bình thường; giỏ hàng tắt và trang báo rõ thiếu cấu hình.
Khi A làm đăng nhập cookie (UC19), có thể gộp hai cấu hình cookie.
