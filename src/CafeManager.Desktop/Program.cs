using CafeManager.Client;

namespace CafeManager.Desktop;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        // Địa chỉ máy chủ lấy từ biến môi trường CAFE_API_BASE_URL (mặc định https://localhost:7034).
        using var api = new ApiClient(ApiClientOptions.FromEnvironment());
        while (true)
        {
            using (var login = new LoginForm(api))
                if (login.ShowDialog() != DialogResult.OK) return;
            using var main = new Form1(api);
            Application.Run(main);
            // Hết phiên (401) thì quay về D00; đóng cửa sổ chủ động thì thoát.
            if (!main.SessionExpired) return;
        }
    }
}
