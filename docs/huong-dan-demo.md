# Hướng dẫn chuẩn bị và chạy demo

Cập nhật 12/11/2026. Kịch bản liên thông: quản lý tạo món → khách đặt đơn trên Web → thu ngân tiếp nhận trên Desktop. Các bước trên Desktop (D06, D02) phụ thuộc phần của C; API đã sẵn sàng.

## 1. Chuẩn bị một lần (máy demo)
```powershell
dotnet --version                       # SDK 10.0.403
sqllocaldb info MSSQLLocalDB           # LocalDB phải có
dotnet build CafeManager.sln
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed --seed-accounts
```
- Lệnh cuối in **mật khẩu ngẫu nhiên của ba tài khoản mẫu một lần duy nhất** (dev.manager, dev.cashier, dev.customer): ghi lại ngay. Quên mật khẩu thì phải tạo lại CSDL Dev của riêng máy đó.
- Nếu Windows báo "Application Control policy has blocked this file" khi build hoặc chạy, máy đang bật Smart App Control; cần tắt (không bật lại được) hoặc dùng máy khác.
- Chạy kiểm thử trước buổi demo (cần biến môi trường để các ca SQL không bị Skipped):
```powershell
$env:CAFE_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test CafeManager.sln
```

## 2. Chạy máy chủ Web và API
```powershell
dotnet run --project src/CafeManager.Web --launch-profile http     # http://localhost:5055
```
- Giao diện khách: http://localhost:5055/ (thực đơn từ CSDL, giỏ hàng, đăng ký, đăng nhập, đặt đơn).
- Kiểm tra nhanh API: `curl http://localhost:5055/api/menu` trả danh sách món; `curl http://localhost:5055/api/orders/mine` trả 401.
- Chạy giao diện với dữ liệu giả, không cần CSDL: đặt `CAFE_FAKE_GATEWAY=1` trước khi chạy.

## 3. Chạy ứng dụng Desktop
```powershell
$env:CAFE_API_BASE_URL='http://localhost:5055'
dotnet run --project src/CafeManager.Desktop
```
Đăng nhập bằng dev.manager (quản lý món) hoặc dev.cashier (tiếp nhận đơn).

## 4. Kịch bản demo (5–7 phút)
1. Desktop, đăng nhập dev.manager: tạo món mới, đặt giá và công thức, bật bán (UC17).
2. Web: đăng ký tài khoản khách rồi đăng nhập (UC18, UC19); xem thực đơn, chọn món và tùy chọn, thêm vào giỏ (UC01).
3. Web: xác nhận đơn với giờ nhận 07:00–22:00 (giờ Việt Nam); thử đổi giá ở bước 1 để thấy cảnh báo "giá đã thay đổi" (UC02).
4. Desktop, đăng nhập dev.cashier: mở danh sách đơn chờ, tiếp nhận đơn (UC03); đơn chuyển sang Đang phục vụ.
5. Web: "Đơn của tôi" hiện trạng thái mới; thử hủy một đơn còn chờ để thấy đơn đã tiếp nhận không tự hủy được (UC04).
6. (Tùy chọn) Khóa tài khoản: nhập sai mật khẩu 5 lần để thấy thông báo khóa.

## 5. Sau buổi demo
Không commit mật khẩu hay tệp TestResults. Quay về bản sạch bằng cách tạo CSDL Dev mới (tên đúng CafeManager_Group6_Dev) rồi chạy lại mục 1.
