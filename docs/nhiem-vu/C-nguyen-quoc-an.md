# Nhiệm vụ của C – Nguyễn Quốc An: Desktop, kiểm thử hệ thống, báo cáo và demo

Hạn: thứ 7 14/11/2026 (đóng băng chức năng thứ 6 18:00). Xem phạm vi chung ở [ke-hoach-den-thu-7.md](../ke-hoach-den-thu-7.md); hợp đồng API nháp ở cuối file [A-ngo-nguyen.md](A-ngo-nguyen.md). Thiết kế màn hình: tài liệu 05 (D00, D02, D06) và mục 3.5 của báo cáo.

> **Cập nhật 14/11/2026:** màn hình D02 (tiếp nhận/từ chối đơn) và D06 (món, biến thể, giá, công thức, bật/ngừng bán) đã được A viết thay trên nhánh `feat/desktop-t6` (chưa gộp vào `main` khi ghi dòng này), kèm test tự động. Các ô trong danh sách dưới đây **chưa được đánh dấu** vì C chưa đọc lại và chưa chạy tay. Việc còn lại của C: đọc mã, chạy D02/D06, chạy tay KT01–KT05, KT08, KT09, KT15, KT18, KT19 và ghi biên bản theo [kiem-thu-tay.md](../kiem-thu-tay.md), viết các mục báo cáo của mình.


## Bạn sở hữu
Toàn bộ `src/CafeManager.Desktop/`. **Desktop không được tham chiếu Infrastructure, EF hay SqlClient và không chứa chuỗi kết nối SQL**: mọi dữ liệu đi qua API HTTPS của máy chủ (kiến trúc ở Chương 4). Không sửa `Core/`, `Infrastructure/`, `Web/`: cần gì thì nhắn A.

## Việc phải làm

### T3 – 10/11 (tối nay)
- [ ] (C tự xác nhận trên máy mình) Cài môi trường, chạy được `dotnet build`, `dotnet test`, `--migrate --seed` (nếu Windows chặn DLL "Application Control policy", báo ngay cả nhóm).
- [ ] Dự họp chốt hợp đồng API; đọc kỹ `auth/*`, `staff/orders`, `staff/menu`.
- [x] Dựng lớp `ApiClient` (đã có trên nhánh `feat/desktop`, nằm ở project mới `CafeManager.Client` để kiểm thử được; 14 test; C đọc lại và tiếp tục từ đây): `HttpClient` + `CookieContainer` **riêng cho từng phiên đăng nhập** (không dùng chung giữa các nhân viên), địa chỉ máy chủ lấy từ cấu hình, có thời hạn chờ và hủy; chuyển lỗi 400/401/403/404/409 thành kết quả rõ ràng cho giao diện (không trả HTML đăng nhập vào form). Khóa nút khi đang gửi để tránh bấm hai lần.
- [x] Form D00 (tên tài khoản, mật khẩu, nút đăng nhập, báo lỗi): đã có `LoginForm`, gọi `ApiClient.LoginAsync`; khi chưa có máy chủ hiện "chưa xác định" thay vì báo sai mật khẩu. Chờ API đăng nhập của A để đăng nhập thật.
- [x] Xong khi: `ApiClient` gọi được một API giả (đã kiểm thử bằng handler giả; chạy thử form D00 trên Windows với máy chủ không tồn tại).

### T4 – 11/11: D00 đăng nhập thật (UC19)
- [ ] Nối D00 với `POST /api/auth/login`; xử lý đúng: sai mật khẩu, tài khoản khóa, mất kết nối (hiện rõ "chưa xác định", không tự coi là thành công); mật khẩu không lưu trên máy, không ghi vào log.
- [ ] Màn hình chính chỉ hiện chức năng theo danh sách quyền trả về (`MENU_MANAGE` → quản lý món; `ORDER_RECEIVE` → tiếp nhận đơn); nút đăng xuất.
- [ ] Hết phiên (401) → quay về D00, không mất dữ liệu đang nhập.
- [ ] Xong khi: KT01 (đăng nhập hợp lệ), KT02 (đăng nhập sai), KT03 (chặn thiếu quyền), KT15 (hết phiên) làm được bằng tay.

### T5 – 12/11: D06 quản lý món (UC17)
- [ ] Danh sách món/biến thể (tìm theo tên), form thêm, sửa, đổi giá (giá + ngày hiệu lực), ngừng bán; thông báo lỗi gần trường (tên trống, giá âm: KT05; khoảng hiệu lực chồng: KT18).
- [ ] Công thức: chọn nguyên liệu và định lượng cho mỗi biến thể; hiển thị rõ món chưa đủ giá và công thức thì **chưa bán được**.
- [ ] Món đã có chứng từ không xóa mà ngừng bán (KT19).
- [ ] Chờ API UC17 của A (dự kiến chiều thứ 5): làm trước giao diện với dữ liệu giả theo hợp đồng.
- [ ] Xong khi: tạo một món mới đầy đủ giá và công thức từ Desktop, thấy món đó trên Web (KT04, KT09).

### T6 – 13/11: D02 tiếp nhận đơn Web (UC03) và kiểm thử tay
- [ ] D02: danh sách đơn đang chờ (mã đơn, giờ đặt, giờ nhận, trạng thái), chi tiết (món, biến thể, tùy chọn, giá chốt, liên hệ), nút **Tiếp nhận** và **Từ chối** (bắt buộc nhập lý do); sau thao tác hiện trạng thái mới; làm mới tự động mỗi 5 giây kèm nút tải lại.
- [ ] Xử lý 409: thiếu nguyên liệu (`INSUFFICIENT_STOCK`) hoặc khách vừa hủy → thông báo, tải lại, không ghi nửa chừng (KT08, KT12).
- [ ] **Chạy tay và ghi biên bản + ảnh chụp** cho KT01–KT05, KT08, KT09, KT15 theo mẫu ở tài liệu 06 mục 6.
- [ ] Mức 2 nếu còn thời gian: D04 thu tiền mặt (UC08) hoặc D03 pha chế (UC07), theo API A giao.
- [ ] **Đóng băng chức năng 18:00.**

### T7 – 14/11: kiểm thử liên thông, báo cáo, demo
- [ ] Chạy kịch bản liên thông từ đầu trên cơ sở dữ liệu sạch: quản lý tạo món (Desktop) → khách đặt (Web) → thu ngân tiếp nhận (Desktop) → kiểm tra dữ liệu trong SQL Server (KT09). Ghi biên bản và ảnh.
- [ ] Viết báo cáo: 5.1.5 (Desktop), 5.2.2 (kiểm thử mức hệ thống, điền bảng 5.4 phần Desktop và liên thông), 5.3 (kết quả đạt được và bình luận) và Chương 6 (kết luận, hạn chế, hướng phát triển). Số liệu lấy từ biên bản thật; **không ghi Đạt khi thiếu bằng chứng**; các yêu cầu phi chức năng chưa đo thì ghi chưa thực hiện.
- [ ] Làm slide demo (đã có `Kiến_Trúc_Hệ_Thống_Quản_Lý_Quán_Cà_Phê.pptx` để cập nhật), viết kịch bản demo 5–7 phút, tập cả nhóm.

## Quy tắc cần nhớ
- Cookie phiên và mật khẩu không lưu dạng văn bản trên máy; mỗi nhân viên một `HttpClientHandler` riêng.
- Form chỉ thu đầu vào và hiển thị; không tính tiền, kiểm tra quyền hay kiểm tra tồn trong mã sự kiện của Form (quyết định cuối cùng ở máy chủ).
- Giao diện: nhãn chữ cho trạng thái, thứ tự Tab, không phím tắt chung cho thao tác nguy hiểm.

## Cần nộp (bằng chứng)
Mã các màn hình D00, D06, D02, biên bản và ảnh chụp cho các ca KT ở trên và kịch bản liên thông, mục báo cáo 5.1.5, 5.2.2, 5.3, Chương 6, slide và kịch bản demo.
