using CafeManager.Client;

namespace CafeManager.Desktop;

// Cửa sổ chính (khung). Màn hình tác nghiệp sẽ hiện theo quyền trả về lúc đăng nhập.
public partial class Form1 : Form
{
    private readonly ApiClient? _api;

    public bool SessionExpired { get; private set; }

    public Form1() : this(null) { }

    public Form1(ApiClient? api)
    {
        _api = api;
        InitializeComponent();
        Text = "Quản lý quán cà phê — Nhóm 6";
        var user = api?.CurrentUser;
        var text = user == null
            ? "Khung WinForms. Các chức năng tác nghiệp sẽ gọi máy chủ qua HTTPS/API.\nChưa triển khai màn hình nghiệp vụ."
            : $"Xin chào {user.DisplayName}.\nQuyền: {(user.Permissions.Count == 0 ? "(chưa được cấp quyền)" : string.Join(", ", user.Permissions))}\n\nCác màn hình tác nghiệp sẽ hiện theo quyền (bước tiếp theo).";
        Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = text });
        if (api != null)
        {
            api.SessionExpired += OnSessionExpired;
            FormClosed += (_, _) => api.SessionExpired -= OnSessionExpired;
        }
    }

    // Phiên hết hạn hoặc bị thu hồi: báo rõ rồi quay về đăng nhập, không giữ dữ liệu nhạy cảm trên màn hình.
    private void OnSessionExpired()
    {
        if (SessionExpired) return;
        SessionExpired = true;
        if (IsHandleCreated) BeginInvoke(() =>
        {
            MessageBox.Show(this, "Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        });
    }
}
