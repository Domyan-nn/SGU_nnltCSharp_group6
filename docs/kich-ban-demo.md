# Kịch bản demo – Nhóm 6 (mẫu để điền)

Mẫu này chỉ có khung và gợi ý. Mọi ô có dấu `…` do người phụ trách tự điền bằng lời của mình. Phần *(Gợi ý: …)* viết nghiêng chỉ để nhắc nên viết gì, xóa đi khi điền xong.

## 1. Thông tin chung

| Mục | Nội dung |
|---|---|
| Thời lượng mục tiêu | 3–5 phút, tối đa 5 phút |
| Ngày quay, địa điểm | … |
| Người quay màn hình, người nói | Người quay: phân công trước buổi quay; Người nói: Ngô Nguyên |
| Phiên bản mã (`git rev-parse --short HEAD`) | … |
| Mục tiêu video | Cho thấy hệ thống chạy 1 cách liền mạch từ đầu đến cuối, bao gồm, phần khách hàng (Xem thực đơn, chọn/tuỳ chỉnh món, Quản lý giỏ hàng, theo dõi đơn hàng, quản lý tài khoản, và phần admin chia ra làm 3 phần (Quản lý thực đơn, Quản lý danh mục, Theo dõi đơn hàng), show ra được luồng hoạt động từ Admin -> Quản lý món -> Khách xem thực đơn -> Nhân viên tiếp nhận -> Hoàn tất đơn hàng |
| Ba thông điệp chính | 1) Khách đặt món trên Web, nhân viên xử lý trên Desktop 2) Web và Desktop làm việc thông qua API và cùng sử dụng dữ liệu của hệ thống 3) Hệ thống có kiểm tra nghiệp vụ và phân quyền, hạn chế các thao tác không hợp lệ.|
| Phần không làm (nói rõ ở cuối) | không triển khai UC05–06 (phục vụ tại bàn, chuyển bàn), UC10–11 (hủy và duyệt hủy món), UC12 (hoàn tiền), UC13 (nhập và kiểm kê kho) |

## 2. Chuẩn bị trước khi quay

Làm trước, đánh dấu khi xong.

- [ ] Kéo `main` mới nhất, build không lỗi, ghi mã phiên bản ở mục 1.
- [ ] CSDL Dev tạo mới (`--migrate --seed --seed-accounts`), đã ghi mật khẩu cả 4 tài khoản (`dev.manager`, `dev.cashier`, `dev.barista`, `dev.customer`). Mật khẩu chỉ hiện một lần.
- [ ] Web chạy ở `http://localhost:5055`; Desktop trỏ đúng địa chỉ đó.
- [ ] Đã **tập một lần cả luồng** theo bảng phân cảnh, đo thời gian từng cảnh.
- [ ] Món dùng để demo có giá, công thức và tồn kho nguyên liệu; giờ nhận hàng chọn trong 07:00–22:00 và không ở quá khứ.
- [ ] Sắp sẵn các cửa sổ theo thứ tự cảnh: Desktop quản lý, Web khách, Desktop thu ngân, Desktop pha chế, SQL (nếu có cảnh đối chiếu).
- [ ] Phóng to chữ và độ phân giải đủ đọc khi chiếu; tắt thông báo hệ thống; đóng cửa sổ không liên quan.
- [ ] Mật khẩu **không** hiện trên màn hình lúc đăng nhập (dùng ô ẩn, không mở file ghi chú trên màn hình quay).

## 3. Bảng phân cảnh

Tổng thời lượng các cảnh phải ≤ 5 phút. Điền cột "Thời lượng" thật sau khi tập.

| Cảnh | Thời lượng | Người nói | Màn hình, tài khoản | Thao tác | Kết quả người xem phải thấy | Lời thoại  | Yêu cầu (UC) |
|---|---|---|---|---|---|---|---|
| 0. Mở đầu | 15s | Ngô Nguyên | Slide hoặc màn hình tiêu đề | Giới thiệu tên đề tài, nhóm và mục tiêu demo | Tên đề tài, nhóm, phạm vi | "Nhóm 6 xin giới thiệu đề tài Hệ thống quản lý quán cà phê. Hệ thống gồm giao diện Web để khách hàng xem thực đơn và đặt món, cùng ứng dụng Desktop để nhân viên tiếp nhận và xử lý đơn. Sau đây nhóm sẽ trình bày một luồng nghiệp vụ từ lúc quản lý thiết lập món đến hoàn tất đơn hàng." | – |
| 1. Quản lý món | 40s | Ngô Nguyên | Desktop, `dev.manager`, Quản lý món (có thể thêm Danh mục, Nguyên liệu) | Chọn món hoặc tạo món demo; trình bày biến thể, giá và công thức; bật bán  | Món có giá và công thức, bật bán, món hiện trên Web sau vài giây | "Đầu tiên, quản lý đăng nhập ứng dụng Desktop để thiết lập thực đơn. Tại đây có thể quản lý món, biến thể, giá bán và công thức pha chế. Khi món được bật bán, dữ liệu thực đơn được cung cấp cho website để khách hàng có thể xem và đặt món" | 17 |
| 2. Khách đặt đơn | 40s | Ngô Nguyên | Web, `dev.customer` | Đăng nhập; chọn món và biến thể; chọn tùy chọn nếu món hỗ trợ; thêm vào giỏ; xác nhận thông tin liên hệ và giờ nhận | Chọn món và tùy chọn, giỏ, xác nhận có mã đơn, trạng thái chờ tiếp nhận | “Tiếp theo, khách hàng truy cập website, đăng nhập và chọn món trong thực đơn. Khách có thể chọn biến thể và các tùy chọn được hệ thống cho phép, sau đó kiểm tra giỏ hàng và xác nhận đơn với thông tin liên hệ cùng thời gian nhận. Khi đặt thành công, hệ thống ghi nhận đơn ở trạng thái chờ tiếp nhận.” |  UC01, UC02|
| 3. Giá đổi trước khi xác nhận *(tùy chọn, bỏ nếu thiếu giờ)* | 20s | Ngô Nguyên| Web + Desktop quản lý | Nếu đã chuẩn bị trước: thay đổi giá món trên Desktop sau khi khách thêm món vào giỏ nhưng trước khi xác nhận; quay lại Web và xác nhận | Cảnh báo giá cũ và giá mới, không tạo đơn khi chưa đồng ý | “Hệ thống không chỉ dựa vào giá đã hiển thị trước đó. Khi giá món thay đổi trước lúc xác nhận, máy chủ tính lại giá và thông báo cho khách biết mức giá cũ, mức giá mới. Khách cần xác nhận lại thì đơn mới được gửi.”| UC02 |
| 4. Thu ngân tiếp nhận | 30s | Ngô Nguyên| Desktop, `dev.cashier`, Tiếp nhận đơn | Mở danh sách đơn chờ; chọn đơn vừa tạo; xem chi tiết; nhấn tiếp nhận| Đơn Web xuất hiện, tiếp nhận xong đơn chuyển "Đang phục vụ" | “Đơn khách vừa đặt được chuyển đến màn hình tiếp nhận của nhân viên. Thu ngân kiểm tra thông tin và chi tiết đơn, sau đó tiếp nhận. Trạng thái đơn được cập nhật để các bộ phận tiếp theo có thể xử lý.”| UC03 |
| 5. Pha chế | 30s | Ngô Nguyên | Desktop, `dev.barista`, Pha chế; Web "Đơn của tôi" | Chọn ly/đơn chờ; bắt đầu pha chế; cập nhật trạng thái sẵn sàng và đã giao theo đúng luồng hỗ trợ| Ly đi qua các bước; Web hiện tiến độ pha chế | “Sau khi đơn được tiếp nhận, nhân viên pha chế xem danh sách món cần thực hiện. Nhân viên cập nhật tiến độ qua các bước của quy trình pha chế. Khách hàng có thể theo dõi trạng thái đơn trên website.”| Nghiệp vụ pha chế; UC04 phần theo dõi đơn |
| 6. Thu tiền | 35s | Ngô Nguyên| Desktop, `dev.cashier`, Thu ngân (đã mở ca) | Chọn đơn đủ điều kiện thanh toán; nhập số tiền khách đưa; xác nhận thu tiền| Nhập tiền khách đưa, hiện tiền thối, đơn hết trong danh sách cần thu; Web hiện "Đã thanh toán" | “Khi khách đến nhận món, thu ngân thực hiện thanh toán trên ứng dụng Desktop. Nhân viên nhập số tiền khách đưa, hệ thống tính tiền thối và ghi nhận khoản thu. Sau khi giao dịch thành công, trạng thái thanh toán được cập nhật.”| Nghiệp vụ thu tiền/ca |
| 7. Đóng ca | 25s | Ngô Nguyên| Desktop, Thu ngân | Thực hiện đóng ca; nhập số tiền mặt thực tế đếm được; xác nhận| Nhập tiền đếm, hệ thống báo chênh lệch (thừa, thiếu hoặc khớp) | “Cuối ca, nhân viên thực hiện đối soát bằng cách nhập số tiền mặt thực tế. Hệ thống so sánh số tiền cần có với số tiền đã đếm và hiển thị phần chênh lệch để nhân viên kiểm tra.”| Nghiệp vụ quản lý ca |
| 8. Dữ liệu và an toàn *(chọn một ý)* | 15s | Ngô Nguyên| SQL hoặc thao tác bị chặn | Chọn một minh chứng đã chạy ổn định: đối chiếu trạng thái đơn trên Web/Desktop, hoặc minh họa thao tác xóa dữ liệu đang được tham chiếu bị từ chối| Ví dụ: kết quả SQL khớp màn hình, hoặc xóa mục đang dùng bị chặn | “Các màn hình không xử lý dữ liệu độc lập mà giao tiếp với máy chủ thông qua API. Những quy tắc nghiệp vụ được kiểm tra ở phía máy chủ, giúp dữ liệu giữa các thành phần nhất quán và hạn chế thao tác không hợp lệ.”| _ |
| 9. Kết | 25s | Ngô Nguyên| Slide | Nêu ngắn gọn kết quả demo, giới hạn phạm vi và lời cảm ơn| Phần chưa làm, cách kiểm thử, phân công | “Qua demo, nhóm đã giới thiệu luồng quản lý thực đơn, khách đặt món trên Web, nhân viên tiếp nhận, pha chế và thanh toán trên Desktop. Trong phạm vi đề tài hiện tại, nhóm chưa triển khai phục vụ tại bàn và chuyển bàn, hủy/duyệt hủy món, hoàn tiền, nhập và kiểm kê kho. Nhóm xin cảm ơn thầy và các bạn đã theo dõi.”| – |


## 4. Cách viết lời thoại

- Mỗi bước nói theo ba phần: **làm gì → thấy gì → vì sao quan trọng**. Một đến hai câu mỗi bước.
- 5 phút nói tối đa khoảng 600–650 từ (mỗi phút khoảng 130 từ). Viết ra rồi đọc thử bấm giờ.
- Chỉ nói điều đã chạy được và nhìn thấy trên màn hình. Không nói "đạt", "hoàn chỉnh" cho phần chưa kiểm thử tay.
- Nhấn điểm khác biệt của đồ án (cùng một dữ liệu giữa Web, Desktop, CSDL; giá chốt; chặn thao tác sai), đừng liệt kê từng nút.
- Phần không làm nói thẳng, ngắn gọn, khớp báo cáo.

## 5. Nếu lỗi khi quay

| Tình huống | Cách xử lý (điền) |
|---|---|
| Một cảnh lỗi hoặc quên bước | Dừng quay, khôi phục dữ liệu demo nếu cần, quay lại riêng cảnh đó. Tối đa hai lần; ghi chú thời điểm để người dựng ghép đúng thứ tự |
| Desktop hoặc Web không kết nối | Kiểm tra Web có chạy tại http://localhost:5055 không; kiểm tra biến CAFE_API_BASE_URL của Desktop có trỏ đúng địa chỉ không; kiểm tra API và đăng nhập lại. Không tiếp tục thao tác nếu dữ liệu chưa đồng bộ. |
| Thiếu giờ, vượt 5 phút | Bỏ cảnh thay đổi giá; rút ngắn phần giới thiệu kiến trúc; giữ lại luồng chính từ khách đặt đơn đến thanh toán. |

## 6. Kiểm tra sau khi quay

- [ ] Độ dài ≤ 5 phút; xem đủ luồng từ đặt đơn đến đóng ca.
- [ ] Âm thanh rõ, chữ đọc được khi chiếu trên màn lớn.
- [ ] Không lộ mật khẩu, email cá nhân hay thông tin riêng.
- [ ] Tên file và nơi lưu, link nộp: …
- [ ] Cả ba người đã xem lại và đồng ý với nội dung nói.
