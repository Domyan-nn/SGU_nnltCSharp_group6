# Nguồn và hiện trạng ban đầu

Đọc được cả sáu tài liệu qua Google Drive ngày 08/11/2026. Các tài liệu là thiết kế dự kiến, không phải bằng chứng mã nguồn/kiểm thử trước lần thực hiện này.

| Nguồn | Nội dung dùng làm căn cứ |
|---|---|
| [Báo cáo chính](https://drive.google.com/file/d/1nIvP3UzitzqKxCALbcIq1BvswWQ-TFMd/view) | Kiến trúc máy chủ MVC/API, Desktop qua HTTPS, một kho/một cửa hàng, trạng thái, snapshot, giao dịch và thu/hoàn |
| [01 SRS](https://drive.google.com/file/d/1-0vhSZUlfqidM-B42i5ke2a6OJccRvav/view) | FR01–15, BR01–12, NFR và giới hạn tích hợp |
| 02 Nghiệp vụ và BFD (bản cục bộ, chưa có liên kết Drive) | Quy trình Web/bàn, bảng trạng thái đơn–pha chế–thanh toán–bàn; khảo sát KiotViet/Sapo chỉ là tài liệu giới thiệu công khai |
| [03 UC/User story](https://drive.google.com/file/d/1oU07u-yvRPXo3mVORRiqqklEIkcS8UlG/view) | UC01–19; nháp chưa giữ kho; tiếp nhận, tiến độ từng ly, duyệt độc lập |
| [04 Dữ liệu/ERD](https://drive.google.com/file/d/1CSOTZQ2qE1chEOfFBXZuY74CYAjBLA9S/view) | Danh mục 40 quan hệ, mục 5.7 toàn bộ trường/kiểu/null/khóa; mục 6 FD và giới hạn kết luận 3NF |
| 05 Giao diện (bản cục bộ, chưa có liên kết Drive) | Danh mục màn hình W00–W03, D00–D08, bố cục và quy tắc phản hồi lỗi; chỉ là thiết kế, chưa có giao diện chạy |
| [06 Truy vết/kiểm thử](https://drive.google.com/file/d/1lFgfum4EUqiOsFB_gT9QcLrbYTjKmnzH/view) | KT01–22 và TC01–11 là kế hoạch; các ca dữ liệu triển khai không thay thế toàn bộ kế hoạch |
| [Đề tài giảng viên](https://drive.google.com/file/d/1mYhMI5PQq9qPcIE_zcNTrY3SCa_AQub2/view) | Đề tài 11; Desktop C# Windows Forms, Web C#, SQL Server dùng chung; EF/API/phân lớp được khuyến khích |

Repo có remote đúng https://github.com/Domyan-nn/doan_CSharp, tại thời điểm đầu (08/11/2026) nhánh main chưa có commit, chưa có solution/mã nguồn/tài liệu cục bộ; .vs/ chưa track được đưa vào gitignore. Cập nhật 10/11/2026: main có 2 commit cục bộ (08/11 và 10/11) nhưng origin/main chưa có trên GitHub (trạng thái "gone"), nghĩa là chưa đẩy lên.

Tham khảo kỹ thuật chính thức:

- [IdentityUserContext cho cấu hình không dùng Identity roles](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0).
- [EF SQL Server và UseSqlOutputClause(false) khi có trigger](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/misc).
- [Gói EF SQL Server 10.0.14](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.14).
- [System.Security.Cryptography.Pkcs 10.0.14](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.14): pin trực tiếp để thay phụ thuộc 9.0.11 bị NuGet audit cảnh báo NU1902/NU1903; không tắt audit hoặc hạ cảnh báo thành công giả.

