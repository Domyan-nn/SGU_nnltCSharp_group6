# Báo cáo doanh thu (B4, UC15)

Màn hình Desktop **Báo cáo doanh thu** dành cho tài khoản có quyền `MENU_MANAGE` (vai trò Manager, ví dụ `dev.manager`).
Nút nằm ở màn hình chính sau khi đăng nhập. Chỉ đọc, không ghi CSDL.

## Cách tính (máy chủ tính, Desktop chỉ hiển thị)

- **Đã thu** = tổng `Payment` có `Status = Succeeded`, tính theo `PaidAt`.
- **Đã hoàn** = tổng `RefundTransaction` có `Status = Succeeded`, tính theo `ExecutedAt`.
- **Doanh thu thuần** = Đã thu − Đã hoàn. Mỗi ngày trong kỳ đều có một dòng, kể cả ngày không bán.
- **Ngày** tính theo giờ Việt Nam (UTC+7): 23:59 ngày 18 và 00:01 ngày 19 rơi vào hai ngày khác nhau.
- **Số đơn** = số đơn khác nhau có thu tiền trong ngày.
- **Món bán chạy** = các dòng của lượt `Accepted` thuộc những đơn có thu tiền trong kỳ (mỗi đơn tính một lần), xếp theo số ly rồi doanh thu; doanh thu mỗi dòng gồm cả tùy chọn thêm. Chưa trừ phần hoàn theo từng món.

## API

`GET /api/staff/reports/revenue?from=yyyy-MM-dd&to=yyyy-MM-dd&top=10` (quyền `MENU_MANAGE`).
Bỏ trống `from`/`to` thì lấy 7 ngày gần nhất. Tối đa 366 ngày, `top` từ 1 đến 50; sai thì trả 400 `VALIDATION`.

## Xuất CSV

Tệp UTF-8 có BOM, phân cách bằng `;`, dòng đầu `sep=;` để Excel nhận đúng dấu phân cách. Tên món bắt đầu bằng `=`, `+`, `-`, `@` được thêm dấu `'` để Excel không chạy thành công thức.

## Kiểm thử

- Tự động (không cần SQL Server): `tests/CafeManager.Tests/ReportTests.cs`.
- Chạy tay: đăng nhập Desktop bằng `dev.manager` → Báo cáo doanh thu → chọn kỳ có đơn đã thu tiền → so tổng với màn hình Thu ngân và truy vấn `Payment` trong CSDL; bấm Xuất CSV và mở bằng Excel.
