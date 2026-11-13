# CafeManager — Đồ án C# Nhóm 6 (Đề tài 11: Quản lý quán cà phê)

Tóm tắt quy ước, cấu trúc và hiện trạng của dự án. Chi tiết nằm trong README.md và các tệp khác trong docs/.

## Đề tài & yêu cầu
- Desktop C# WinForms + Web C# (ASP.NET Core) dùng chung SQL Server. Khuyến khích EF, API, phân lớp.
- Một kho, một cửa hàng. 19 use case (UC01–19), FR01–15, BR01–12, NFR; 40 quan hệ dữ liệu.
- Kiến trúc: Web là máy chủ MVC/API; Desktop gọi HTTPS (HttpClient), không truy cập DB trực tiếp.

## Tài liệu nguồn (Google Drive, cần đăng nhập)
Thư mục: https://drive.google.com/drive/folders/1Wj-u4_8ctqh_lf5BonmDJDKznHSdjb35
- ĐỒ ÁN C-SHARP.docx (báo cáo chính), Kiến_Trúc_Hệ_Thống_Quản_Lý_Quán_Cà_Phê.pptx
- 01 SRS, 02 Nghiệp vụ & BFD, 03 UC/User story, 04 Dữ liệu & ERD, 05 Giao diện, 06 Truy vết/kiểm thử/nghiệm thu
- 07 Nguồn sơ đồ (zip)
Bản tóm tắt cách dùng các tài liệu: docs/sources.md (đã gồm 02 và 05).
Bản cục bộ đọc được ở `../CK_GD1_Nhom11_NgoNguyen_3124411191-.../` (bản gốc, không sửa). Bản đã chỉnh cho khớp mã nguồn nằm ở `../bao-cao-da-sua/` kèm CHANGELOG.md; các điểm cần nhóm xác nhận ghi ở đó và ở docs/business-rules.md.

## Công nghệ
.NET 10 (SDK khóa 10.0.403 trong global.json), EF Core 10.0.14, SQL Server (LocalDB `(localdb)\MSSQLLocalDB`), ASP.NET Core Identity (IdentityUserContext, không dùng role store; quyền qua StaffRole/RolePermission).

## Cấu trúc
- src/CafeManager.Core — entity, ICafeDataCommands, hợp đồng API (ApiContracts.cs: DTO, quyền, mã lỗi, AppException)
- src/CafeManager.Infrastructure — DbContext, migrations (InitialSchema, IntegrityGuards, HistoryProtection, SchemaGapColumns), seed, CafeDataCommands; Services/: AccountService, MenuQueryService, CustomerOrderService, StaffOrderService, StaffMenuService
- src/CafeManager.Web — host ASP.NET Core: Controllers/Api (auth, menu, orders, staff), Controllers/Web (giao diện khách của B), cookie xác thực + antiforgery + session; CAFE_FAKE_GATEWAY=1 chạy giao diện với dữ liệu giả
- src/CafeManager.Client — ApiClient (HttpClient + CookieContainer riêng, phân loại lỗi 400–409, "chưa xác định" khi mất mạng/hết giờ/5xx); chỉ phụ thuộc Core
- src/CafeManager.Desktop — WinForms: D00 đăng nhập (gọi API thật); màn hình D06 và D02 đang làm
- tests/CafeManager.Tests — 49 test tích hợp SQL Server (CSDL, dịch vụ, API qua HTTP) + 15 test ApiClient (không cần SQL) (cần env CAFE_TEST_SQLSERVER, nếu không sẽ Skipped)
- docs/: business-rules.md (ma trận BR, quyết định cần nhóm chốt), relation-mapping.md, schema.md, verification.md

## Hiện trạng
- Xong: lược đồ 40 bảng + 4 Identity, 68 FK, 52 CHECK, 13 trigger, 8 RowVersion; seed; các thao tác dữ liệu (AcceptRound, StartUnit, CashPayment, CashRefund, ReconcileShift) có test.
- Xong (12/11): API UC18/19, UC01, UC02, UC03, UC04 (xem/hủy), UC17 qua HTTP có kiểm tra quyền trong CSDL, antiforgery, khóa tài khoản; 64 test đạt. Web khách (B) dùng dữ liệu thật qua cổng và MenuSource.
- Chưa làm: màn hình Desktop D06/D02, thu tiền và ca (UC08/14), pha chế/bàn (UC05–07, 09), hủy/hoàn trọn vẹn (UC10–12), kho (UC13), báo cáo/quản trị (UC15/16), hiệu năng, sao lưu.
- Bước tiếp theo: Desktop D06/D02 nối API, kiểm thử tay KT/TC mức hệ thống, báo cáo Chương 5–6; Mức 2 (thu tiền, pha chế) nếu còn thời gian.

## Quy tắc làm việc
- Không dùng EnsureCreated, drop/reset DB, hay hạ migration; thay đổi mô hình = migration mới, đọc Up/Down trước.
- Migration/seed chỉ chạy qua `dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed` (tài khoản mẫu: thêm `--seed-accounts`, mật khẩu ngẫu nhiên in một lần) với ASPNETCORE_ENVIRONMENT=Development, DB CafeManager_Group6_Dev.
- Không commit bí mật; connection string dùng Windows Auth/User Secrets.
- Không dùng SQLite/InMemory làm bằng chứng; không báo "đạt" khi test bị Skipped.
- Không đưa ICafeDataCommands ra API công khai trước khi có quyền, validation, snapshot giá từ máy chủ.
- Tài liệu và UI bằng tiếng Việt.
