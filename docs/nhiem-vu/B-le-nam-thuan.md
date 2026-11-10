# Nhiệm vụ của B – Lê Nam Thuận: Web khách (ASP.NET Core MVC)

Hạn: thứ 7 14/11/2026 (đóng băng chức năng thứ 6 18:00). Xem phạm vi chung ở [ke-hoach-den-thu-7.md](../ke-hoach-den-thu-7.md); hợp đồng API nháp ở cuối file [A-ngo-nguyen.md](A-ngo-nguyen.md). Thiết kế màn hình: tài liệu 05 (W00–W03) và mục 3.5 của báo cáo.

## Bạn sở hữu
`Web/Controllers/Web/`, `Web/Views/`, `Web/wwwroot/` (và model dùng riêng cho View). Không sửa `Program.cs`, `Core/`, `Infrastructure/`: cần gì thì nhắn A. Các Controller MVC gọi dịch vụ trong cùng máy chủ (không cần tự gọi HTTP đến chính nó), nhưng **luôn theo hợp đồng của A** để hành vi giống API.

## Việc phải làm

### T3 – 10/11 (tối nay)
- [ ] Cài môi trường, chạy được `dotnet build`, `dotnet test`, `--migrate --seed` trên LocalDB máy mình (nếu Windows chặn DLL "Application Control policy", báo ngay cả nhóm).
- [ ] Dự họp chốt hợp đồng API; đọc kỹ `POST /api/orders` và `GET /api/menu`.
- [ ] Dựng layout chung: thanh điều hướng (Trang chủ | Danh mục | Đơn của tôi | Tài khoản), trang gốc thay trang chữ hiện tại; dùng dữ liệu giả theo hợp đồng để không phải chờ A.
- [ ] Xong khi: chạy Web thấy được layout và một danh sách món giả.

### T4 – 11/11: W01 thực đơn và giỏ (UC01)
- [ ] Trang thực đơn: danh mục, ô tìm theo tên, nút xóa bộ lọc; thẻ món có tên, giá, tình trạng; chọn biến thể (kích cỡ), tùy chọn thêm (số lượng **trên mỗi ly**, không vượt `maxPerUnit`) và số lượng nguyên dương.
- [ ] Giỏ hàng lưu trong session: danh sách món, tùy chọn, tạm tính (chỉ để hiển thị; máy chủ sẽ tính lại).
- [ ] Món ngừng bán: báo và yêu cầu chọn lại; danh sách rỗng có thông báo và đường về thực đơn.
- [ ] Responsive từ **360 px**, nhãn chữ cho trạng thái (không chỉ dùng màu), có thứ tự Tab.
- [ ] Nối với `GET /api/menu` thật khi A giao (cuối T4).
- [ ] Xong khi: chạy thật trên dữ liệu seed; KT06 (tìm và xem món) làm được bằng tay.

### T5 – 12/11: W00 tài khoản và W02 gửi đơn (UC18, UC19, UC02)
- [ ] W00: form đăng ký (tên đăng nhập, mật khẩu, xác nhận mật khẩu, họ tên, số điện thoại) và đăng nhập; báo lỗi gần trường, giữ giá trị đã nhập; không hiển thị mật khẩu; tài khoản khóa hoặc sai mật khẩu → không cấp phiên.
- [ ] Chặn truy cập "Đơn của tôi" và gửi đơn khi chưa đăng nhập (chuyển về W00).
- [ ] W02: xem lại đơn, nhập giờ nhận (không ở quá khứ, trong giờ phục vụ), tên và số điện thoại liên hệ, gửi. Mỗi lần mở form sinh **một `requestId` mới**, gửi lại do mất phản hồi thì dùng lại **cùng** `requestId` để không tạo đơn trùng.
- [ ] Xử lý 409 `PRICE_CHANGED`: hiện giá cũ/mới, chỉ gửi lại khi khách đồng ý (KT10). Thành công: hiện mã đơn và mã nhận đơn, trạng thái Chờ tiếp nhận.
- [ ] Timeout: hiện "Tra cứu kết quả", không khẳng định thất bại và không tự tạo mã mới.
- [ ] Xong khi: đặt được một đơn thật từ trình duyệt và thấy nó trong danh sách chờ ở máy chủ (KT07, KT17, KT21 làm tay được).

### T6 – 13/11: W03 đơn của tôi và kiểm thử tay (UC04)
- [ ] W03: danh sách đơn của tôi (phân trang), chi tiết đơn (món, giá chốt, giờ nhận, trạng thái), nút **Hủy** chỉ hiện khi Pending và có hộp xác nhận nêu rõ đơn nào.
- [ ] Kiểm tra quyền sở hữu: đổi mã đơn trên URL sang đơn người khác → không xem được (404), không hủy được.
- [ ] **Chạy tay và ghi biên bản + ảnh chụp** cho KT06, KT07, KT10, KT14, KT16, KT17, KT21 theo mẫu ở tài liệu 06 mục 6 (môi trường, dữ liệu, kết quả thực tế, trạng thái Đạt/Không đạt, ảnh).
- [ ] Viết báo cáo: 5.1.4 (phần Web MVC).
- [ ] **Đóng băng chức năng 18:00.**

### T7 – 14/11
- [ ] Sửa lỗi Web, chụp ảnh giao diện đặt vào báo cáo, điền bảng 5.4 phần Web (ca nào Đạt/Không đạt/Chưa kiểm thử và bằng chứng).
- [ ] Kiểm tra giao diện ở 360 px và 1280 px; đọc soát chính tả toàn báo cáo; tập demo.

### Mức 2 (chỉ khi Mức 1 xong trước thứ 5 tối)
Hiển thị tiến độ đơn (Đang phục vụ, Sẵn sàng) trên W03; làm mới danh sách mỗi 5 giây kèm nút tải lại.

## Quy tắc cần nhớ
- Không tin giá từ trình duyệt: giỏ chỉ là tạm tính, máy chủ tính lại.
- Không đặt dữ liệu cá nhân trong URL hoặc chuỗi truy vấn; mã hóa nội dung khi hiển thị.
- Không ghi "Đạt" khi chưa chạy: chỉ ghi theo bằng chứng thật.

## Cần nộp (bằng chứng)
Mã các màn hình W00–W03, biên bản và ảnh chụp cho các ca KT ở trên, mục báo cáo 5.1.4 (Web) và bảng 5.4 (phần Web).
