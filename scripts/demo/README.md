# Script demo

Ba script dựng và chạy demo trên một CSDL riêng (instance LocalDB `CafeDemoNhom6`), không đụng CSDL Dev của máy. Mã nguồn lấy từ chính repo chứa script, nên hãy `git pull` nhánh `main` trước khi chạy.

| Script | Việc |
| --- | --- |
| `tao-demo.ps1` | Xóa và tạo lại CSDL demo sạch, build, chạy `--migrate --seed --seed-accounts`, ghi tài khoản mẫu và mật khẩu vào `du-lieu\tai-khoan-demo.txt` |
| `chay-demo.ps1` | Mở Web ở `http://localhost:5055` và Desktop trỏ vào Web đó |
| `dung-demo.ps1` | Dừng Web, đóng Desktop, dừng instance LocalDB (dữ liệu vẫn giữ) |

Chạy từ PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\demo\tao-demo.ps1
powershell -ExecutionPolicy Bypass -File scripts\demo\chay-demo.ps1
powershell -ExecutionPolicy Bypass -File scripts\demo\dung-demo.ps1
```

Thư mục `du-lieu\` (tệp CSDL và mật khẩu) nằm trong `.gitignore`, không commit. Mỗi lần chạy `tao-demo.ps1` mật khẩu đổi. Bản mật khẩu của CSDL demo trên máy dùng để quay được để ở thư mục nhóm trên Drive, không để trong repo.
