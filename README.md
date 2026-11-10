# Quản lý quán cà phê — Nhóm 6

Phần dữ liệu dùng **.NET 10, EF Core 10.0.14, SQL Server và ASP.NET Core Identity**. Đã xây dựng 40 quan hệ theo tài liệu (cộng bảng mở rộng AllowedAddon), migration, dữ liệu thử và kiểm thử SQL Server. Web/WinForms hiện là khung khởi động; chưa hoàn thiện 19 UC hay giao diện sản phẩm.

Xem [kết quả kiểm chứng](docs/verification.md), [đối chiếu 40 quan hệ và chuẩn hóa](docs/relation-mapping.md), [từ điển lược đồ](docs/schema.md), [quy tắc và giới hạn](docs/business-rules.md), [tài liệu nguồn](docs/sources.md).

## Cấu trúc và phụ thuộc

```text
CafeManager.sln
src/
  CafeManager.Core/             Entity, ICafeDataCommands, kết quả đối soát
  CafeManager.Infrastructure/   DbContext, Identity store, transaction, seed, migrations
  CafeManager.Web/              Host ASP.NET Core, DI, nền MVC/API, lệnh dữ liệu Development
  CafeManager.Desktop/          Khung Windows Forms
tests/
  CafeManager.Tests/            33 kiểm thử tích hợp SQL Server
docs/                          Thiết kế, đối chiếu và bằng chứng
database/                      Truy vấn kiểm tra chỉ đọc
```

Infrastructure → Core; Web → Infrastructure (và Core qua project reference); Tests → Infrastructure. Core không phụ thuộc EF/SQL Server. Gộp Domain và hợp đồng Application vào Core vì phạm vi này chưa cần một project Application riêng. Khi xây UC có thể tách theo nhu cầu thực tế.

Desktop hiện độc lập, không tham chiếu Infrastructure, EF hay SqlClient, không chứa connection string SQL. Các màn hình WinForms sau này dùng HttpClient gọi API HTTPS của Web; controller và MVC dùng cùng dịch vụ máy chủ → EF Core → SQL Server. Chưa có API ghi dữ liệu công khai, phiên đăng nhập cookie hay tích hợp hai giao diện.

## Môi trường Windows / Visual Studio

Máy đã kiểm tra: Windows x64, SDK 10.0.403, Visual Studio 18.11.12328.437, LocalDB SQL Server 2025 (17.0.4025.3). `global.json` khóa SDK 10.0.403, cho phép bản vá cùng feature band. Máy khác cần cài SDK này hoặc chủ động cập nhật global.json sau kiểm tra tương thích.

Trong Visual Studio Installer chọn **ASP.NET and web development**, **.NET desktop development**, thành phần SQL Server Express LocalDB / SQL Server Data Tools. Dùng Visual Studio hỗ trợ .NET 10; không mặc định Visual Studio cũ mở được solution.

Mở PowerShell/Terminal ở gốc repo:

```powershell
dotnet --info
dotnet --list-sdks
sqllocaldb info
sqllocaldb info MSSQLLocalDB
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -l 15 -Q "SELECT @@VERSION; SELECT name FROM sys.databases;"
dotnet tool restore
dotnet ef --version
dotnet restore CafeManager.sln
dotnet build CafeManager.sln --no-restore
```

Nếu LocalDB chưa có, cài LocalDB rồi dùng `sqllocaldb create MSSQLLocalDB` và `sqllocaldb start MSSQLLocalDB`. Không cần tạo lại instance đã có. Lỗi registry/quyền trong sandbox không đồng nghĩa thiếu SQL Server: phiên làm việc này phải kiểm tra ngoài sandbox mới truy cập được. Nếu máy khác thiếu SQL Server, vẫn có thể build/tạo migration; các kiểm thử tích hợp sẽ không được coi là đạt khi chưa chạy SQL Server.

## Kết nối an toàn

Mặc định trong Web/appsettings.json và design-time factory:

```text
Server=(localdb)\MSSQLLocalDB;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true
```

Không chứa mật khẩu. TrustServerCertificate=true chỉ phục vụ LocalDB cục bộ; SQL Server máy chủ dùng chứng chỉ tin cậy và `TrustServerCertificate=false`, tài khoản quyền tối thiểu. Ví dụ không có bí mật: [appsettings.Example.json](docs/appsettings.Example.json).

Nếu đổi máy chủ, ưu tiên Windows Authentication. Có thể ghi cấu hình cục bộ cho Web bằng User Secrets:

```powershell
dotnet user-secrets set 'ConnectionStrings:CafeDatabase' 'Server=YOUR_SERVER;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=false' --project src/CafeManager.Web
```

Hoặc biến môi trường dùng chung Web/EF tooling:

```powershell
$env:ConnectionStrings__CafeDatabase='Server=YOUR_SERVER;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=false'
```

Nếu phải dùng SQL Authentication, nhập chuỗi có bí mật vào User Secrets bằng công cụ quản lý secrets của IDE hoặc biến môi trường cục bộ; không commit, không đưa mật khẩu thật vào lệnh minh họa hoặc log. Design-time factory đọc biến môi trường và mặc định LocalDB, **không đọc User Secrets của Web**; khi chạy `dotnet ef` hãy cấu hình biến môi trường đúng đích. Không bật EnableSensitiveDataLogging.

## Migration và dữ liệu thử

Kiểm tra server/CSDL đích trước khi chạy. Dự án không dùng EnsureCreated, không tự chạy migration/seed khi khởi động Web thông thường.

Đường dẫn an toàn cho phát triển (chỉ cho phép đúng tên CafeManager_Group6_Dev, môi trường Development; không drop/reset CSDL):

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate
dotnet run --project src/CafeManager.Web --no-launch-profile -- --seed
```

Có thể dùng `--migrate --seed` trong cùng một lần chạy. Lần này đã xác nhận đích chưa tồn tại trước khi tạo. Khi dùng CSDL cùng tên có sẵn trên máy khác, phải kiểm tra lịch sử migration/schema trước; tên đúng không chứng minh CSDL đó thuộc đồ án.

EF CLI tương đương, **cần tự xác minh đích vì CLI EF không có guard của host**:

```powershell
dotnet ef migrations list --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
dotnet ef database update --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
dotnet ef migrations has-pending-model-changes --project src/CafeManager.Infrastructure --startup-project src/CafeManager.Web
```

Các file migration đã có: InitialSchema → IntegrityGuards → HistoryProtection → SchemaGapColumns. Không tạo lại InitialSchema ở máy khác. Khi đổi mô hình, tạo migration mới, đọc Up/Down/SQL và kiểm thử trên CSDL thử trước. Không chạy database drop, migration về 0 hay xóa migration để thay thế CSDL đang dùng.

Seed là tùy chọn, chạy lặp lại an toàn trong transaction có khóa: 1 danh mục, 3 món/biến thể (cà phê đen, cà phê sữa, thêm sữa), giá, 2 nguyên liệu, công thức, 3 bàn, 1 quầy và tồn thử có chứng từ mở đầu. Tên có [THỬ], mã DEMO-. Chỉ thêm đối tượng thiếu; không sửa giá/tồn/món đã tồn tại. Nếu người dùng đã sửa bản mẫu hoặc làm dữ liệu mẫu không đầy đủ, seed không tự sửa lại. Không tạo tài khoản, vai trò có quyền quản trị hoặc mật khẩu cố định. Các tài khoản trong tests nằm ở CSDL thử và được dọn cùng CSDL.

## Mở và kiểm tra trong Visual Studio

1. Mở `CafeManager.sln`. Chờ NuGet restore; chọn Build → Build Solution.
2. View → Terminal, chạy các lệnh migration/seed phía trên tại gốc repo.
3. View → SQL Server Object Explorer → Add SQL Server: `(localdb)\MSSQLLocalDB`, Windows Authentication.
4. Mở Databases → CafeManager_Group6_Dev → Tables. Có 40 bảng nghiệp vụ, 4 bảng Identity và __EFMigrationsHistory: tổng 45.
5. New Query trên đúng CSDL; chạy [database/inspect.sql](database/inspect.sql) để xem cột, FK, CHECK, chỉ mục, trigger, seed và các migration. Có thể làm tương tự trong SSMS.
6. Trong Solution Explorer chọn Web làm Startup Project, profile https, F5: hiện thông báo khung máy chủ; không tự thay đổi CSDL. Desktop làm Startup Project sẽ mở khung WinForms, chưa có màn hình nghiệp vụ.
7. Để chạy Test Explorer có SQL, khởi động Visual Studio từ PowerShell đã đặt `CAFE_TEST_SQLSERVER`, hoặc chạy lệnh terminal dưới đây. Không có biến này thì 29 ca SQL đánh dấu **Skipped**, không phải Pass.

## Chạy kiểm thử thực

Tài khoản Windows cần quyền tạo CSDL thử trên máy chủ đã chọn:

```powershell
$env:CAFE_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test CafeManager.sln --no-restore --logger 'trx;LogFileName=database-tests.trx' --results-directory TestResults
```

Tests tạo CSDL mới `CafeManager_Group6_Test_<GUID>` bằng CREATE DATABASE (không nhận CSDL có sẵn), áp dụng toàn bộ migration rồi kiểm tra trực tiếp SQL Server. Chỉ dọn tên do fixture vừa tạo, có kiểm tra tiền tố và GUID; không dùng CSDL Development để chạy các ca phá ràng buộc. Nếu tiến trình bị kill trước cleanup, có thể còn CSDL thử; kiểm tra đúng tên/lần chạy trước khi tự dọn. Không dùng SQLite/InMemory làm bằng chứng SQL Server.

TRX nằm trong TestResults (gitignore). [verification.md](docs/verification.md) ghi kết quả thực tế và liên hệ ca kiểm thử với yêu cầu, không thay thế nghiệm thu sản phẩm.

## Bước tiếp theo

Nhóm cần xác nhận các [quyết định và phần thiếu](docs/business-rules.md), đặc biệt trạng thái, chính sách quyền/duyệt, idempotency và luồng tiền. Sau đó xây UC18/19 đăng ký/đăng nhập với Identity và transaction hồ sơ; bổ sung cookie/antiforgery riêng cho Web và Desktop; áp dụng StaffPermissionReader tại máy chủ; xây UC17/01/02/03 rồi thu tiền/đóng ca, hủy/hoàn theo đúng thiết kế.

ICafeDataCommands là các thao tác dữ liệu phía máy chủ đã kiểm thử, **không phải bộ UC hoàn chỉnh**. Không đưa chúng ra API ghi công khai trước khi bổ sung quyền, validation, snapshot giá từ máy chủ và các điều kiện còn thiếu. Chưa đo hiệu năng, backup/restore, kết nối thiết bị/ngân hàng hay nghiệm thu Web–Desktop.

