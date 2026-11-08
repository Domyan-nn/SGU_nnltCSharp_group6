namespace CafeManager.Desktop;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Text = "Quản lý quán cà phê — Nhóm 6";
        Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            Text = "Khung WinForms. Các chức năng tác nghiệp sẽ gọi máy chủ qua HTTPS/API.\nChưa triển khai màn hình nghiệp vụ." });
    }
}
