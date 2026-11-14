using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

public partial class Form1 : Form
{
    private readonly ApiClient? _api;
    private readonly FlowLayoutPanel _actions = new()
    {
        AutoSize = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };

    public bool ReturnToLogin { get; private set; }

    public Form1() : this(null) { }

    public Form1(ApiClient? api)
    {
        _api = api;
        InitializeComponent();
        Text = "Quản lý quán cà phê — Nhóm 6";

        var user = api?.CurrentUser;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(24),
            ColumnCount = 1
        };

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
            Text = user is null ? "Chưa đăng nhập." : $"Xin chào {user.DisplayName}"
        });

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 12),
            Text = "Chọn chức năng theo quyền được cấp:"
        });

        if (user?.Permissions.Contains(Permissions.MenuManage) == true)
            _actions.Controls.Add(CreateActionButton("Quản lý món", OpenMenuManagement));

        if (user?.Permissions.Contains(Permissions.OrderReceive) == true)
            _actions.Controls.Add(CreateActionButton("Tiếp nhận đơn", OpenOrderReceiving));

        if (_actions.Controls.Count == 0)
            _actions.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Tài khoản chưa được cấp quyền tác nghiệp."
            });

        _actions.Controls.Add(CreateActionButton("Đăng xuất", LogoutAsync));
        layout.Controls.Add(_actions);
        Controls.Add(layout);

        if (api is not null)
        {
            api.SessionExpired += OnSessionExpired;
            FormClosed += (_, _) => api.SessionExpired -= OnSessionExpired;
        }
    }

    private static Button CreateActionButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(180, 32)
        };
        button.Click += handler;
        return button;
    }

    private void OpenMenuManagement(object? sender, EventArgs e)
    {
        if (_api is null) return;
        using var form = new MenuManagementForm(_api);
        form.ShowDialog(this);
    }

    private void OpenOrderReceiving(object? sender, EventArgs e)
    {
        if (_api is null) return;
        using var form = new OrderReceivingForm(_api);
        form.ShowDialog(this);
    }

    private async void LogoutAsync(object? sender, EventArgs e)
    {
        if (_api is null) return;

        SetActionsEnabled(false);
        try
        {
            var result = await _api.LogoutAsync();
            if (!result.Ok && !result.Unknown)
            {
                MessageBox.Show(this, result.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ReturnToLogin = true;
            Close();
        }
        finally
        {
            if (!IsDisposed) SetActionsEnabled(true);
        }
    }

    private void OnSessionExpired()
    {
        if (ReturnToLogin) return;
        ReturnToLogin = true;

        if (IsHandleCreated)
        {
            BeginInvoke(() =>
            {
                // Đóng các màn hình con (D02/D06 và hộp thoại của chúng) trước, nếu không cửa sổ chính không đóng được.
                foreach (var form in Application.OpenForms.Cast<Form>().Where(f => f != this).Reverse().ToList())
                    if (!form.IsDisposed) form.Close();
                MessageBox.Show(this,
                    "Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            });
        }
    }

    private void SetActionsEnabled(bool enabled)
    {
        foreach (Control control in _actions.Controls)
            control.Enabled = enabled;
    }
}