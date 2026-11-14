# Kế hoạch 3 người đến thứ 7 (14/11/2026)

Lập ngày 10/11/2026 (thứ 3). Giả định: hạn là **thứ 7 14/11/2026**; thứ 6 tối đóng băng chức năng, thứ 7 chỉ sửa lỗi, hoàn thiện báo cáo và tập demo. Nếu hạn sớm hơn, giữ nguyên Mức 1 và cắt Mức 2.

## Hiện trạng cập nhật 14/11/2026
- Xong: API (UC17/18/19/01/02/03/04), Web khách W00–W03 (B), Desktop D00, D02, D06; 105/105 test đạt; điều khiển tự động form D02/D06 trên máy chủ thật 22/22 (xem [verification.md](verification.md)).
- Còn lại: kiểm thử tay có biên bản và ảnh ([kiem-thu-tay.md](kiem-thu-tay.md)), báo cáo Chương 5–6 do nhóm tự viết, slide và kịch bản demo; HTTPS tin cậy trên từng máy (`dotnet dev-certs https --trust`).

## Hiện trạng ban đầu (đã kiểm tra 10/11/2026)
- Xong: CSDL 40 bảng + 4 Identity (4 migration), 5 thao tác dữ liệu (AcceptRound, StartUnit, CashPayment, CashRefund, ReconcileShift), 33/33 test đạt.
- Chưa có: Web (chỉ khung), Desktop (chỉ khung), API, đăng ký/đăng nhập, 19/19 ca sử dụng chưa hoàn chỉnh; Chương 5–6 mới là khung.
- Git: 2 commit cục bộ, `origin/main` chưa có trên GitHub (chưa đẩy). Tài liệu docx nằm ngoài repo (`bao-cao-da-sua/`).

## Phạm vi (cắt ba mức)
**Mức 1 – bắt buộc, để demo luồng liên thông (không đạt thì coi như chưa xong):**
Quản lý đăng nhập Desktop → thêm/sửa món (UC17) → khách đăng ký/đăng nhập Web (UC18/19) → xem, tìm món, giỏ (UC01) → gửi đơn (UC02) → xem/hủy đơn chờ (UC04) → thu ngân đăng nhập Desktop, xem đơn chờ, tiếp nhận/từ chối (UC03).

**Mức 2 – nếu Mức 1 xong trước thứ 5 tối:** UC08 thu tiền mặt (đã có `RecordCashPayment`), UC07 pha chế (đã có `StartUnit`), mở/đóng ca.

**Mức 3 – không làm, ghi vào Chương 6:** UC05, 06, 09–13, 15, 16, chuyển khoản, báo cáo, sao lưu/RPO/RTO, thử tải.

## Phân việc

Chi tiết từng người: [A – Ngô Nguyên](nhiem-vu/A-ngo-nguyen.md) · [B – Lê Nam Thuận](nhiem-vu/B-le-nam-thuan.md) · [C – Nguyễn Quốc An](nhiem-vu/C-nguyen-quoc-an.md).

| | A – Ngô Nguyên: máy chủ, API, bảo mật | B – Lê Nam Thuận: Web khách (MVC) | C – Nguyễn Quốc An: Desktop, kiểm thử hệ thống |
|---|---|---|---|
| **T3 10/11 (tối)** | Họp 30 phút chốt phạm vi và hợp đồng API; đẩy `main` lên GitHub, mời 2 bạn; tạo nhánh `feat/api`, `feat/web`, `feat/desktop`; viết `docs/api-contract.md` (DTO JSON); lệnh dev tạo tài khoản mẫu (mật khẩu ngẫu nhiên in một lần) | Cài môi trường, chạy được `dotnet build`, `dotnet test`, migrate Dev DB; dựng layout Web (menu, trang chủ) | Như B; dựng khung Desktop: lớp `ApiClient` (HttpClient + CookieContainer, địa chỉ cấu hình), form D00 |
| **T4 11/11** | UC19: đăng nhập cookie Web + Desktop, policy theo `StaffPermissionReader`, antiforgery; API đọc thực đơn/tìm kiếm | W01: thực đơn, danh mục, tìm kiếm, giỏ (session), 360 px | D00 đăng nhập thật; khung màn hình chính hiện chức năng theo quyền |
| **T5 12/11** | UC02: tạo đơn (tính giá từ PriceHistory phía máy chủ, ảnh chụp dòng, lượt nháp, RequestId chống trùng); UC04 danh sách/hủy đơn; UC03 danh sách chờ, tiếp nhận (gọi AcceptRound), từ chối (RejectReason...). Test tích hợp cho từng API | W00 đăng ký/đăng nhập khách; W02 xác nhận giá, giờ nhận, gửi đơn (xử lý "giá đổi") | D06 quản lý món: thêm, sửa, ngừng bán, đổi giá (gọi API UC17 của A, thứ 5 chiều A giao) |
| **T6 13/11** | UC17 API (CRUD món, giá); sửa lỗi tích hợp; **đóng băng chức năng 18:00**; viết 5.1.2, 5.1.3, 5.1.4 (phần API/bảo mật), 5.2.1 | W03 đơn của tôi + tự hủy; kiểm tra quyền sở hữu đơn; chạy tay KT06, KT07, KT10, KT14, KT16, ghi biên bản + ảnh; viết 5.1.4 (phần Web) | D02 danh sách đơn chờ, tiếp nhận/từ chối; chạy tay KT01–KT05, KT08, KT09, KT15; Mức 2 nếu còn thời gian |
| **T7 14/11** | Chạy lại build, 33+ test, lập bản chạy demo; cập nhật tài liệu 01–06 trạng thái; ghép báo cáo, cập nhật mục lục (Ctrl+A, F9), xóa các đoạn `[Nhóm điền]` | Sửa lỗi Web, chụp ảnh giao diện, điền bảng 5.4 phần Web, đọc soát chính tả | Quay/ghi kịch bản demo, slide; viết 5.1.5, 5.2.2 (Desktop, liên thông), 5.3, Chương 6; tập demo cả nhóm |

## Quy tắc làm việc chung
- **Sở hữu thư mục (tránh xung đột):** A: `Core/`, `Infrastructure/`, `Web/Controllers/Api/`, `Web/Services/`, `Web/Program.cs`, migration. B: `Web/Controllers/Web/`, `Web/Views/`, `Web/wwwroot/`. C: `Desktop/`. Cần sửa thư mục người khác thì nhắn người đó.
- **Hợp đồng API chốt tối nay, đổi sau đó phải báo cả nhóm.** B và C làm trước với dữ liệu giả theo hợp đồng, không đợi A.
- **Không dùng EnsureCreated, không xóa DB, thay đổi mô hình = migration mới** (xem `docs/quy-uoc-du-an.md`); chỉ A tạo migration.
- **Mỗi việc xong = mã + test hoặc biên bản chạy tay + ảnh.** Không ghi "Đạt" khi thiếu bằng chứng; kết quả điền vào Chương 5.
- **Họp 15 phút lúc 20:00 mỗi tối:** làm gì, vướng gì, ngày mai làm gì. Mốc cắt: **thứ 5 22:00** nếu Mức 1 chưa chạy được đầu-cuối thì cắt UC04 và Mức 2, dồn người sửa luồng chính.

## Rủi ro cần xử lý ngay trong tối nay
1. **Smart App Control của Windows chặn DLL vừa build** (đã gặp trên máy này, `0x800711C7`): mỗi người chạy thử `dotnet build` và `dotnet test` trước 21:00; nếu bị chặn cần tắt tính năng này (không bật lại được nếu không cài lại Windows) hoặc dùng máy khác.
2. **Chưa đẩy lên GitHub:** nếu không đẩy tối nay thì ba người không làm song song được.
3. **Không có tài khoản mẫu** (quy tắc dự án cấm mật khẩu cố định): A làm lệnh tạo tài khoản dev với mật khẩu ngẫu nhiên in một lần; không commit mật khẩu.
4. **Mỗi máy một LocalDB riêng:** mỗi người tự chạy `--migrate --seed` trên máy mình.
5. **Khối lượng lớn so với 5 ngày:** Mức 1 là mục tiêu thực tế; Mức 2 chỉ là phần thưởng.

## Bàn giao báo cáo (thứ 7)
Điền các đoạn `[Nhóm điền: ...]` ở Chương 5–6 theo phân việc trên, rồi cập nhật mục lục. Mỗi ca KT/TC chỉ ghi trạng thái theo bằng chứng thật (xem mẫu biên bản ở tài liệu 06, mục 6).
