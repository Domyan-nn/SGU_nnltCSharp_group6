# Hệ thống quản lý quán cà phê – Nhóm 6

Đồ án môn Ngôn ngữ lập trình C# (đề tài 11), giảng viên Đỗ Như Tài. Nhóm gồm Ngô Nguyên (A), Lê Nam Thuận (B) và Nguyễn Quốc An (C).

Hệ thống có hai giao diện dùng chung một cơ sở dữ liệu SQL Server:

- **Web cho khách** (ASP.NET Core MVC): xem thực đơn và chi tiết món, chọn món, giỏ hàng, đăng ký, đăng nhập, đặt đơn mang đi, xem đơn của mình và tiến độ pha chế.
- **Desktop cho nhân viên** (Windows Forms): đăng nhập theo quyền, tiếp nhận hoặc từ chối đơn Web, thu tiền và mở, đóng ca, pha chế, quản lý món, danh mục và nguyên liệu.

Desktop không nối thẳng vào cơ sở dữ liệu. Nó gọi API của máy chủ Web bằng HTTP (cookie đăng nhập và token chống giả mạo), còn Web và API cùng dùng chung các dịch vụ nghiệp vụ rồi mới tới EF Core và SQL Server. Quyền của nhân viên được kiểm tra lại trong cơ sở dữ liệu ở mỗi yêu cầu.

Công nghệ: .NET 10 (SDK 10.0.403 khóa trong `global.json`), EF Core 10.0.14, SQL Server (LocalDB khi phát triển), ASP.NET Core Identity, xUnit.

## Đã làm được gì

Luồng nghiệp vụ xuyên suốt của đồ án: quản lý tạo món trên Desktop → khách đặt đơn trên Web → thu ngân tiếp nhận trên Desktop → pha chế từng ly → thu tiền mặt → đóng ca, trong đó khách theo dõi tiến độ đơn trên Web. Cả ba phần (máy chủ, Desktop, Web) của luồng này đều đã có mã và kiểm thử tự động; chưa ai chạy tay trọn luồng (xem "Mức kiểm chứng" bên dưới).

| Phần | Nội dung | Người làm chính |
|---|---|---|
| Cơ sở dữ liệu | 40 bảng nghiệp vụ và 4 bảng Identity, 5 migration, trigger bảo vệ toàn vẹn, các thao tác tiếp nhận đơn, trừ nguyên liệu, thu tiền, đối soát ca | A |
| API và dịch vụ máy chủ | Đăng nhập, đăng ký, thực đơn, đơn của khách, tiếp nhận, quản lý món, danh mục, nguyên liệu, ca, thu tiền, pha chế | A |
| Web khách | Thực đơn, chi tiết món, giỏ hàng, đăng ký, đăng nhập, đặt đơn, đơn của tôi | B |
| Desktop | Đăng nhập, tiếp nhận đơn, quản lý món, Thu ngân, Pha chế, Danh mục, Nguyên liệu | A viết khung đăng nhập, D02 và D06; C hoàn thiện đăng nhập và viết các màn còn lại |

Phân công chi tiết và lịch sử làm việc theo từng commit (ai làm, ngày nào, thêm, sửa, xóa file nào, để làm gì) nằm ở [docs/phan-cong.md](docs/phan-cong.md).

**Chưa làm** (ghi vào giới hạn đề tài): phục vụ tại bàn và chuyển bàn, hủy và duyệt hủy món, hoàn tiền, nhập và kiểm kê kho, báo cáo doanh thu, quản lý nhân viên trên giao diện, thanh toán chuyển khoản, đo hiệu năng, sao lưu và phục hồi.

**Mức kiểm chứng.** Bộ kiểm thử tự động có 149 ca: 62 ca cần SQL Server (cơ sở dữ liệu, dịch vụ, API qua HTTP, luồng Desktop qua `ApiClient` thật) và 87 ca không cần. Kiểm thử giao diện ngày 15/11 chạy tự động (17 Đạt, 1 Không đạt), xem [biên bản](docs/bien-ban-kiem-thu.md). Các màn hình mới từ 17/11 chưa được người chạy tay trọn luồng; kết quả chạy tay sẽ ghi vào [kiem-thu-tay.md](docs/kiem-thu-tay.md) và báo cáo.

## Cấu trúc thư mục

```text
CafeManager.sln
src/
  CafeManager.Core/             Entity, hợp đồng API (DTO, quyền, mã lỗi), ICafeDataCommands
  CafeManager.Infrastructure/   DbContext, migration, dịch vụ nghiệp vụ, thao tác dữ liệu, seed
  CafeManager.Web/              Host ASP.NET Core: API (Controllers/Api) và giao diện khách (Controllers/Web, Views)
  CafeManager.Client/           Máy khách API cho Desktop (HttpClient, cookie riêng mỗi phiên) và logic hiển thị; chỉ phụ thuộc Core
  CafeManager.Desktop/          Windows Forms, chỉ gọi API qua Client
tests/CafeManager.Tests/        Kiểm thử xUnit
docs/                           Tài liệu thiết kế, đối chiếu, kiểm thử và bằng chứng
database/                       Truy vấn kiểm tra chỉ đọc
scripts/                        Script kiểm tra máy trước khi chạy thử
```

Hướng phụ thuộc: Infrastructure → Core; Web → Infrastructure và Core; Client → Core; Desktop → Client và Core. Core không biết EF hay SQL Server, và Desktop không tham chiếu Infrastructure, EF hay SqlClient nên không chứa chuỗi kết nối cơ sở dữ liệu. Domain và hợp đồng nghiệp vụ gộp trong Core vì phạm vi đồ án chưa cần thêm một project Application riêng.

## Chạy thử trên máy mới

Cần Windows x64, .NET SDK 10.0.403 và SQL Server LocalDB. Trong Visual Studio chọn "ASP.NET and web development", ".NET desktop development" và thành phần LocalDB. Máy của A đã kiểm tra với Visual Studio 18.11 và LocalDB 17.0.4025.3. Chạy `scripts\kiem-tra-may.ps1` để máy tự kiểm tra các điều kiện (chỉ đọc, không đổi gì).

```powershell
dotnet tool restore
dotnet build CafeManager.sln
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed --seed-accounts
```

Lệnh cuối tạo cơ sở dữ liệu `CafeManager_Group6_Dev`, thêm dữ liệu mẫu (có đánh dấu `[THỬ]`) và bốn tài khoản mẫu: `dev.manager` (quyền quản lý món, danh mục, nguyên liệu), `dev.cashier` (tiếp nhận đơn và thu tiền), `dev.barista` (pha chế) và `dev.customer` (khách). **Mật khẩu ngẫu nhiên chỉ in ra một lần**, không lưu và không đưa lên git; quên thì phải tạo lại cơ sở dữ liệu Dev của máy mình.

Chạy máy chủ Web rồi Desktop (hai cửa sổ terminal):

```powershell
dotnet run --project src/CafeManager.Web --launch-profile http          # http://localhost:5055
$env:CAFE_API_BASE_URL='http://localhost:5055'
dotnet run --project src/CafeManager.Desktop
```

Kịch bản demo từng bước nằm ở [huong-dan-demo.md](docs/huong-dan-demo.md). Nếu muốn xem giao diện Web mà không cần cơ sở dữ liệu, đặt `CAFE_FAKE_GATEWAY=1` trước khi chạy Web (dùng dữ liệu giả). Nếu Windows báo "Application Control policy has blocked this file" thì máy đang bật Smart App Control, cần tắt hoặc dùng máy khác.

## Kết nối cơ sở dữ liệu

Mặc định (trong `appsettings.json` của Web và design-time factory), không chứa mật khẩu:

```text
Server=(localdb)\MSSQLLocalDB;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true
```

`TrustServerCertificate=true` chỉ dành cho LocalDB cục bộ. SQL Server máy chủ phải dùng chứng chỉ tin cậy, `TrustServerCertificate=false` và tài khoản quyền tối thiểu. Đổi máy chủ thì ưu tiên Windows Authentication; chuỗi có bí mật đặt trong User Secrets hoặc biến môi trường `ConnectionStrings__CafeDatabase`, không commit. Công cụ `dotnet ef` không đọc User Secrets của Web, nên khi chạy `dotnet ef` hãy đặt biến môi trường đúng đích. Ví dụ không có bí mật: [appsettings.Example.json](docs/appsettings.Example.json).

## Migration và dữ liệu thử

Dự án không dùng `EnsureCreated` và không tự chạy migration hay seed khi Web khởi động. Các lệnh `--migrate`, `--seed`, `--seed-accounts` chỉ chạy ở môi trường Development và chỉ với cơ sở dữ liệu tên đúng `CafeManager_Group6_Dev`; không có lệnh nào xóa hay đặt lại cơ sở dữ liệu. Thay đổi mô hình = tạo migration mới, đọc `Up` và `Down` trước, thử trên cơ sở dữ liệu thử rồi mới áp dụng.

Các migration hiện có: InitialSchema → IntegrityGuards → HistoryProtection → SchemaGapColumns → PreparationUnit3NF. Migration cuối (18/11/2026) chuẩn hóa bảng `PreparationUnit` về 3NF: bỏ `TicketId` và `RecipeVersionId`, chuyển công thức lên `OrderLine`. **Sau khi kéo mã mới về, mỗi máy cần chạy `--migrate` một lần** (không cần `--seed`), nếu không việc tiếp nhận đơn và pha chế sẽ lỗi. Muốn quay lại: `dotnet ef database update SchemaGapColumns`.

Lệnh EF tương đương (cần tự kiểm tra đích vì EF CLI không có chốt chặn của host):

```powershell
dotnet ef migrations list --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
dotnet ef database update --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
dotnet ef migrations has-pending-model-changes --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
```

Seed chạy lại an toàn: chỉ thêm đối tượng còn thiếu (1 danh mục, 3 món, giá, 2 nguyên liệu, công thức, 3 bàn, 1 quầy `DEMO-C01`, tồn thử), không sửa món, giá hay tồn đã có. Xem cấu trúc trong Visual Studio: SQL Server Object Explorer → `(localdb)\MSSQLLocalDB` → `CafeManager_Group6_Dev` (44 bảng và `__EFMigrationsHistory`); [database/inspect.sql](database/inspect.sql) liệt kê cột, khóa ngoại, CHECK, chỉ mục và trigger.

## Chạy kiểm thử

```powershell
$env:CAFE_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test CafeManager.sln --logger 'trx;LogFileName=tests.trx' --results-directory TestResults
```

Không đặt biến `CAFE_TEST_SQLSERVER` thì 62 ca cần SQL Server hiện **Skipped**, đó không phải Pass. Mỗi lần chạy, các ca SQL tạo một cơ sở dữ liệu riêng `CafeManager_Group6_Test_<GUID>` bằng `CREATE DATABASE`, áp dụng toàn bộ migration, rồi xóa đúng cơ sở dữ liệu đó khi xong; không động vào cơ sở dữ liệu Dev. Không dùng SQLite hay InMemory làm bằng chứng cho SQL Server. Kết quả từng lần chạy ghi ở [verification.md](docs/verification.md); file `.trx` nằm trong `TestResults` (không commit).

## Cách nhóm làm việc

- Mỗi người làm trên nhánh riêng, commit bằng tài khoản của mình và mở pull request vào `main`; từ tối 18/11 `main` được bảo vệ, không đẩy thẳng được.
- Chia thư mục để tránh xung đột: A sửa `Core`, `Infrastructure`, `Controllers/Api`, `Client`, `tests`, `Program.cs`; B sửa `Controllers/Web`, `Views`, `wwwroot`; C sửa `Desktop`. Cần sửa ngoài phần mình thì nhắn người phụ trách trước.
- Không commit mật khẩu, chuỗi kết nối có bí mật hay file `TestResults`. Không báo "đạt" khi test bị Skipped.
- Phân công, tiến độ và lịch sử làm việc nằm ở [docs/phan-cong.md](docs/phan-cong.md) (bảng phân công chép từ Sheet của nhóm); từng người ghi nhật ký làm việc cá nhân trong `docs/nhat-ky-<tên>.md`.

## Nguồn và công cụ đã dùng

**Nguồn.** Yêu cầu và phạm vi lấy từ tài liệu đề tài của giảng viên và các tài liệu thiết kế của nhóm (báo cáo chính, SRS, nghiệp vụ, use case, dữ liệu, giao diện, kiểm thử); danh sách và đường dẫn ở [docs/sources.md](docs/sources.md). Kỹ thuật tham khảo từ tài liệu chính thức của Microsoft (EF Core, ASP.NET Core Identity, cookie, chống giả mạo yêu cầu, transaction, đồng thời) được liệt kê trong phần Tài liệu tham khảo của báo cáo.

**Thư viện.** Chỉ dùng gói NuGet: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (đều 10.0.14), `System.Security.Cryptography.Pkcs` (10.0.14, ghim trực tiếp để thay bản phụ thuộc bị NuGet audit cảnh báo), và cho kiểm thử `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`. Giao diện Web dùng CSS tự viết trong `wwwroot/css`, không dùng thư viện giao diện bên ngoài.

**Công cụ.** Visual Studio, Git và GitHub, SQL Server LocalDB, `sqlcmd`, Microsoft Edge (điều khiển tự động khi kiểm thử giao diện Web), Google Drive và Google Docs (tài liệu và báo cáo).

## Giới hạn hiện tại và hướng phát triển

- **Chưa dùng Docker.** Desktop là Windows Forms và cơ sở dữ liệu phát triển là LocalDB, cả hai chỉ chạy trên Windows, nên đóng gói bằng Docker chưa cần cho việc phát triển và demo; nhóm ưu tiên hoàn thiện chức năng trong thời hạn. Hướng đi: đóng gói máy chủ Web và SQL Server bằng `docker-compose`, dùng cùng container SQL Server để chạy cả các test cần SQL Server trên CI.
- **Giỏ hàng và session đang lưu trong bộ nhớ của tiến trình Web** (`AddDistributedMemoryCache`), nên khởi động lại máy chủ thì giỏ hàng mất và chưa chạy được nhiều bản máy chủ cùng lúc. Hướng đi: lưu session và khóa Data Protection trong SQL Server.
- `ICafeDataCommands` là các thao tác dữ liệu đã kiểm thử, không phải bộ use case hoàn chỉnh; không đưa trực tiếp ra API công khai mà không qua lớp dịch vụ có kiểm tra quyền và kiểm tra đầu vào.
- Chưa đo hiệu năng, sao lưu hoặc phục hồi, kết nối thiết bị in hay ngân hàng.

## Tài liệu trong `docs/`

[quy-uoc-du-an.md](docs/quy-uoc-du-an.md) (quy ước), [schema.md](docs/schema.md) (từ điển lược đồ), [relation-mapping.md](docs/relation-mapping.md) và [phan-tich-3nf.md](docs/phan-tich-3nf.md) (chuẩn hóa), [mo-hinh-du-lieu-rut-gon.md](docs/mo-hinh-du-lieu-rut-gon.md), [business-rules.md](docs/business-rules.md), [verification.md](docs/verification.md), [kiem-thu-tay.md](docs/kiem-thu-tay.md), [bien-ban-kiem-thu.md](docs/bien-ban-kiem-thu.md), [huong-dan-demo.md](docs/huong-dan-demo.md), [phan-cong.md](docs/phan-cong.md) (phân công và lịch sử làm việc), [hop-dong-api.md](docs/hop-dong-api.md) (hợp đồng API).
