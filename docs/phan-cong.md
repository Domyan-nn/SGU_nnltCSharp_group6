# Phân công và lịch sử làm việc – Nhóm 6

Cập nhật 19/11/2026. File này có hai phần:

1. **Phân công chi tiết** (mục 1, 2): thành viên, phần mã mỗi người phụ trách, và bảng phân công đang dùng từ 16/11 đến 21/11.
2. **Lịch sử làm việc** (mục 3): từng commit do ai làm, vào ngày giờ nào, thêm, sửa hay xóa file nào, việc đó để làm gì và có ý nghĩa gì với đồ án.

## 1. Thành viên

| Mã | Họ tên | MSSV | Tài khoản GitHub | Phần mã phụ trách |
|---|---|---|---|---|
| A | Ngô Nguyên | 3124411191 | `Domyan-nn` | `Core`, `Infrastructure` (CSDL, migration), `Web/Controllers/Api`, `Web/Services`, `Client`, `tests`, `Web/Program.cs` |
| B | Lê Nam Thuận | 3124411296 | `LeeNaimThwuan`; trong git còn có hai tên khác là "Lê Nam Thuận" và "Le Nam Thuan" | `Web/Controllers/Web`, `Web/Views`, `Web/wwwroot`, các model riêng cho View |
| C | Nguyễn Quốc An | 3124411003 | `qannee` | `src/CafeManager.Desktop` |

Ai cần sửa file ngoài phần của mình thì nhắn người phụ trách phần đó trước.

## 2. Bảng phân công 16/11–21/11

Bảng dưới đây chép từ [Sheet phân công của nhóm](https://claude.ai/artifact/RzzzADWV419cgf63wV1ZcA), bản sửa 35, đọc ngày 19/11. Sheet được cập nhật hằng ngày: khi trạng thái một việc thay đổi, sửa trên Sheet trước rồi chép lại vào đây.

**Mốc chung:**
- Mức 1 xong trước 18:00 thứ Năm 19/11.
- Từ 19:00 đến 22:00 cùng ngày: chạy thử toàn luồng (đặt đơn Web → tiếp nhận → pha chế → thu tiền → đóng ca) và sửa lỗi. Đóng băng mã lúc 22:00.
- Sáng thứ Sáu 20/11: quay video demo.
- Nộp trước 23:59 thứ Bảy 21/11.

Mức 2 chỉ làm khi phần Mức 1 của mình đã xong.

**Không làm, ghi vào giới hạn đề tài:** UC05–06 (phục vụ tại bàn, chuyển bàn), UC10–11 (hủy và duyệt hủy món), UC12 (hoàn tiền), UC13 (nhập và kiểm kê kho).

**Tổng hợp:**

| Người làm | Số việc | Xong | Đang làm | Chưa làm | Bỏ |
|---|---|---|---|---|---|
| Cả nhóm (N1–N6, C6) | 7 | 0 | 1 | 6 | 0 |
| A – Ngô Nguyên | 19 | 10 | 1 | 7 | 1 |
| B – Lê Nam Thuận | 9 | 2 | 0 | 6 | 1 |
| C – Nguyễn Quốc An | 9 | 4 | 0 | 5 | 0 |
| **Tổng** | **44** | **16** | **2** | **24** | **2** |

Với các việc A4, B1, B2, C1, C2, C3, "Xong" nghĩa là mã đã vào `main`. Chưa ai chạy tay các màn hình này với một đơn Web thật; việc đó thuộc C6.

### 2.1. Cả nhóm

| Mã | Việc | Hạn | Trạng thái | Xong khi có |
|---|---|---|---|---|
| N1 | Họp 20 phút chốt kế hoạch; mỗi người tạo nhánh riêng | 16/11 | Đang làm | Biên bản họp ngắn trong `docs/` |
| N2 | Họp 15 phút lúc 21:00 mỗi tối: xong gì, vướng gì, mai làm gì | 17/11 | Chưa làm | Mỗi tối 3 dòng ghi chú trong nhật ký nhóm |
| N3 | Mỗi tối, mỗi người ghi nhật ký làm việc của mình (ngày, việc, file hoặc commit, kết quả) và commit bằng tài khoản mình. Các mốc trước 18/11 chỉ dựng lại từ bằng chứng có thật, không viết bù | 17/11 | Chưa làm | `docs/nhat-ky-<tên>.md` của từng người. Hiện mới có [an_nhat_ki_cong_viec.md](an_nhat_ki_cong_viec.md) của C, ghi đến 12/11 |
| N4 | Tối 19/11, trước khi quay: vấn đáp chéo theo luồng (A hỏi B, B hỏi C, C hỏi A). Mỗi người đặt breakpoint, chạy luồng của mình trên máy mình và giải thích từng bước | 19/11 | Chưa làm | Mỗi người tự giải thích được luồng của mình và nộp 1 trang tự viết "tôi hiểu phần này thế nào" |
| N5 | Bảng đóng góp từng thành viên ghi đúng sự thật, có cột "đã đọc hiểu, chạy, sửa" cho người nhận lại phần người khác làm thay. Không sửa lịch sử, không đổi tác giả commit | 21/11 | Chưa làm | Bảng trong báo cáo, cả 3 người đồng ý |
| N6 | Khai báo các công cụ đã dùng; mỗi người tự ghi phần mình | 20/11 | Chưa làm | Mục khai báo trong báo cáo và `docs/sources.md` |
| C6 | Tối 19/11, 19:00–22:00, do C điều phối: chạy tay toàn luồng và các ca KT01–KT05, KT19, đổi chéo người chạy; cập nhật Bảng 5.4 và thêm bảng 4 mức kiểm thử theo mô hình V | 19/11 | Chưa làm | Biên bản có ảnh, Bảng 5.4 đã cập nhật. Cần xong trước: A4, B2, C3, C10 |

### 2.2. A – Ngô Nguyên

| Mã | Việc | Hạn | Trạng thái | Xong khi có / hiện trạng |
|---|---|---|---|---|
| A0.1 | Hợp đồng API cho A1–A3: DTO trong `Core`, hàm gọi trong `StaffApi`, quyền thu ngân và pha chế, tài khoản mẫu | 16/11 | Xong | PR #10, gộp 23:07 16/11, 109/109 test |
| A0.2 | Bỏ biến static `CustomerGatewayProvider.Current`, đưa `ICustomerGateway` vào DI | 16/11 | Xong | PR #10 |
| A0.3 | Nhắn nhóm link Sheet, tên nhánh và cách chia file; nhắc B, C chạy `scripts/kiem-tra-may.ps1` và build được `main` | 16/11 | Chưa làm | B, C báo đã build được trên máy mình |
| A1 | API danh mục và nguyên liệu: xem, thêm, sửa, xóa khi chưa bị tham chiếu; kèm test | 17/11 | Xong | PR #11, gộp 19:23 17/11, 117/117 test |
| A2 | API ca và thu tiền: mở ca, thu tiền mặt, đóng ca và đối soát; kèm test | 17/11 | Xong | PR #11 |
| A3 | API pha chế: danh sách ly chờ, bắt đầu làm, sẵn sàng, đã giao; kèm test | 18/11 | Xong | PR #11 |
| A5 | Mỗi tối xem và gộp PR của B, C; chạy lại toàn bộ test trước khi gộp | 18/11 | Xong | Kiểm lại lúc 22:41 18/11: `main` ở `017b7b0` build 0 cảnh báo, 148/148 test đạt |
| A6 | (Mức 2) Quản lý nhân viên và phân quyền (UC16) | 18/11 | Chưa làm | Tạo được nhân viên mới và đăng nhập bằng tài khoản đó |
| A7 | README mới, bảng nhật ký thay đổi, nguồn và công cụ; sửa dòng "chưa đẩy lên GitHub" trong biên bản; mục 2.11 và Chương 6 về Docker | 19/11 | Xong trên Sheet; PR #15 chưa gộp | README; lịch sử làm việc (mục 3 của file này) |
| A8 | Đưa báo cáo lên Google Docs và chia sẻ cho thầy; từ đó mỗi người sửa phần mình bằng tài khoản mình | 18/11 | Đang làm | Đã tải lên lúc 23:25 18/11 và chia sẻ; còn chờ B, C sửa phần mình (B9, C9) |
| A9 | Chương 4: C4 mức 1–3, phân tích giao tiếp, chức năng mẫu UC02, SoC, DIP, Clean Architecture | 19/11 | Chưa làm | Các sơ đồ trong Chương 4 và 4–5 slide |
| A10 | Ghép báo cáo và slide, cập nhật mục lục, nộp lên Classroom | 21/11 | Chưa làm | Đã nộp file docx, slide và link repo |
| A11 | (Mức 2) CI bằng GitHub Actions, có container SQL Server | 19/11 | Chưa làm | PR có dấu kiểm xanh, không test nào bị bỏ qua |
| A12 | Đưa phân tích 3NF và mô hình rút gọn lên `main`, sửa số liệu theo mã ngày 18/11 | 18/11 | Xong | PR #13, gộp 23:20 18/11 |
| A13 | Thêm `.mailmap` để gộp 3 tên git của B; nhắc B đặt `user.email` đúng | 19/11 | Chưa làm | `git shortlog -sne` hiện đúng 3 người |
| A14 | Khung slide 15 phút (khoảng 20 slide); bỏ các chỗ "CHƯA TRIỂN KHAI" ở slide kiến trúc cũ | 19/11 | Chưa làm | Đủ khung, mỗi slide ghi tên người điền |
| A15 | Bật bảo vệ nhánh `main` | 18/11 | Xong | Bật tối 18/11: bắt buộc qua PR, cấm force-push và xóa nhánh, áp dụng cả với admin |
| A16 | Chốt lịch giữa môn Máy học và đồ án C# | 18/11 | Bỏ (A quyết định ngày 19/11) | — |
| A17 | Chuẩn hóa `PreparationUnit` về 3NF, làm mới từ `main` | 19/11 | Xong | PR #14, gộp 23:40 18/11, 149/149 test. B và C còn phải tự chạy `--migrate` một lần |

### 2.3. B – Lê Nam Thuận

| Mã | Việc | Hạn | Trạng thái | Xong khi có / hiện trạng |
|---|---|---|---|---|
| B1 | Web: trang chi tiết món | 18/11 | Xong | PR #12, gộp 21:54 18/11. Chưa có ca KT, chưa chạy tay (chờ C10, C6) |
| B2 | "Đơn của tôi" hiện trạng thái pha chế và đã thanh toán | 18/11 | Xong | PR #12. Chưa chạy thật cùng Desktop (chờ C6) |
| B3 | Kiểm tra giao diện Web ở 360 px và trên máy tính, sửa chỗ vỡ bố cục | 19/11 | Chưa làm | Ảnh chụp ở 2 kích cỡ |
| B4 | (Mức 2) Báo cáo doanh thu | 19/11 | Bỏ (theo đánh giá 18/11) | — |
| B5 | Báo cáo và slide quy trình: 1–2 sơ đồ chia làn; phân cấp 19 use case theo 3 mức | 20/11 | Chưa làm | 2 sơ đồ chia làn và 1 bảng |
| B6 | Đặc tả đầy đủ UC02, UC03, UC08 theo mẫu của thầy, kèm sơ đồ hoạt động | 20/11 | Chưa làm | 3 bảng đặc tả kèm sơ đồ |
| B7 | Bản đồ màn hình Web | 20/11 | Chưa làm | 1 sơ đồ, 1 slide |
| B8 | Khảo sát: ưu và nhược điểm của KiotViet, Sapo và một quán tham chiếu so với hệ thống của nhóm | 20/11 | Chưa làm | Bảng trong Chương 2, 1 slide |
| B9 | Viết lại mục 5.1.4 (Web) bằng lời mình trên Google Docs; đọc soát phần Web ở 5.3 và Chương 6 | 20/11 | Chưa làm | Version History của Google Docs có bản sửa của B |

### 2.4. C – Nguyễn Quốc An

| Mã | Việc | Hạn | Trạng thái | Xong khi có / hiện trạng |
|---|---|---|---|---|
| A4 | Desktop: màn hình Thu ngân (mã A4 nhưng giao cho C) | 18/11 | Xong | Commit `9d26373`. Chưa thu thật một đơn Web (chờ C6) |
| C1 | Desktop: form danh mục | 19/11 | Xong | Commit `6f843f3`. Chưa chạy tay; KT19 chưa chạy lại |
| C2 | Desktop: form nguyên liệu | 19/11 | Xong | Commit `6f843f3`. Chưa chạy tay |
| C3 | Desktop: màn hình Pha chế | 19/11 | Xong | Commit `84d7770`. Chưa chạy với đơn Web thật (chờ C6) |
| C5 | Rà lại ERD; bản đồ màn hình Desktop có cả các màn hình mới | 20/11 | Chưa làm | ERD và sơ đồ đọc rõ khi chiếu |
| C7 | Cập nhật phạm vi và giới hạn đề tài (1.12, 3.7, Chương 6) cho đúng phần đã làm | 21/11 | Chưa làm | Báo cáo khớp với sản phẩm |
| C8 | Kịch bản demo 3–5 phút; sáng 20/11 cả nhóm quay video | 20/11 | Chưa làm | Kịch bản và video |
| C9 | Tự commit qua PR ít nhất một sửa đổi thật trong D02 hoặc D06 trước 22:00 19/11; viết lại 5.1.5, 5.2.2 và Chương 6 bằng lời mình | 20/11 | Chưa làm | PR của C trong D02 hoặc D06; Version History có bản sửa của C |
| C10 | Viết ca kiểm thử cho 6 màn hình mới (B viết ca cho màn hình của C, C viết ca cho màn hình của B); sửa kỳ vọng của KT19 | 19/11 | Chưa làm | Các ca KT có trong tài liệu 06 trước 19:00 19/11 |

## 3. Lịch sử làm việc

**Nguồn:** `git log` của nhánh `main` đến commit `e0823d7` (23:40 18/11), thêm commit `bf25b22` của PR #15 (chưa gộp), và danh sách pull request trên GitHub. Giờ là giờ Việt Nam.

**Cách đọc:**
- "Người commit" là tài khoản đã tạo commit. Khi việc đó trong kế hoạch được giao cho người khác, mục đó ghi rõ.
- **Thêm**, **Sửa**, **Xóa** là các file mà commit đó thêm mới, thay đổi nội dung hoặc xóa đi.
- Đường dẫn trong `src` và `tests` được viết gọn: `Web/` là `src/CafeManager.Web/`, `Core/` là `src/CafeManager.Core/`, `Infrastructure/` là `src/CafeManager.Infrastructure/`, `Client/` là `src/CafeManager.Client/`, `Desktop/` là `src/CafeManager.Desktop/`, `tests/` là `tests/CafeManager.Tests/`.
- Commit gộp pull request không tự đổi file nào, nên chỉ được ghi ở dòng "Gộp" cuối mỗi ngày.

**Tổng hợp theo người:**

| | A – Ngô Nguyên | B – Lê Nam Thuận | C – Nguyễn Quốc An |
|---|---|---|---|
| Commit trên `main` (không tính commit gộp) | 22, trong đó 3 commit không đổi file | 4 | 7 |
| Ngày có commit | 08, 10, 12, 13, 14, 15, 16, 17, 18/11 và 19/11 (PR #15) | 11, 12, 13, 18/11 | 12, 18/11 |
| Pull request đã mở | #1, #2, #3, #7, #8, #9 (đóng, không gộp), #10, #11, #13, #14, #15 (đang mở) | #4, #5, #6, #12 | Không có. Cả 7 commit được đẩy thẳng vào `main`, trước khi bật bảo vệ nhánh |

### Chủ nhật 08/11

**13:13 · `a37d9b2` · A (Domyan-nn) · Dựng nền cơ sở dữ liệu và khung dự án**
- Làm gì: tạo solution .NET 10 gồm `Core`, `Infrastructure`, `Web`, `Desktop` và project test. Dựng mô hình EF Core trên SQL Server: 39 bảng nghiệp vụ và các bảng Identity. Viết ba migration đầu: `InitialSchema` (tạo bảng), `IntegrityGuards` (trigger giữ toàn vẹn dữ liệu), `HistoryProtection` (chặn sửa dữ liệu lịch sử). Viết `CafeDataCommands`, các thao tác chạy trong transaction như tiếp nhận lượt gọi món, bắt đầu pha một ly, thu tiền, hoàn tiền, đối soát ca. Viết lệnh tạo dữ liệu mẫu và test chạy trên SQL Server thật. Web và Desktop lúc này chỉ là khung chạy được.
- Ý nghĩa: mọi phần làm sau (API, Web, Desktop) đều đứng trên CSDL và các thao tác này. Đây cũng là chỗ đáp ứng yêu cầu Web và Desktop dùng chung một CSDL SQL Server.
- Thêm (43 file):
  - Gốc repo: `.gitignore`, `CafeManager.sln`, `Directory.Build.props`, `README.md`, `dotnet-tools.json`, `global.json`, `database/inspect.sql`.
  - `docs/`: `appsettings.Example.json`, `business-rules.md`, `relation-mapping.md`, `schema.md`, `sources.md`, `verification.md`.
  - `Core/`: `CafeManager.Core.csproj`, `DataContracts.cs`, `Entities.cs`.
  - `Infrastructure/`: `CafeManager.Infrastructure.csproj`, `CafeDataCommands.cs`, `CafeDbContext.cs`, `CafeDbContextFactory.cs`, `DevelopmentSeeder.cs`, `StaffPermissionReader.cs`.
  - `Infrastructure/Migrations/`: `InitialSchema`, `IntegrityGuards`, `HistoryProtection` (mỗi migration một file `.cs` và một file `.Designer.cs`), `CafeDbContextModelSnapshot.cs`, `HistorySql.cs`, `IntegritySql.cs`.
  - `Web/`: `CafeManager.Web.csproj`, `Program.cs`, `Properties/launchSettings.json`, `appsettings.json`.
  - `Desktop/`: `CafeManager.Desktop.csproj`, `Form1.cs`, `Form1.Designer.cs`, `Form1.resx`, `Program.cs`.
  - `tests/`: `CafeManager.Tests.csproj`, `DatabaseTests.cs`, `SqlServerFixture.cs`.

Commit này làm trên máy A, đến tối 10/11 mới được đẩy lên GitHub.

### Thứ Ba 10/11

**15:36 · `93038c7` · A · Bổ sung cột còn thiếu và bảng `AllowedAddon`**
- Làm gì: migration `SchemaGapColumns` thêm các cột còn thiếu. Thêm bảng `AllowedAddon`, quy định món nào được thêm tùy chọn nào và tối đa bao nhiêu phần mỗi ly. Cập nhật entity, `DbContext`, test và tài liệu. Thông điệp commit bị trùng với commit 08/11.
- Ý nghĩa: CSDL khớp với tài liệu thiết kế dữ liệu, đủ 40 bảng nghiệp vụ. Web dựa vào `AllowedAddon` để giới hạn tùy chọn thêm khi khách chọn món. Lúc này có 33 test đạt.
- Thêm: `Infrastructure/Migrations/20261110082113_SchemaGapColumns.cs` (kèm `.Designer.cs`), `Infrastructure/Migrations/AllowedAddonSql.cs`.
- Sửa: `README.md`, `docs/business-rules.md`, `docs/relation-mapping.md`, `docs/schema.md`, `docs/sources.md`, `docs/verification.md`, `Core/Entities.cs`, `Infrastructure/CafeDbContext.cs`, `Infrastructure/Migrations/CafeDbContextModelSnapshot.cs`, `tests/DatabaseTests.cs`.

**16:33 · `681bc72` · A · Kế hoạch ba người đến 14/11**
- Làm gì: lập kế hoạch đầu tiên của nhóm. Phạm vi chia 3 mức. Mỗi người có danh sách việc theo ngày: A làm máy chủ và API, B làm Web, C làm Desktop, kiểm thử hệ thống và báo cáo. Kèm bản nháp hợp đồng API.
- Ý nghĩa: lần chia việc đầu tiên, có chia thư mục để ba người làm song song không xung đột. Kế hoạch này hết hạn ngày 14/11 và được thay bằng bảng phân công ở mục 2. Các file của nó đã xóa ngày 19/11 (xem cuối mục này).
- Thêm: `docs/ke-hoach-den-thu-7.md`, `docs/nhiem-vu/A-ngo-nguyen.md`, `docs/nhiem-vu/B-le-nam-thuan.md`, `docs/nhiem-vu/C-nguyen-quoc-an.md`.

**16:33 · `3727d03` · A · Hợp đồng API dùng chung và tài khoản mẫu**
- Làm gì: thêm `Core/ApiContracts.cs`, gồm DTO, mã quyền và mã lỗi dùng chung cho Web và Desktop. Thêm lệnh `--seed-accounts`: tạo vai trò Quản lý, Thu ngân, quyền `MENU_MANAGE`, `ORDER_RECEIVE` và tài khoản mẫu, mật khẩu ngẫu nhiên chỉ in ra một lần. Sửa một test hay hỏng: test ghi giá chồng khoảng thời gian khi hai yêu cầu chạy cùng lúc.
- Ý nghĩa: có hợp đồng thì B và C dựng giao diện trước với dữ liệu giả, không phải chờ API. Có tài khoản mẫu để đăng nhập thử mà không phải commit mật khẩu. 34 test đạt.
- Thêm: `Core/ApiContracts.cs`, `Infrastructure/DevelopmentAccountSeeder.cs`.
- Sửa: `README.md`, `docs/sources.md`, `docs/verification.md`, `Web/Program.cs`, `tests/DatabaseTests.cs`.

**23:29 · `aeda3c1`, `543cfc3`, `6e974c2` · A · Nối lại ba nhánh sau khi đẩy lên GitHub**
- Làm gì: lần đầu đẩy lên GitHub thì lịch sử nhánh `main` bị viết lại. Ba nhánh `feat/api`, `feat/web`, `feat/desktop` được nối lại với `origin/main` bằng ba commit không đổi file nào.
- Ý nghĩa: từ đây repo có trên GitHub, mỗi người làm trên nhánh riêng.
- Không thêm, sửa hay xóa file nào.

**23:31 · `275d128` · A · Web: layout chung và trang thực đơn dùng dữ liệu mẫu**
- Việc này trong kế hoạch giao cho B (ngày T3); người commit là A.
- Làm gì: dựng layout chung có thanh điều hướng (Trang chủ, Đơn của tôi, Tài khoản), CSS cho màn hình 360 px và chế độ tối. Trang thực đơn có tìm theo tên, lọc theo danh mục và báo khi không có món. Dữ liệu mẫu `SampleMenu` theo đúng hợp đồng API.
- Ý nghĩa: B có sẵn khung Web để làm tiếp W01 sáng 11/11.
- Thêm: `Web/Controllers/Web/HomeController.cs`, `Web/Models/SampleMenu.cs`, `Web/Views/Home/Account.cshtml`, `Web/Views/Home/Index.cshtml`, `Web/Views/Home/Orders.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Web/Views/_ViewImports.cshtml`, `Web/Views/_ViewStart.cshtml`, `Web/wwwroot/css/site.css`.
- Sửa: `Web/Program.cs`, `docs/nhiem-vu/B-le-nam-thuan.md`.

**23:34 · `f4a7feb` · A · Desktop: `ApiClient` và form đăng nhập D00**
- Việc này trong kế hoạch giao cho C (ngày T3); người commit là A.
- Làm gì:
  - Thêm project `CafeManager.Client`, chỉ phụ thuộc `Core`. Trong đó có `ApiClient`: mỗi phiên đăng nhập một kho cookie riêng, đổi lỗi 400–409 thành kết quả rõ ràng cho giao diện, mất mạng hay hết thời gian chờ thì báo "chưa xác định", nhận 401 thì báo hết phiên.
  - Form `LoginForm` (D00) gọi API đăng nhập; hết phiên thì quay lại màn hình đăng nhập.
  - 14 test cho `ApiClient`, dùng máy chủ giả.
- Ý nghĩa: Desktop lấy dữ liệu qua API của máy chủ chứ không nối thẳng vào CSDL, đúng kiến trúc ở Chương 4. Tách `Client` ra khỏi Windows Forms để kiểm thử tự động được.
- Thêm: `Client/ApiClient.cs`, `Client/CafeManager.Client.csproj`, `Desktop/LoginForm.cs`, `tests/ApiClientTests.cs`.
- Sửa: `CafeManager.sln`, `README.md`, `docs/nhiem-vu/C-nguyen-quoc-an.md`, `Desktop/CafeManager.Desktop.csproj`, `Desktop/Form1.cs`, `Desktop/Program.cs`, `tests/CafeManager.Tests.csproj`.

Gộp: PR #1 (Web), #2 (Desktop), #3 (API), A gộp lúc 23:38–23:39.

### Thứ Tư 11/11

**07:02 · `e3c2ae9` · B (Lê Nam Thuận) · Web W01: thực đơn và giỏ hàng**
- Làm gì:
  - Mỗi thẻ món có nhãn "Đang bán", mỗi kích cỡ một biểu mẫu: số lượng nguyên dương, tùy chọn thêm tính theo từng ly và không vượt mức tối đa.
  - `CartController` thêm, sửa, xóa món trong giỏ, có chống giả mạo yêu cầu và kiểm tra lại mọi đầu vào ở máy chủ.
  - Giỏ hàng lưu trong session, chỉ giữ mã món và số lượng; giá được tính lại mỗi lần hiển thị.
  - Trang giỏ có tạm tính. Món đã ngừng bán được đánh dấu và không tính tiền; giỏ rỗng có đường quay về thực đơn.
  - Viết `docs/patch-session-for-A.md` nhờ A bật session trong `Program.cs`, vì file đó thuộc phần của A.
- Ý nghĩa: xong UC01 (xem, tìm món, giỏ hàng). Giỏ không lưu giá nên khách không sửa được giá gửi lên. B giữ đúng quy ước chia thư mục.
- Thêm: `docs/patch-session-for-A.md`, `Web/Controllers/Web/CartController.cs`, `Web/Models/Cart.cs`, `Web/Models/MenuSource.cs`, `Web/Models/Money.cs`, `Web/Views/Cart/Index.cshtml`.
- Sửa: `Web/Controllers/Web/HomeController.cs`, `Web/Views/Home/Index.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Web/Views/_ViewImports.cshtml`, `Web/wwwroot/css/site.css`.

### Thứ Năm 12/11

**09:14 · `51d1ff4` · C (qannee) · DTO cho mã chống giả mạo và dữ liệu D06**
- Làm gì: thêm vào `Core/ApiContracts.cs` kiểu dữ liệu nhận mã chống giả mạo (`AntiforgeryTokenResponse`) và các kiểu cho màn hình quản lý món D06: món, biến thể, giá hiện hành, công thức, danh mục, nguyên liệu. Bắt đầu ghi nhật ký cá nhân.
- Ý nghĩa: Desktop có sẵn kiểu dữ liệu để nhận mã chống giả mạo và dữ liệu màn hình D06. `Core` là thư mục của A, C sửa trực tiếp.
- Thêm: `docs/an_nhat_ki_cong_viec.md`.
- Sửa: `Core/ApiContracts.cs`.

**09:37 · `964ce49` · C · Đăng nhập Desktop báo đúng lý do lỗi**
- Làm gì: `LoginForm` phân biệt tài khoản bị khóa, sai tên hoặc mật khẩu, và mất kết nối. Chỉ xóa ô mật khẩu khi biết chắc là lỗi đăng nhập.
- Ý nghĩa: nhân viên biết vì sao không vào được. Khi mất mạng, ứng dụng không báo nhầm là sai mật khẩu.
- Sửa: `Desktop/LoginForm.cs`, `docs/an_nhat_ki_cong_viec.md`.

**09:46 · `980796f` · B · Web W00 (tài khoản khách) và W02 (gửi đơn)**
- Làm gì:
  - W00: đăng ký (có xác nhận mật khẩu, họ tên, số điện thoại) và đăng nhập. Lỗi hiện ngay cạnh ô nhập, giữ lại giá trị đã nhập; sai mật khẩu hoặc tài khoản bị khóa thì không cấp phiên.
  - Chặn "Đơn của tôi" và đặt hàng khi chưa đăng nhập.
  - W02: xem lại đơn, chọn giờ nhận (không ở quá khứ, trong giờ phục vụ), nhập liên hệ, gửi đơn.
  - Mỗi lần mở biểu mẫu sinh một `requestId` mới; gửi lại dùng đúng mã cũ.
  - Xử lý giá đổi (409 `PRICE_CHANGED`): hiện giá cũ và giá mới, chỉ gửi lại khi khách đồng ý. Mất phản hồi thì hiện "Tra cứu kết quả" chứ không tạo mã mới.
  - `ICustomerGateway` là điểm nối duy nhất tới API của A; lúc này chạy bằng bản giả trong bộ nhớ.
- Ý nghĩa: xong phần giao diện của UC18 (đăng ký), UC19 (đăng nhập khách) và UC02 (gửi đơn). Gửi lại không tạo đơn trùng, và khách luôn thấy giá trước khi chốt.
- Thêm:
  - `Web/Controllers/Web/AccountController.cs`, `Web/Controllers/Web/CheckoutController.cs`.
  - `Web/Models/AccountForms.cs`, `Web/Models/CustomerGateway.cs`, `Web/Models/CustomerSession.cs`, `Web/Models/OrderModels.cs`.
  - `Web/Views/Account/Index.cshtml`, `Login.cshtml`, `Register.cshtml`.
  - `Web/Views/Checkout/Index.cshtml`, `PriceChanged.cshtml`, `Result.cshtml`, `Timeout.cshtml`.
  - `Web/Views/Shared/_CheckoutHidden.cshtml`, `_FormErrors.cshtml`, `_OrderSummary.cshtml`.
- Sửa: `Web/Controllers/Web/HomeController.cs`, `Web/Models/Cart.cs`, `Web/Views/Cart/Index.cshtml`, `Web/Views/Home/Orders.cshtml`, `Web/wwwroot/css/site.css`.
- Xóa: `Web/Views/Home/Account.cshtml` (trang giữ chỗ cũ, thay bằng `Views/Account`).

**09:47 · `9bafa0e` · C · Màn hình chính Desktop theo quyền**
- Làm gì: màn hình chính chỉ hiện nút cho chức năng mà tài khoản có quyền (Quản lý món, Tiếp nhận đơn), báo rõ khi chưa được cấp quyền nào. Thêm nút Đăng xuất. Hết phiên thì quay lại màn hình đăng nhập.
- Ý nghĩa: đáp ứng yêu cầu phân quyền của Desktop: mỗi nhân viên chỉ thấy việc của mình.
- Sửa: `Desktop/Form1.cs`, `Desktop/Program.cs`, `docs/an_nhat_ki_cong_viec.md`.

**23:30 · `847cda7` · A · API và dịch vụ máy chủ**
- Làm gì:
  - Các dịch vụ trong `Infrastructure/Services`:
    - Tài khoản: đăng nhập, khóa tài khoản khi sai nhiều lần, đăng ký trong một transaction.
    - Đọc thực đơn.
    - Đơn của khách: máy chủ tự tính giá, lưu ảnh chụp tên và giá, chống gửi trùng bằng `RequestId`, xem và hủy đơn.
    - Tiếp nhận và từ chối đơn của nhân viên.
    - Quản lý món (UC17).
  - Bên Web: đăng nhập bằng cookie; trả JSON 401 và 403; kiểm tra quyền trong CSDL ở mỗi yêu cầu; chống giả mạo cho yêu cầu ghi; bật session cho giỏ hàng của B.
  - Các controller `api/auth`, `api/menu`, `api/orders`, `api/staff`.
  - `RealCustomerGateway` và `MenuSource` đọc CSDL thật thay cho bản giả. Sửa `MenuSource` và `HomeController` là sửa vào phần của B.
  - Thêm 15 test, tổng 64/64 đạt.
- Ý nghĩa: Web và Desktop có máy chủ thật. Từ đây luồng liên thông chạy được: khách đặt đơn trên Web, nhân viên tiếp nhận ở Desktop, dữ liệu nằm chung trong SQL Server.
- Thêm (16 file):
  - `Infrastructure/Services/AccountService.cs`, `CustomerOrderService.cs`, `IdentitySetup.cs`, `MenuQueryService.cs`, `StaffMenuService.cs`, `StaffOrderService.cs`.
  - `Web/Controllers/Api/AuthController.cs`, `MenuController.cs`, `OrdersController.cs`, `StaffController.cs`.
  - `Web/Services/ApiExceptionFilter.cs`, `AuthSetup.cs`, `RealCustomerGateway.cs`.
  - `tests/ApiTests.cs`, `ServiceTests.cs`, `TestData.cs`.
- Sửa (14 file):
  - `README.md`, `docs/business-rules.md`, `docs/nhiem-vu/A-ngo-nguyen.md`, `docs/verification.md`.
  - `Client/ApiClient.cs`, `Core/ApiContracts.cs`, `Infrastructure/DevelopmentSeeder.cs`, `Infrastructure/StaffPermissionReader.cs`.
  - `Web/Controllers/Web/HomeController.cs`, `Web/Models/MenuSource.cs`, `Web/Program.cs`.
  - `tests/ApiClientTests.cs`, `tests/CafeManager.Tests.csproj`, `tests/SqlServerFixture.cs`.

**23:33 · `a5e9e7b` · A · Hướng dẫn chạy demo**
- Làm gì: viết các bước chuẩn bị máy (tạo CSDL, dữ liệu mẫu, tài khoản mẫu, chạy test), cách chạy Web và Desktop, và kịch bản demo ngắn cho luồng Web sang Desktop.
- Ý nghĩa: người khác trong nhóm, hay thầy, dựng lại được bản demo trên máy mới.
- Thêm: `docs/huong-dan-demo.md`.

Gộp: PR #4 (W01 của B), A gộp lúc 10:31. PR #5 (W00, W02), B tự gộp lúc 17:43. Ba commit buổi sáng của C được đẩy thẳng vào `main`, không qua PR.

### Thứ Sáu 13/11

**11:22 · `54ab08e` · A · Quy ước dự án**
- Làm gì: thêm `docs/quy-uoc-du-an.md`, ghi quy ước chung và hiện trạng dự án. Ví dụ: không dùng `EnsureCreated`, không xóa CSDL, đổi mô hình phải bằng migration mới, không ghi "Đạt" khi test bị bỏ qua. Các file kế hoạch được sửa để trỏ tới file này.
- Ý nghĩa: các quy tắc có một chỗ chung để cả nhóm theo.
- Thêm: `docs/quy-uoc-du-an.md`.
- Sửa: `docs/ke-hoach-den-thu-7.md`, `docs/nhiem-vu/A-ngo-nguyen.md`.

**16:06 · `8575c2e` · B (Le Nam Thuan) · Web W03: đơn của tôi**
- Làm gì:
  - Trang danh sách đơn của khách, 10 đơn mỗi trang, trạng thái ghi bằng chữ.
  - Trang chi tiết đơn: món, giá đã chốt, giờ đặt và giờ nhận theo giờ Việt Nam, mã nhận đơn, lý do nếu bị từ chối.
  - Nút Hủy chỉ hiện khi đơn còn chờ tiếp nhận, và phải qua một trang xác nhận ghi rõ mã đơn.
  - Mở đơn của người khác hoặc đơn không tồn tại thì trả 404, cả khi xem lẫn khi hủy.
  - Thêm 3 phương thức gọi dịch vụ đơn hàng của A vào `RealCustomerGateway`.
- Ý nghĩa: xong UC04 (xem và hủy đơn của mình), có kiểm tra quyền sở hữu đơn.
- Thêm: `Web/Controllers/Web/MyOrdersController.cs`, `Web/Views/MyOrders/ConfirmCancel.cshtml`, `Detail.cshtml`, `Index.cshtml`, `OrderNotFound.cshtml`.
- Sửa: `Web/Controllers/Web/HomeController.cs`, `Web/Models/CustomerGateway.cs`, `Web/Models/OrderModels.cs`, `Web/Services/RealCustomerGateway.cs`, `Web/wwwroot/css/site.css`.
- Xóa: `Web/Views/Home/Orders.cshtml` (trang giữ chỗ cũ, thay bằng `Views/MyOrders`).

Gộp: PR #6, B tự gộp lúc 16:07.

### Thứ Bảy 14/11

**14:26 · `676fa8e` · A · Desktop D02 (tiếp nhận đơn) và D06 (quản lý món)**
- Việc này trong kế hoạch giao cho C (ngày T5, T6); người commit là A.
- Làm gì:
  - D02: danh sách đơn Web đang chờ, tiếp nhận, từ chối có lý do, tự làm mới.
  - D06: món, biến thể, giá và công thức theo thời điểm hiệu lực, bật và ngừng bán, tìm không phân biệt dấu.
  - `StaffApi` gọi API nhân viên. `StaffPresentation` lo thông báo, kiểm tra đầu vào và định dạng hiển thị, tách khỏi Windows Forms để kiểm thử được.
  - 41 test mới, tổng 105/105 đạt.
  - Hướng dẫn kiểm thử tay kèm mẫu biên bản.
- Ý nghĩa: Desktop có đủ hai màn hình của luồng Mức 1. Quản lý tạo món và giá, thu ngân tiếp nhận đơn khách đặt trên Web.
- Thêm:
  - `Client/StaffApi.cs`, `Client/StaffPresentation.cs`.
  - `Desktop/Dialogs.cs`, `Desktop/MenuManagementForm.cs`, `Desktop/OrderReceivingForm.cs`, `Desktop/Ui.cs`.
  - `tests/DesktopFlowTests.cs`, `tests/StaffPresentationTests.cs`.
  - `docs/kiem-thu-tay.md`.
- Sửa: `README.md`, `docs/huong-dan-demo.md`, `docs/ke-hoach-den-thu-7.md`, `docs/nhiem-vu/C-nguyen-quoc-an.md`, `docs/quy-uoc-du-an.md`, `docs/verification.md`, `Desktop/Form1.cs`.

Gộp: PR #7, A gộp lúc 19:37.

### Chủ nhật 15/11

**08:41 · `6011399` · A · Sửa hướng dẫn kiểm thử tay, thêm script kiểm tra máy**
- Làm gì: sửa các bước của ca KT18, KT21 và lệnh `sqlcmd -I` trong hướng dẫn. Thêm script kiểm tra máy trước khi chạy. Bỏ dòng giữ chỗ trên trang chủ Web, đây là file thuộc phần của B. Ghi kết quả chạy thử.
- Ý nghĩa: máy mới kiểm tra được môi trường trước khi chạy, và hướng dẫn dùng đúng lệnh.
- Thêm: `scripts/kiem-tra-may.ps1`.
- Sửa: `docs/kiem-thu-tay.md`, `docs/verification.md`, `Web/Views/Home/Index.cshtml`.

**08:52 · `7a8d253` · A · Desktop: ô chọn ngày giờ hiện được giờ phút**
- Làm gì: mở rộng ô chọn ngày giờ trong hộp thoại Đổi giá và Công thức. Sửa trong lúc chạy kiểm thử giao diện.
- Ý nghĩa: quản lý nhập được cả giờ phút bắt đầu hiệu lực của giá và công thức.
- Sửa: `Desktop/Dialogs.cs`.

**09:07 · `7dadaad` · A · Biên bản kiểm thử giao diện lần 1**
- Làm gì: chạy tự động 18 ca kiểm thử trên giao diện thật của Web và Desktop: 17 Đạt, 1 Không đạt (KT19). Kèm 99 ảnh chụp và kết quả truy vấn SQL.
- Ý nghĩa: Chương 5 có bằng chứng kèm ảnh. Đây là lần chạy tự động, chưa phải người thao tác tay; theo kế hoạch, các ca chạy tay giao cho B (Web) và C (Desktop).
- Thêm: `docs/bien-ban-kiem-thu.md`, 99 ảnh trong `docs/bang-chung/`.
- Sửa: `docs/kiem-thu-tay.md`, `docs/verification.md`.

**14:10 · `abbb594` · A · Cập nhật tài liệu theo kết quả 15/11**
- Làm gì: chạy lại toàn bộ test, 105/105 đạt, ghi lại mã băm của file kết quả. Cập nhật hiện trạng và phân việc trong các file kế hoạch, quy ước, README.
- Ý nghĩa: tài liệu khớp với kết quả chạy thật.
- Sửa: `README.md`, `docs/ke-hoach-den-thu-7.md`, `docs/nhiem-vu/A-ngo-nguyen.md`, `docs/nhiem-vu/B-le-nam-thuan.md`, `docs/nhiem-vu/C-nguyen-quoc-an.md`, `docs/quy-uoc-du-an.md`, `docs/verification.md`.

Gộp: PR #8 (cả 4 commit ngày 15/11), A gộp lúc 14:19.

### Thứ Hai 16/11

PR #9 (A mở): nhánh thử chuẩn hóa `PreparationUnit` về 3NF. PR này đóng mà không gộp; việc được làm lại ở PR #14 ngày 18/11.

**22:59 · `84addf1` · A · Controller khách nhận `ICustomerGateway` qua DI (A0.2)**
- Làm gì: bỏ biến static `CustomerGatewayProvider`. Ba controller khách nhận cổng nghiệp vụ qua constructor, đăng ký trong `Program.cs`.
- Ý nghĩa: đúng nguyên tắc đảo phụ thuộc (DIP) và dễ thay bản giả khi test. Làm trước để B sửa `MyOrdersController` (B2) không bị xung đột.
- Sửa: `Web/Controllers/Web/AccountController.cs`, `Web/Controllers/Web/CheckoutController.cs`, `Web/Controllers/Web/MyOrdersController.cs`, `Web/Models/CustomerGateway.cs`, `Web/Program.cs`.

**22:59 · `62388e4` · A · Hợp đồng API cho A1–A3 (A0.1)**
- Làm gì: thêm DTO và hàm gọi trong `StaffApi` cho danh mục, nguyên liệu, ca, thu tiền và pha chế. Thêm quyền `PAYMENT_COLLECT`, `PREPARATION_WORK` và tài khoản mẫu `dev.barista`. Thêm test khóa đường dẫn API.
- Ý nghĩa: C dựng được các form Thu ngân, Pha chế, Danh mục, Nguyên liệu ngay theo hợp đồng, không phải chờ máy chủ.
- Thêm: `tests/StaffApiContractTests.cs`.
- Sửa: `docs/nhiem-vu/A-ngo-nguyen.md`, `Client/ApiClient.cs`, `Client/StaffApi.cs`, `Core/ApiContracts.cs`, `Infrastructure/DevelopmentAccountSeeder.cs`, `Web/Services/AuthSetup.cs`, `tests/DatabaseTests.cs`.

Gộp: PR #10, A gộp lúc 23:07. 109/109 test đạt.

### Thứ Ba 17/11

**13:30 · `5116319` · A · Máy chủ cho danh mục, nguyên liệu, ca, thu tiền và pha chế (A1–A3)**
- Làm gì:
  - A1: thêm, sửa, xóa, tìm danh mục và nguyên liệu. Tên không được trùng; chỉ xóa được khi chưa bị tham chiếu, còn tham chiếu thì trả 409.
  - A2: mở ca, đóng ca, đối soát, danh sách đơn còn phải thu, thu tiền mặt. Ghi sổ đúng số còn phải thu; tiền khách đưa dư thành tiền thối. Hai người thu cùng một đơn không thu vượt được.
  - A3: hàng đợi ly, bắt đầu pha, sẵn sàng, đã giao theo từng nấc. Bắt đầu pha thì trừ nguyên liệu; giao ly thì ghi người giao. Đơn có thêm trường `progress` để Web hiện tiến độ.
  - Thêm 8 test, tổng 117/117 đạt.
- Ý nghĩa: luồng bán hàng khép kín được ở phía máy chủ: tiếp nhận, pha chế, thu tiền, đóng ca.
- Thêm:
  - `Infrastructure/OrderTotals.cs`.
  - `Infrastructure/Services/SqlErrors.cs`, `StaffCashierService.cs`, `StaffCatalogService.cs`, `StaffPreparationService.cs`.
  - `tests/StaffA123ApiTests.cs`, `tests/StaffApiClientA123Tests.cs`.
- Sửa: `docs/nhiem-vu/A-ngo-nguyen.md`, `Core/DataContracts.cs`, `Infrastructure/CafeDataCommands.cs`, `Infrastructure/Services/CustomerOrderService.cs`, `Web/Controllers/Api/StaffController.cs`, `Web/Program.cs`, `tests/TestData.cs`.

Gộp: PR #11, A gộp lúc 19:23.

### Thứ Tư 18/11

**20:29 · `9d26373` · C · Desktop: màn hình Thu ngân (A4)**
- Làm gì: mở ca, chọn đơn đã tiếp nhận, nhập tiền khách đưa, tính tiền thối, đóng ca và xem chênh lệch.
- Ý nghĩa: nhân viên thu tiền và đối soát ca ngay trên Desktop (UC08).
- Thêm: `Desktop/CashierDialogs.cs`, `Desktop/CashierForm.cs`.

**20:30 · `84d7770` · C · Desktop: màn hình Pha chế (C3)**
- Làm gì: danh sách ly đang chờ; các nút bắt đầu làm, sẵn sàng, đã giao; tự làm mới danh sách.
- Ý nghĩa: pha chế làm theo từng ly trên Desktop (UC07), và khách thấy tiến độ đó trên Web.
- Thêm: `Desktop/PreparationForm.cs`.

**20:37 · `6f843f3` · C · Desktop: Danh mục (C1) và Nguyên liệu (C2)**
- Làm gì:
  - Form danh mục và form nguyên liệu: xem, tìm, thêm, sửa, xóa có hỏi xác nhận.
  - Màn hình chính thêm nút Danh mục, Nguyên liệu, Thu ngân, Pha chế, mỗi nút chỉ hiện theo quyền.
  - Thêm `CashierPresentation`: phần kiểm tra đầu vào và hiển thị của Thu ngân, Pha chế, tách khỏi Windows Forms để kiểm thử được. File này nằm trong `Client`, thư mục của A.
- Ý nghĩa: Desktop đủ thêm, xóa, sửa, xem, tìm cho các danh mục nghiệp vụ như đề yêu cầu. Danh mục và nguyên liệu xóa được khi chưa bị dùng, gỡ một phần lý do ca KT19 Không đạt.
- Thêm: `Client/CashierPresentation.cs`, `Desktop/CatalogDialogs.cs`, `Desktop/CategoryForm.cs`, `Desktop/IngredientForm.cs`.
- Sửa: `Desktop/Form1.cs`.

**20:38 · `e02e301` · C · Test cho logic Thu ngân**
- Làm gì: thêm test tự động cho `CashierPresentation`.
- Ý nghĩa: phần tính tiền và kiểm tra đầu vào của màn hình Thu ngân được kiểm thử tự động.
- Thêm: `tests/CashierPresentationTests.cs`.

**21:39 · `2b4c8ff` · B (LeeNaimThwuan) · Web: chi tiết món (B1), tiến độ và thanh toán trong đơn (B2)**
- Làm gì: trang chi tiết một món, gồm biến thể, giá, tùy chọn thêm và nút thêm vào giỏ; món không tồn tại thì có trang báo riêng. Biểu mẫu chọn biến thể tách thành phần dùng chung. "Đơn của tôi" hiện tiến độ pha chế, số ly ở từng trạng thái và tình trạng thanh toán; đơn còn chờ, bị từ chối hoặc đã hủy thì có câu giải thích.
- Ý nghĩa: có trang chi tiết như đề yêu cầu. Khách thấy đơn đổi trạng thái khi nhân viên thao tác ở Desktop, thể hiện Web, Desktop và CSDL đồng bộ với nhau.
- Thêm: `Web/Controllers/Web/ProductController.cs`, `Web/Views/Product/Detail.cshtml`, `Web/Views/Product/NotFound.cshtml`, `Web/Views/Shared/_VariantForm.cshtml`.
- Sửa: `Web/Controllers/Web/MyOrdersController.cs`, `Web/Models/CustomerGateway.cs`, `Web/Models/OrderModels.cs`, `Web/Views/Home/Index.cshtml`, `Web/Views/MyOrders/Detail.cshtml`, `Web/Views/MyOrders/Index.cshtml`.

Gộp: PR #12, A gộp lúc 21:54. Bốn commit buổi tối của C được đẩy thẳng vào `main`, không qua PR. Sau đó A bật bảo vệ nhánh `main` (A15): bắt buộc qua PR, cấm force-push và xóa nhánh. Việc này làm trong phần cài đặt của GitHub, không có commit.

**23:10 · `fd0a574` · A · Phân tích 3NF và mô hình dữ liệu rút gọn (A12)**
- Làm gì: đưa lên repo phân tích chuẩn hóa từ 1NF đến BCNF cho 40 bảng, và mô hình rút gọn (30 bảng lõi, 10 bảng mở rộng). Hai file viết từ 16/11 và báo cáo đã trích nhưng chưa có trên GitHub; số liệu được cập nhật theo mã ngày 18/11.
- Ý nghĩa: có bằng chứng cho tiêu chí "CSDL đạt 3NF" trong rubric. Thầy mở repo đối chiếu với báo cáo sẽ thấy được file.
- Thêm: `docs/mo-hinh-du-lieu-rut-gon.md`, `docs/phan-tich-3nf.md`.
- Sửa: `docs/relation-mapping.md`.

Gộp: PR #13, A gộp lúc 23:20.

**23:35 · `8001428` · A · Chuẩn hóa `PreparationUnit` về 3NF (A17)**
- Làm gì:
  - Bỏ hai cột `TicketId` và `RecipeVersionId` khỏi `PreparationUnit`, vì hai cột này chỉ phụ thuộc vào dòng món (vi phạm 2NF, 3NF). Thêm `RecipeVersionId` vào `OrderLine`.
  - Migration `PreparationUnit3NF` chép dữ liệu cũ sang chỗ mới trước khi bỏ cột, và có `Down` để quay lại. Ba trigger thay vai trò của hai khóa ngoại đã bỏ.
  - Sửa dịch vụ pha chế cho khớp; thêm test cho trigger mới. Tổng 149/149 test đạt.
- Ý nghĩa: cả 40 bảng đạt BCNF. Dữ liệu cũ được giữ nguyên: đã thử trên bản sao CSDL thật có 63 ly.
- Thêm: `Infrastructure/Migrations/20261118162917_PreparationUnit3NF.cs` (kèm `.Designer.cs`), `Infrastructure/Migrations/PreparationUnit3NFSql.cs`.
- Sửa:
  - `README.md`, `docs/mo-hinh-du-lieu-rut-gon.md`, `docs/phan-tich-3nf.md`, `docs/relation-mapping.md`, `docs/schema.md`.
  - `Core/Entities.cs`, `Infrastructure/CafeDataCommands.cs`, `Infrastructure/CafeDbContext.cs`, `Infrastructure/Migrations/CafeDbContextModelSnapshot.cs`, `Infrastructure/Services/StaffPreparationService.cs`.
  - `tests/DatabaseTests.cs`.

Gộp: PR #14, A gộp lúc 23:40. Sau commit này, mỗi máy cần chạy `--migrate` một lần.

### Thứ Năm 19/11

**09:34 · `bf25b22` · A · README mới (A7, PR #15, chưa gộp)**
- Làm gì: viết lại README theo hiện trạng 19/11, gồm các màn hình mới, migration mới, bảo vệ nhánh và giới hạn hiện tại. Thêm bảng nhật ký thay đổi. Ghi rõ trong biên bản kiểm thử rằng commit `7a8d253` đã lên `main` qua PR #8.
- Ý nghĩa: người mở repo, kể cả thầy, đọc README là biết hệ thống đang làm được gì và chạy thế nào.
- Thêm: `docs/nhat-ky-thay-doi.md`.
- Sửa: `README.md`, `docs/bien-ban-kiem-thu.md`.

**Lần cập nhật đưa file này vào repo (A, 19/11):**
- Thêm: `docs/phan-cong.md` (file này) và `docs/hop-dong-api.md`. File `hop-dong-api.md` chứa hai bảng hợp đồng API chuyển ra từ `docs/nhiem-vu/A-ngo-nguyen.md`.
- Xóa: `docs/ke-hoach-den-thu-7.md`, `docs/nhiem-vu/A-ngo-nguyen.md`, `docs/nhiem-vu/B-le-nam-thuan.md`, `docs/nhiem-vu/C-nguyen-quoc-an.md` (kế hoạch 10/11–14/11, đã hết hạn và được thay bằng mục 2) và `docs/nhat-ky-thay-doi.md` (được thay bằng mục 3).
- Sửa: `README.md` và chú thích đầu `Core/ApiContracts.cs`, cho trỏ tới các file mới.
- Ý nghĩa: phân công và lịch sử làm việc nằm ở một chỗ, không còn các bản kế hoạch cũ ghi trạng thái sai.
