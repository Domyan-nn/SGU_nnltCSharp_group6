# CafeManager — Đồ án C# Nhóm 6 (Đề tài 11: Quản lý quán cà phê)

Tóm tắt quy ước, cấu trúc và hiện trạng của dự án. Chi tiết nằm trong README.md và các tệp khác trong docs/.

## Đề tài & yêu cầu
- Desktop C# WinForms + Web C# (ASP.NET Core) dùng chung SQL Server. Khuyến khích EF, API, phân lớp.
- Một kho, một cửa hàng. 19 use case (UC01–19), FR01–15, BR01–12, NFR; 40 quan hệ dữ liệu.
- Kiến trúc: Web là máy chủ MVC/API; Desktop gọi HTTPS (HttpClient), không truy cập DB trực tiếp.

## Tài liệu nguồn (Google Drive, cần đăng nhập)
Thư mục: https://drive.google.com/drive/folders/1Wj-u4_8ctqh_lf5BonmDJDKznHSdjb35
- [C# Nhóm 6] ĐỒ ÁN C-SHARP (báo cáo chính, Google Docs) và PowerPoint của đồ án.
- 01 SRS, 02 Nghiệp vụ và BFD, 03 Use case và User story, 04 Dữ liệu và ERD, 05 Giao diện, 06 Truy vết, kiểm thử và nghiệm thu (đều là Google Docs).
Bản tóm tắt cách dùng các tài liệu: [sources.md](sources.md). Tài liệu thay đổi thì sửa trên Drive; thư mục `docs/` này là bản làm việc của mã nguồn.

## Công nghệ
.NET 10 (SDK khóa 10.0.403 trong global.json), EF Core 10.0.14, SQL Server (LocalDB `(localdb)\MSSQLLocalDB`), ASP.NET Core Identity (IdentityUserContext, không dùng role store; quyền qua StaffRole/RolePermission).

## Cấu trúc
- src/CafeManager.Core: entity (40 bảng nghiệp vụ), ICafeDataCommands, hợp đồng API (ApiContracts.cs: DTO, quyền, mã lỗi, AppException)
- src/CafeManager.Infrastructure: DbContext, 5 migration (InitialSchema, IntegrityGuards, HistoryProtection, SchemaGapColumns, PreparationUnit3NF), seed, CafeDataCommands; Services/: AccountService, MenuQueryService, CustomerOrderService, StaffOrderService, StaffMenuService, StaffCatalogService, StaffCashierService, StaffPreparationService, StaffReportService, StaffAccountService
- src/CafeManager.Web: host ASP.NET Core: Controllers/Api (auth, menu, orders, staff, báo cáo), Controllers/Web (giao diện khách của B), cookie xác thực, antiforgery, session; CAFE_FAKE_GATEWAY=1 chạy giao diện với dữ liệu giả
- src/CafeManager.Client: ApiClient (HttpClient + CookieContainer riêng, phân loại lỗi 400–409, "chưa xác định" khi mất mạng, hết giờ hoặc 5xx), StaffApi và các lớp logic hiển thị; chỉ phụ thuộc Core
- src/CafeManager.Desktop: WinForms: D00 đăng nhập, màn hình chính theo quyền, D02 tiếp nhận hoặc từ chối đơn Web, D06 quản lý món, giá, công thức, Danh mục, Nguyên liệu, Thu ngân, Pha chế, Báo cáo doanh thu, Nhân viên và phân quyền (gọi API qua Client, không tham chiếu EF hay SqlClient)
- tests/CafeManager.Tests: 182 test, xem [kiem-thu.md](kiem-thu.md): 72 ca cần SQL Server (cần biến CAFE_TEST_SQLSERVER, nếu không các ca này Skipped) và 110 ca không cần
- docs/: business-rules.md, relation-mapping.md, schema.md, phan-tich-3nf.md, hop-dong-api.md, kiem-thu.md, bien-ban-kiem-thu.md, verification.md, huong-dan-demo.md, phan-cong.md

## Hiện trạng (21/11/2026)
- Cơ sở dữ liệu: 40 bảng nghiệp vụ và 4 bảng Identity, 5 migration, ràng buộc và trigger bảo vệ toàn vẹn; cả 40 bảng đạt BCNF sau migration PreparationUnit3NF (xem phan-tich-3nf.md).
- Đã làm 12 trong 19 use case: UC01–UC04, UC07, UC08, UC14–UC19. Web khách: thực đơn, chi tiết món, giỏ hàng, đăng ký, đăng nhập, đặt đơn, đơn của tôi kèm tiến độ pha chế và thanh toán. Desktop: 7 trong 9 màn hình D00–D08 (chưa có D01 và D05).
- Kiểm thử: 182/182 test tự động đạt; 29 ca KT/LT ở tài liệu 06 đã chạy thử và Đạt (KT19 lần 1 ngày 15/11 Không đạt theo kỳ vọng gốc, lần 2 Đạt theo kỳ vọng đã sửa theo quyết định C10). Ảnh và truy vấn SQL làm bằng chứng nằm ở bien-ban-kiem-thu.md.
- CI: GitHub Actions chạy build và toàn bộ test với SQL Server trong container.
- Chưa làm (ghi vào giới hạn đề tài): phục vụ tại bàn và chuyển bàn (UC05–06), hủy và duyệt hủy món (UC10–11), hoàn tiền (UC12), nhập và kiểm kê kho (UC13), đo hiệu năng, sao lưu và phục hồi, kết nối ngân hàng, hóa đơn điện tử và in phiếu.

## Quy tắc làm việc
- Không dùng EnsureCreated, drop/reset DB, hay hạ migration; thay đổi mô hình = migration mới, đọc Up/Down trước.
- Migration/seed chỉ chạy qua `dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed` (tài khoản mẫu: thêm `--seed-accounts`, mật khẩu ngẫu nhiên in một lần) với ASPNETCORE_ENVIRONMENT=Development, DB CafeManager_Group6_Dev.
- Không commit bí mật; connection string dùng Windows Auth/User Secrets.
- Không dùng SQLite/InMemory làm bằng chứng; không báo "đạt" khi test bị Skipped.
- `main` được bảo vệ: mọi thay đổi đi qua pull request.
- Không đưa ICafeDataCommands ra API công khai trước khi có quyền, validation, snapshot giá từ máy chủ.
- Tài liệu và UI bằng tiếng Việt.
