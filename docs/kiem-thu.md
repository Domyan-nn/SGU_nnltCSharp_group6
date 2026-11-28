# Kiểm thử

Cập nhật 21/11/2026, mã `main` ở commit `73a9e5b`. Tài liệu chỉ có hai bảng: **Bảng 1** các ca kiểm thử KT (giao diện Web, Desktop và đối chiếu SQL, đồng bộ với tài liệu 06) và **Bảng 2** kiểm thử tự động chạy bằng `dotnet test`. Ảnh chụp và truy vấn SQL của từng ca nằm ở [bien-ban-kiem-thu.md](bien-ban-kiem-thu.md) và [bang-chung/](bang-chung/). Chuẩn bị máy, tài khoản mẫu và dữ liệu: [huong-dan-demo.md](huong-dan-demo.md).

## Bảng 1. Kiểm thử KT (29 ca, tất cả đã chạy thử và Đạt)

| Mã | Chức năng | Các bước kiểm thử | Kết quả mong đợi | Kết quả |
|---|---|---|---|---|
| **A. Web khách hàng** | | | | |
| KT06 | Tìm và xem món trên Web | Mở trang chủ; tìm 'cà phê sữa'; mở chi tiết; tìm một tên không có; thu nhỏ về 360 px. | Đúng món, giá hiện hành, tùy chọn; tên không có thì báo 'Không tìm thấy món phù hợp'; không tràn ngang ở 360 px. | Đạt |
| KT07 | Đặt đơn trên Web | Đăng nhập dev.customer; thêm 2 Món A vào giỏ; Đặt hàng; chọn giờ nhận hợp lệ; xác nhận. | Nhận mã đơn và mã nhận hàng, trạng thái 'Chờ tiếp nhận'; chưa giữ kho, chưa có phiếu pha chế. | Đạt |
| KT10 | Giá thay đổi trước khi xác nhận | Khách mở trang đặt hàng chưa gửi; quản lý đổi giá món (hiệu lực ngay); khách bấm Gửi đơn. | Web báo giá cũ và mới, không tạo đơn; khách đồng ý thì đơn lưu theo giá mới. | Đạt |
| KT14 | Hủy đơn chờ | Mở đơn chờ; Hủy đơn; xác nhận; thử hủy đơn đã tiếp nhận. | Đơn chờ thành 'Đã hủy', không phiếu, không giữ kho; đơn đã tiếp nhận không hủy được. | Đạt |
| KT16 | Quyền sở hữu đơn | Khách A đặt đơn; khách B mở và thử hủy đơn của A. | B nhận 404; đơn của A không đổi; A vẫn xem được. | Đạt |
| KT17 | Đăng ký tài khoản khách | Đăng ký hợp lệ; đăng ký trùng tên; mật khẩu ngắn; xác nhận không khớp; số điện thoại sai. | Lần đầu thành công; các trường hợp sai bị từ chối, lỗi gần trường, giữ giá trị đã nhập (trừ mật khẩu); không có tài khoản dở. | Đạt |
| KT21 | Đăng nhập tài khoản khách | Đăng nhập đúng; đăng xuất; sai mật khẩu; sai 5 lần; thử mật khẩu đúng khi đang khóa; mở khóa bằng SQL. | Sai hoặc khóa không cấp phiên; khóa 15 phút; mật khẩu đúng khi khóa vẫn bị từ chối. | Đạt |
| KT23 | Chi tiết món trên Web (B1) | Mở /mon/<id>; thử số lượng 0, 51, abc và tùy chọn vượt mức; thêm vào giỏ; mở món không tồn tại; thu nhỏ về 360 px. | Nội dung khớp D06 và SQL; lỗi rõ ràng, giỏ không đổi; món không tồn tại trả 404; không tràn ngang ở 360 px. | Đạt |
| KT24 | Tiến độ pha chế và thanh toán trong 'Đơn của tôi' (B2) | Khách đặt 2 ly; thu ngân tiếp nhận; pha chế bắt đầu, làm xong, giao; thu ngân thu tiền; thêm đơn hủy và đơn bị từ chối. | Chữ hiển thị đúng ở từng bước; danh sách và chi tiết cùng số liệu; đơn hủy hoặc từ chối không có dòng tiến độ. | Đạt |
| **B. Desktop: thực đơn, danh mục, nguyên liệu** | | | | |
| KT04 | Quản lý món (D06) | Thêm món; thêm biến thể; đặt giá 30.000; công thức; Bật bán; đổi giá 35.000; tìm không dấu. | Trạng thái dòng: thiếu giá và công thức → thiếu công thức → sẵn sàng bật bán → Đang bán; mỗi lần đổi giá ghi phiên bản mới; ô Tìm hoạt động. | Đạt |
| KT05 | Dữ liệu món không hợp lệ | Thêm món để trống tên; đổi giá -1, abc, 35.00. | Hộp thoại không đóng, báo lỗi gần trường; không có món hoặc giá mới. | Đạt |
| KT18 | Khoảng hiệu lực giá | Đổi giá hiệu lực sau 30 phút; rồi đổi giá hiệu lực ngay. | Bước 1 được nhận; bước 2 bị từ chối kèm hướng dẫn chọn thời điểm muộn hơn. | Đạt |
| KT19 | Quản lý danh mục, nguyên liệu và xóa | Ngừng bán rồi bật bán lại một món; ở Danh mục: thêm, đổi tên, tìm, xóa danh mục còn món; ở Nguyên liệu: xóa nguyên liệu đang dùng trong công thức, thêm rồi xóa nguyên liệu mới. | Món không bị xóa, ngừng bán vẫn giữ lịch sử; danh mục còn món và nguyên liệu đang dùng không xóa được; mục mới xóa được. | Đạt |
| **C. Desktop: tác nghiệp tại quầy** | | | | |
| KT08 | Tiếp nhận đơn Web (D02) | Đăng nhập dev.cashier; mở D02; chọn đơn; Tiếp nhận; thử Từ chối với lý do trống rồi có lý do. | Đơn hiện đúng món, giá chốt, giờ nhận; sau tiếp nhận rời danh sách chờ và có đúng 1 phiếu; từ chối bắt buộc có lý do. | Đạt |
| KT25 | Thu ngân: mở ca, thu tiền mặt, đóng ca và đối soát (mới) | Mở màn Thu ngân; Mở ca (quầy DEMO-C01, đầu ca 500.000); chọn đơn cần thu; nhập khách đưa 100.000; Thu tiền mặt; Đóng ca nhập tiền đếm 528.000. | Hiện tiền thối; thu xong đơn rời danh sách; đóng ca báo đối soát đúng công thức và chênh lệch. | Đạt |
| KT26 | Pha chế (mới) | Mở màn Pha chế (dev.barista); với một ly chờ: Bắt đầu làm; Làm xong; Đã giao cho khách. | Ly đi qua đủ ba bước; hàng đợi cập nhật; Web 'Đơn của tôi' đổi theo (xem KT24). | Đạt |
| KT27 | Báo cáo doanh thu (mới, B4) | Mở Báo cáo doanh thu; chọn 7 ngày; Xem báo cáo; đối chiếu số với SQL. | Doanh thu thuần, đã thu, đã hoàn, số đơn khớp với khoản thu thật. | Đạt |
| **D. Tài khoản, phiên và phân quyền** | | | | |
| KT01 | Đăng nhập hợp lệ (Desktop) | Mở D00; nhập dev.manager và mật khẩu; Đăng nhập. | Vào màn hình chính với các nút đúng quyền quản lý; không có Tiếp nhận đơn, Thu ngân, Pha chế. | Đạt |
| KT02 | Đăng nhập sai (Desktop) | Nhập sai mật khẩu; trỏ Desktop vào địa chỉ máy chủ không chạy; thử tài khoản khách ở D00. | Sai: 'Sai tên tài khoản hoặc mật khẩu.' và xóa ô mật khẩu; mất kết nối: báo chưa xác định, không báo sai mật khẩu. | Đạt |
| KT03 | Chặn thao tác thiếu quyền | Đăng nhập thu ngân và pha chế; gọi API quản trị bằng phiên thu ngân; đếm Product trước và sau. | Menu chỉ có nút đúng quyền; API trả 403; dữ liệu không đổi; ghi thiếu token chống giả mạo bị từ chối. | Đạt |
| KT15 | Hết phiên (Desktop) | Dev.cashier mở D02; trong SQL vô hiệu hóa nhân viên; đợi tối đa 5 giây; khôi phục. | D02 tự đóng, báo phiên hết hạn, về đăng nhập; phiên cũ gọi API trả 401. | Đạt |
| KT28 | Nhân viên và phân quyền (A6, mới) | Mở màn Nhân viên và phân quyền; thêm nhân viên; đăng nhập bằng tài khoản mới; khóa; mở khóa; sửa vai trò; thêm mà không chọn vai trò. | Đăng nhập được ngay; khóa thì không đăng nhập; mở khóa lại được; đổi vai trò có hiệu lực ở lần đăng nhập kế; không chọn vai trò bị chặn; không tự khóa mình. | Đạt |
| **E. Liên thông Web, Desktop và SQL Server** | | | | |
| KT09 | Dữ liệu dùng chung giữa Web, Desktop và SQL | So giá Món A trên Web, D06 và SQL; so đơn cũ ở Web với SQL sau khi đổi giá. | Ba nơi khớp theo thời điểm chốt; đơn cũ vẫn hiện giá chốt cũ. | Đạt |
| LT01 | Kịch bản liên thông | Quản lý tạo món ở D06 → khách tìm thấy và đặt trên Web → thu ngân thấy đơn ở D02 và tiếp nhận → khách thấy 'Đang phục vụ' → SQL. | Dữ liệu chạy xuyên Web, Desktop, SQL Server khớp nhau. | Đạt |
| **F. Kiểm thử mức cơ sở dữ liệu và dịch vụ** | | | | |
| KT11 | Gửi lặp yêu cầu thu | Thu một đơn bằng một mã yêu cầu; gửi lại cùng mã và cùng nội dung; gửi lại cùng mã nhưng khác nội dung. | Gửi lại cùng nội dung trả kết quả đã lưu, không tạo khoản thu thứ hai; khác nội dung bị từ chối. | Đạt |
| KT12 | Cạnh tranh tồn kho | Hai đơn đồng thời cùng tiếp nhận khi chỉ đủ nguyên liệu cho một đơn. | Chỉ một đơn được tiếp nhận; đơn còn lại báo thiếu nguyên liệu; tồn không âm và không giữ vượt tồn. | Đạt |
| KT13 | Ghi thu vào ca đã đóng | Đóng ca rồi thử ghi khoản thu vào ca đó. | Khoản thu bị từ chối; ca đã đóng không nhận thêm giao dịch. | Đạt |
| KT20 | Quan hệ và ràng buộc | Tạo đơn không có khách lẫn phiên bàn; gắn đơn vị món vào dòng thuộc lượt khác. | Cơ sở dữ liệu từ chối cả hai trường hợp. | Đạt |
| KT22 | Đối soát tiền mặt của ca | Mở ca với tiền đầu ca, ghi các khoản thu tiền mặt, đóng ca với tiền đếm. | Tiền dự kiến = đầu ca + thu; chênh lệch = tiền đếm trừ tiền dự kiến. | Đạt |

## Bảng 2. Kiểm thử tự động qua `dotnet test`

Chạy bằng `dotnet test CafeManager.sln` với biến môi trường `CAFE_TEST_SQLSERVER`. Nếu không đặt biến này thì 72 ca cần SQL Server hiện **Skipped** (không phải Pass). Mỗi lần chạy, các ca SQL tạo cơ sở dữ liệu riêng `CafeManager_Group6_Test_<GUID>`, áp dụng toàn bộ migration rồi xóa đúng cơ sở dữ liệu đó, không chạm CSDL Dev. Lần chạy 21/11/2026: 182/182 Đạt trong 12 giây.

| Lớp kiểm thử | Số ca | Cần SQL Server | Nội dung kiểm | Kết quả |
|---|---|---|---|---|
| `ApiTests` | 7 | Có | API qua HTTP: 401 khi chưa đăng nhập, khóa tài khoản, quyền và token chống giả mạo, luồng đặt đơn và đổi giá, thu hồi phiên, đăng ký khách, quản lý món cần giá và công thức | Đạt |
| `DatabaseTests` | 35 | Có | CSDL SQL Server: migration khớp mô hình, khóa ngoại và ràng buộc, chống chồng khoảng giá, một phiên và một ca mở, tiếp nhận nguyên tử, tranh chấp tồn kho, thu lặp, hoàn tiền, hủy món, lịch sử chỉ thêm | Đạt |
| `DesktopFlowTests` | 4 | Có | Luồng Desktop D02 và D06 qua ApiClient thật: tiếp nhận, từ chối, thiếu tồn kho, dựng món bán được, hết phiên | Đạt |
| `ServiceTests` | 8 | Có | Lớp dịch vụ: đăng ký và đăng nhập, thực đơn, tạo đơn (giá do máy chủ tính, chống gửi trùng), đơn của khách, tiếp nhận có giữ kho | Đạt |
| `StaffA123ApiTests` | 5 | Có | API danh mục, nguyên liệu, ca và thu tiền mặt, hàng đợi pha chế, hai người cùng bắt đầu một ly | Đạt |
| `StaffApiClientA123Tests` | 3 | Có | Máy khách API cho danh mục và nguyên liệu, thu ngân, pha chế | Đạt |
| `StaffAccountTests (gồm 3 lớp nhỏ cùng tệp)` | 10 | Có | Quản lý nhân viên và phân quyền: tạo, khóa, mở khóa, đổi vai trò có hiệu lực ngay, không khóa quản trị viên cuối, seed lại giữ mật khẩu | Đạt |
| `ApiClientTests` | 15 | Không | Máy khách API: đăng nhập, token chống giả mạo, phân loại lỗi 400–409, mất kết nối, hết giờ | Đạt |
| `CashierPresentationTests` | 31 | Không | Logic hiển thị Thu ngân và Pha chế: nhập tiền, tiền thối, chênh lệch ca, thông báo | Đạt |
| `ReportTests` | 5 | Không | Báo cáo doanh thu: gom theo ngày giờ Việt Nam, trừ hoàn tiền, món bán chạy, thang biểu đồ, xuất CSV | Đạt |
| `StaffAccountPresentationTests` | 18 | Không | Kiểm tra đầu vào và thông báo của màn hình Nhân viên và phân quyền | Đạt |
| `StaffApiContractTests` | 4 | Không | Khóa đường dẫn hợp đồng API và chính sách quyền | Đạt |
| `StaffPresentationTests` | 37 | Không | Logic hiển thị D02 và D06: giá, số lượng, giờ, tìm không dấu, thông báo | Đạt |
| **Tổng** | **182** | 72 cần SQL Server, 110 không | | **182/182 Đạt, 0 Skipped** |
