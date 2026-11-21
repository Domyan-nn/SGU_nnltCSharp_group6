# Hướng dẫn chuẩn bị và chạy demo

Cập nhật 21/11/2026. Luồng chính: quản lý tạo món (Desktop) → khách đặt đơn trên Web → thu ngân tiếp nhận (Desktop) → pha chế từng ly → thu tiền mặt → đóng ca; khách theo dõi tiến độ trên Web. Các ca kiểm thử tương ứng nằm ở [kiem-thu.md](kiem-thu.md); lời thoại và phân cảnh quay video ở [kich-ban-demo.md](kich-ban-demo.md).

## 1. Chuẩn bị một lần (máy demo)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-may.ps1   # kiểm tra máy, chỉ đọc
dotnet --version                       # SDK 10.0.403 (global.json, rollForward latestPatch)
sqllocaldb info MSSQLLocalDB           # LocalDB phải có
dotnet build CafeManager.sln
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed --seed-accounts
```
- Lệnh cuối tạo CSDL `CafeManager_Group6_Dev`, dữ liệu mẫu và **in mật khẩu ngẫu nhiên của các tài khoản mẫu một lần duy nhất**: ghi lại ngay. Quên mật khẩu thì phải tạo lại CSDL Dev của riêng máy đó.
- Sau khi kéo mã mới về, chạy lại `--migrate` một lần (cần cho migration `PreparationUnit3NF`). Nếu CSDL đã có `dev.manager` từ trước khi có quyền quản lý nhân viên, chạy lại `--seed-accounts` một lần để vai trò Manager nhận quyền `STAFF_MANAGE` (mật khẩu cũ giữ nguyên).
- Nếu Windows báo "Application Control policy has blocked this file" khi build hoặc chạy, máy đang bật Smart App Control; cần tắt (không bật lại được) hoặc dùng máy khác.
- Truy vấn SQL để đối chiếu: SSMS hoặc `sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -I -b -d CafeManager_Group6_Dev -Q "..."`. **Bắt buộc có `-I`** (bật QUOTED_IDENTIFIER), nếu thiếu thì các lệnh `UPDATE` vào bảng có chỉ mục lọc (ví dụ `AspNetUsers`) báo lỗi và không đổi gì.
- Chạy kiểm thử tự động trước buổi demo (cần biến môi trường để các ca SQL không bị Skipped):
```powershell
$env:CAFE_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test CafeManager.sln          # mong đợi 182/182 Đạt, 0 Skipped
```

| Tài khoản | Loại | Quyền | Dùng cho |
|---|---|---|---|
| `dev.manager` | Nhân viên | MENU_MANAGE, STAFF_MANAGE | Quản lý món, Danh mục, Nguyên liệu, Báo cáo doanh thu, Nhân viên và phân quyền |
| `dev.cashier` | Nhân viên | ORDER_RECEIVE, PAYMENT_COLLECT | Tiếp nhận đơn, Thu ngân (mở ca, thu tiền mặt, đóng ca) |
| `dev.barista` | Nhân viên | PREPARATION_WORK | Pha chế |
| `dev.customer` | Khách | — | Web (khách thứ hai tự đăng ký trên Web) |

## 2. Chạy máy chủ Web và API
```powershell
dotnet run --project src/CafeManager.Web --launch-profile http     # http://localhost:5055
```
- Giao diện khách: http://localhost:5055/ (thực đơn từ CSDL, chi tiết món, giỏ hàng, đăng ký, đăng nhập, đặt đơn, đơn của tôi).
- Kiểm tra nhanh API: `curl http://localhost:5055/api/menu` trả danh sách món; `curl http://localhost:5055/api/orders/mine` trả 401.
- Chạy giao diện với dữ liệu giả, không cần CSDL: đặt `CAFE_FAKE_GATEWAY=1` trước khi chạy (không dùng khi demo thật).

## 3. Chạy ứng dụng Desktop
```powershell
$env:CAFE_API_BASE_URL='http://localhost:5055'
dotnet run --project src/CafeManager.Desktop
```
Màn hình chính chỉ hiện các nút đúng quyền của tài khoản. Dùng HTTPS (`https://localhost:7034`) thì chạy một lần `dotnet dev-certs https --trust`, nếu không Desktop báo lỗi SSL; hoặc đặt `CAFE_API_BASE_URL` về địa chỉ HTTP như trên.

## 4. Kịch bản demo (5–7 phút)
1. Desktop, `dev.manager` → Quản lý món: Thêm món → Thêm biến thể → Đổi giá → Công thức → Bật bán (UC17); món hiện trên Web sau vài giây. Có thể mở Danh mục và Nguyên liệu để thêm, sửa, tìm, xóa.
2. Web: đăng ký tài khoản khách rồi đăng nhập (UC18, UC19); xem thực đơn và trang chi tiết món, chọn biến thể và tùy chọn, thêm vào giỏ (UC01).
3. Web: xác nhận đơn với giờ nhận 07:00–22:00 (giờ Việt Nam); thử đổi giá ở bước 1 để thấy cảnh báo "giá đã thay đổi" (UC02).
4. Desktop, `dev.cashier` → Tiếp nhận đơn: chọn đơn, xem chi tiết, Tiếp nhận đơn (UC03); đơn chuyển sang Đang phục vụ. Có thể thử Từ chối đơn… (bắt buộc lý do).
5. Desktop, `dev.barista` → Pha chế: Bắt đầu làm, Làm xong, Đã giao cho khách. Web "Đơn của tôi" hiện tiến độ từng bước (tải lại trạng thái).
6. Desktop, `dev.cashier` → Thu ngân: Mở ca (quầy DEMO-C01), chọn đơn, nhập tiền khách đưa, Thu tiền mặt (hiện tiền thối); Web hiện "Đã thanh toán". Cuối ca: Đóng ca, nhập tiền đếm, xem chênh lệch.
7. Desktop, `dev.manager` → Báo cáo doanh thu (chọn kỳ, xuất CSV) và Nhân viên và phân quyền (thêm nhân viên, khóa, mở khóa).
8. (Tùy chọn) Khóa tài khoản: nhập sai mật khẩu 5 lần để thấy thông báo khóa (15 phút).

Mức chưa làm, nói thẳng khi demo: phục vụ tại bàn và chuyển bàn, hủy và duyệt hủy món, hoàn tiền, nhập và kiểm kê kho.

## 5. Sau buổi demo
Không commit mật khẩu hay tệp TestResults. Quay về bản sạch bằng cách tạo CSDL Dev mới (tên đúng CafeManager_Group6_Dev) rồi chạy lại mục 1.
