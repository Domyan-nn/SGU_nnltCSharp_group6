using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// D00 – Đăng nhập nhân viên. Gọi POST /api/auth/login qua ApiClient; không lưu mật khẩu, không ghi log.
public sealed class LoginForm : Form
{
    private readonly ApiClient _api;
    private readonly TextBox _user = new() { Name = "userName", MaxLength = 100, AccessibleName = "Tên tài khoản" };
    private readonly TextBox _password = new() { Name = "password", MaxLength = 200, UseSystemPasswordChar = true, AccessibleName = "Mật khẩu" };
    private readonly Button _login = new() { Name = "login", Text = "Đăng nhập", AutoSize = true, AccessibleName = "Đăng nhập" };
    private readonly Label _status = new() { Name = "status", AutoSize = true, MaximumSize = new Size(380, 0), AccessibleRole = AccessibleRole.Alert };
    private readonly ErrorProvider _errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };

    public LoginForm(ApiClient api)
    {
        _api = api;
        Text = "Đăng nhập nhân viên – Quản lý quán cà phê";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Đăng nhập nhân viên", Font = new Font(Font.FontFamily, 14f, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
        layout.Controls.Add(Field("Tên tài khoản", _user));
        layout.Controls.Add(Field("Mật khẩu", _password));
        layout.Controls.Add(_status);
        layout.Controls.Add(_login);
        Controls.Add(layout);

        _user.TabIndex = 0; _password.TabIndex = 1; _login.TabIndex = 2;
        AcceptButton = _login;
        _login.Click += async (_, _) => await SignInAsync();
        ShowStatus("");
    }

    private static Control Field(string label, TextBox box)
    {
        var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        box.Width = 380;
        panel.Controls.Add(box);
        return panel;
    }

    private void ShowStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.ForeColor = error ? Color.Firebrick : SystemColors.ControlText;
        _status.Margin = new Padding(0, text.Length == 0 ? 0 : 4, 0, text.Length == 0 ? 0 : 8);
    }

    private async Task SignInAsync()
    {
        _errors.Clear();
        var userName = _user.Text.Trim();
        if (userName.Length == 0) { _errors.SetError(_user, "Nhập tên tài khoản."); _user.Focus(); return; }
        if (_password.Text.Length == 0) { _errors.SetError(_password, "Nhập mật khẩu."); _password.Focus(); return; }

        SetBusy(true);
        ShowStatus("Đang đăng nhập...");
        try
        {
            var result = await _api.LoginAsync(userName, _password.Text);
            if (result.Ok) { DialogResult = DialogResult.OK; Close(); return; }
            _password.Clear();
            ShowStatus(MessageFor(result), error: true);
            _password.Focus();
        }
        finally { SetBusy(false); }
    }

    // Tách riêng để có thể kiểm thử: mất kết nối hoặc chưa xác định không được báo như sai mật khẩu.
    internal static string MessageFor<T>(ApiResult<T> result) => result switch
    {
        { Unknown: true } => result.Message,
        { StatusCode: 401 } => "Sai tên tài khoản hoặc mật khẩu, hoặc tài khoản đã bị khóa.",
        { StatusCode: 403 } => result.Message,
        _ => result.Message,
    };

    // Khóa nút khi đang gửi để tránh bấm hai lần; chống trùng thật sự vẫn ở máy chủ.
    private void SetBusy(bool busy)
    {
        _login.Enabled = !busy; _user.Enabled = !busy; _password.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
