# Nhật ký thay đổi của dự án

Bảng này liệt kê các mốc thay đổi **có thật** của kho mã, lập ngày 19/11/2026 từ `git log` và danh sách pull request trên GitHub (giờ Việt Nam). Mỗi dòng ghi mã commit hoặc số PR để tra lại được; không có mốc nào được thêm cho đủ. Cột "Người commit" là tên tài khoản git đã commit, không phải lúc nào cũng là người được giao việc đó (xem cột Ghi chú).

Tên tài khoản git ứng với thành viên: **Domyan-nn** là Ngô Nguyên (A); **LeeNaimThwuan**, "Lê Nam Thuận", "Le Nam Thuan" là Lê Nam Thuận (B, cùng một người nhưng cấu hình git bằng ba tên hoặc email khác nhau); **qannee** là Nguyễn Quốc An (C).

| Ngày giờ | Commit / PR | Người commit | Thay đổi | Số test hoặc bằng chứng | Ghi chú |
|---|---|---|---|---|---|
| 08/11 13:13 | `a37d9b2` | Domyan-nn | Nền dữ liệu SQL Server: 40 bảng nghiệp vụ và 4 bảng Identity, ba migration đầu (InitialSchema, IntegrityGuards, HistoryProtection), `CafeDataCommands`, bộ kiểm thử SQL Server, khung Web và Desktop | [verification.md](verification.md) | Làm cục bộ, chưa đẩy lên GitHub |
| 10/11 15:36 | `93038c7` | Domyan-nn | Bổ sung các cột còn thiếu (migration SchemaGapColumns) và bảng AllowedAddon | 33 test đạt (ghi trong [ke-hoach-den-thu-7.md](ke-hoach-den-thu-7.md)) | |
| 10/11 16:33 | `681bc72`, `3727d03` | Domyan-nn | Kế hoạch ba người đến thứ Bảy và file nhiệm vụ từng người; hợp đồng API (DTO), lệnh tạo tài khoản mẫu, sửa một test chập chờn | | |
| 10/11 23:29 đến 23:39 | `aeda3c1`, `543cfc3`, `6e974c2`; PR #1, #2, #3 | Domyan-nn | Lịch sử nhánh `main` trên GitHub được viết lại; ba nhánh `feat/web`, `feat/desktop`, `feat/api` được merge lại từ `origin/main` (nội dung giữ nguyên, ghi trong thông điệp commit) rồi gộp bằng PR #1 (Web), #2 (Desktop), #3 (API) | | Lúc lập kế hoạch 10/11 kho chưa có trên GitHub ([ke-hoach-den-thu-7.md](ke-hoach-den-thu-7.md)), nên lần đẩy đầu tiên diễn ra trong tối 10/11 |
| 10/11 23:31 và 23:34 | `275d128`, `f4a7feb` | Domyan-nn | Giao diện khách và trang thực đơn mẫu (Web); `ApiClient`, form đăng nhập D00 và test máy khách (Desktop) | | Việc được giao cho B và C ở ngày T3, A viết thay (bảng đóng góp N5) |
| 11/11 07:02 | `e3c2ae9` | Lê Nam Thuận | W01: thực đơn có chọn món, tùy chọn, số lượng và giỏ hàng lưu trong session | | PR #4 |
| 12/11 09:14 đến 09:47 | `51d1ff4`, `964ce49`, `9bafa0e` | qannee | DTO cho CSRF; form đăng nhập phân biệt tài khoản bị khóa, lỗi xác thực và mất kết nối; hoàn thiện menu chính, đăng xuất | [an_nhat_ki_cong_viec.md](an_nhat_ki_cong_viec.md) (các dòng ghi "chưa kiểm tra") | |
| 12/11 09:46 | `980796f`; PR #5 (gộp 17:43) | Lê Nam Thuận | W00 đăng ký, đăng nhập khách và W02 gửi đơn, dùng cổng giả theo hợp đồng API | | |
| 12/11 23:30 | `847cda7`, `a5e9e7b` | Domyan-nn | API tài khoản, thực đơn, đơn hàng, nhân viên và quản lý món (UC01, 02, 03, 04, 17, 18, 19); hướng dẫn chạy demo | 64 test đạt (ghi trong [quy-uoc-du-an.md](quy-uoc-du-an.md)) | |
| 13/11 11:22 | `54ab08e` | Domyan-nn | Thêm [quy-uoc-du-an.md](quy-uoc-du-an.md) | | |
| 13/11 16:06 | `8575c2e`; PR #6 | Lê Nam Thuận | W03 "Đơn của tôi": danh sách phân trang, chi tiết, hủy có xác nhận, kiểm tra chủ đơn | | |
| 14/11 14:26 | `676fa8e`; PR #7 (gộp 19:37) | Domyan-nn | Desktop D02 (tiếp nhận hoặc từ chối đơn Web), D06 (món, biến thể, giá, công thức), logic máy khách, test và hướng dẫn kiểm thử tay | 105 test đạt; điều khiển tự động hai form trên máy chủ thật 22/22 ([verification.md](verification.md)) | Việc của C trong kế hoạch, A viết thay (N5) |
| 15/11 08:41 đến 09:07 | `6011399`, `7a8d253`, `7dadaad` | Domyan-nn | Sửa hướng dẫn kiểm thử tay và thêm script kiểm tra máy; mở rộng ô chọn ngày giờ ở hộp thoại Đổi giá, Công thức; ghi biên bản kiểm thử giao diện chạy tự động | 18 ca: 17 Đạt, 1 Không đạt (KT19), 99 ảnh ([bien-ban-kiem-thu.md](bien-ban-kiem-thu.md)) | Kiểm thử chạy tự động, chưa có người thao tác tay |
| 15/11 14:10 và 14:19 | `abbb594`; PR #8 | Domyan-nn | Cập nhật tài liệu theo kết quả 15/11 và chạy lại toàn bộ test | 105/105 test đạt | |
| 16/11 | PR #9 (mở, đóng 23:07, không gộp) | Domyan-nn | Nhánh thử chuẩn hóa `PreparationUnit` về 3NF | 105/105 test đạt trên nhánh thử | Không vào `main`; làm lại ở PR #14 |
| 16/11 22:59 và 23:07 | `84addf1`, `62388e4`; PR #10 | Domyan-nn | Hợp đồng API cho danh mục, nguyên liệu, ca, thu tiền và pha chế (DTO, `StaffApi`, quyền, tài khoản `dev.barista`); đưa `ICustomerGateway` vào DI, bỏ biến static | 109/109 test đạt (mô tả PR #10) | |
| 17/11 13:30 | `5116319`; PR #11 (gộp 19:23) | Domyan-nn | Máy chủ cho danh mục, nguyên liệu, ca, thu tiền mặt và pha chế; trường `progress` của đơn | 117/117 test đạt (mô tả PR #11) | |
| 18/11 20:29 đến 20:38 | `9d26373`, `84d7770`, `6f843f3`, `e02e301` | qannee | Desktop: màn hình Thu ngân (A4), Pha chế (C3), Danh mục và Nguyên liệu (C1, C2), test logic hiển thị | 148/148 test đạt (A kiểm lại 18/11) | Đẩy thẳng vào `main`, không qua PR; chưa chạy tay với đơn Web thật |
| 18/11 21:39 đến 21:54 | `2b4c8ff`; PR #12 | LeeNaimThwuan | Web: trang chi tiết món (B1); tiến độ pha chế và thanh toán trong "Đơn của tôi" (B2) | 148/148 test đạt | |
| 18/11 tối | (cài đặt GitHub, không có commit) | Domyan-nn | Bật bảo vệ nhánh `main`: bắt buộc qua pull request, cấm force-push và xóa nhánh | Dòng A15 trong bảng phân công | Từ đây mọi thay đổi vào `main` đi qua PR |
| 18/11 23:10 và 23:20 | `fd0a574`; PR #13 | Domyan-nn | Đưa phân tích 3NF và mô hình dữ liệu rút gọn lên kho, cập nhật số liệu theo mã | [phan-tich-3nf.md](phan-tich-3nf.md), [mo-hinh-du-lieu-rut-gon.md](mo-hinh-du-lieu-rut-gon.md) | |
| 18/11 23:35 và 23:40 | `8001428`; PR #14 | Domyan-nn | Chuẩn hóa `PreparationUnit` về 3NF: bỏ `TicketId`, `RecipeVersionId` khỏi `PreparationUnit`, chuyển công thức lên `OrderLine`, migration `PreparationUnit3NF` có `Down` | 149/149 test đạt | Mỗi máy cần chạy `--migrate` một lần |

## Ghi chú về cách đọc bảng

- Số test chỉ ghi ở những mốc có nơi ghi lại bằng chứng (tài liệu trong `docs/` hoặc mô tả PR). Test cần SQL Server chỉ tính là đạt khi chạy với biến `CAFE_TEST_SQLSERVER`.
- Các mốc ngày 08/11 và đầu ngày 10/11 là commit cục bộ trên máy A, đẩy lên GitHub trong tối 10/11.
- Các thay đổi chỉ có ở máy từng người và chưa lên GitHub không được ghi ở đây.
- Bảng không sửa lịch sử git và không đổi tác giả commit; việc ai thật sự viết phần nào được ghi riêng ở bảng đóng góp trong báo cáo (dòng N5 của bảng phân công).
