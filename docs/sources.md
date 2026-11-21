# Nguồn và hiện trạng

Cập nhật 21/11/2026. Các tài liệu dưới đây là Google Docs của nhóm (thư mục Drive: https://drive.google.com/drive/folders/1Wj-u4_8ctqh_lf5BonmDJDKznHSdjb35). Chúng được viết theo hiện trạng của mã nguồn ngày 21/11/2026; khi tài liệu và mã khác nhau thì mã và kết quả kiểm thử là căn cứ.

| Nguồn | Nội dung dùng làm căn cứ |
|---|---|
| [Báo cáo chính](https://docs.google.com/document/d/11tUJay5OTaEVv72ig_1zjuEE8zn3qwSqCg_1pJslEyw/edit) | Sáu chương: tổng quan, khảo sát, phân tích, thiết kế, triển khai và kiểm thử, kết luận; nhật ký thay đổi |
| [01 SRS](https://docs.google.com/document/d/1YtjebMwbdgcaZbt_oijQsjG6IMg_HLPtzPRbRfdLkSI/edit) | FR01–15, BR01–12, NFR và giới hạn tích hợp |
| [02 Nghiệp vụ và BFD](https://docs.google.com/document/d/19jxJrSCITIW0edpDOo275MbChVqql_fs-avOv1IVCq8/edit) | Quy trình Web và bàn, bảng trạng thái đơn, pha chế, thanh toán, bàn; khảo sát KiotViet và Sapo từ tài liệu công khai |
| [03 Use case và User story](https://docs.google.com/document/d/1nYwmBUwfdJlvB2zFz4FpaxsaSXVe_HTWiIwW0M4JwKc/edit) | UC01–19; đã làm 12 use case |
| [04 Dữ liệu và ERD](https://docs.google.com/document/d/1uuXSh42pKnIfoZ6EhIwnfEdIHmVvBR5K0dSnoZtFUYk/edit) | Danh mục 40 quan hệ, toàn bộ trường, kiểu, null, khóa; phụ thuộc hàm và 3NF |
| [05 Phân tích giao diện](https://docs.google.com/document/d/1iB7O0TakWOphlEFnFILMBw_0R8eeZVijjhokkrH6TDo/edit) | Màn hình W00–W03, D00–D08, bố cục và quy tắc phản hồi lỗi |
| [06 Truy vết, kiểm thử và nghiệm thu](https://docs.google.com/document/d/1S89wwGqxfqeifagS32DC9ONB2MCTXnTB9xW_UhF04O8/edit) | Bảng kiểm thử 29 ca (KT, LT01), cùng nội dung với Bảng 1 của [kiem-thu.md](kiem-thu.md) |
| [Đề tài giảng viên](https://drive.google.com/file/d/1mYhMI5PQq9qPcIE_zcNTrY3SCa_AQub2/view) | Đề tài 11; Desktop C# Windows Forms, Web C#, SQL Server dùng chung; EF, API, phân lớp được khuyến khích |

Mã nguồn: https://github.com/Domyan-nn/doan_CSharp (nhánh `main` được bảo vệ, thay đổi đi qua pull request).

Tham khảo kỹ thuật chính thức:

- [IdentityUserContext cho cấu hình không dùng Identity roles](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0).
- [EF SQL Server và UseSqlOutputClause(false) khi có trigger](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/misc).
- [Gói EF SQL Server 10.0.14](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.14).
- [System.Security.Cryptography.Pkcs 10.0.14](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.14): pin trực tiếp để thay phụ thuộc 9.0.11 bị NuGet audit cảnh báo NU1902/NU1903; không tắt audit hoặc hạ cảnh báo thành công giả.

