# Nhiệm vụ của A – Ngô Nguyên: máy chủ, API, bảo mật

Hạn: thứ 7 14/11/2026 (đóng băng chức năng thứ 6 18:00). Xem phạm vi chung ở [ke-hoach-den-thu-7.md](../ke-hoach-den-thu-7.md). Bạn là **đường găng**: B và C cần API của bạn, nên giao sớm và đúng hợp đồng.

## Bạn sở hữu
`Core/`, `Infrastructure/` (kể cả migration), `Web/Program.cs`, `Web/Controllers/Api/`, `Web/Services/`, `tests/`. Chỉ bạn tạo migration và sửa `Program.cs`; B và C cần đổi gì thì nhắn bạn.

## Việc phải làm

### T3 – 10/11 (tối nay)
- [ ] Đẩy `main` lên GitHub (`origin/main` đang "gone"), mời B và C, tạo nhánh `feat/api`, `feat/web`, `feat/desktop`.
- [ ] Họp 30 phút với B, C: chốt phạm vi Mức 1 và **hợp đồng API** (bản nháp ở cuối file); sau đó mọi thay đổi hợp đồng phải báo cả nhóm.
- [x] Đưa hợp đồng vào `Core/ApiContracts.cs` dưới dạng DTO (record) để B và C dùng chung (đã có; hằng số quyền `Permissions`, mã lỗi `ApiErrorCodes`).
- [x] Lệnh dev tạo dữ liệu mẫu (`--seed-accounts`, có test): vai trò **Manager**, **Cashier**; quyền `MENU_MANAGE`, `ORDER_RECEIVE`; một tài khoản nhân viên mỗi vai trò và một khách. Mật khẩu **ngẫu nhiên, in ra một lần**, không commit, không cố định (quy tắc dự án). Chỉ chạy được ở môi trường Development, đích CafeManager_Group6_Dev.
- [ ] Xong khi: B và C chạy được `dotnet build`, `dotnet test`, `--migrate --seed` trên máy mình.

### T4 – 11/11: đăng nhập và đọc thực đơn
- [ ] UC19: `POST /api/auth/login`, `logout`, `me`; cookie xác thực (Secure, HttpOnly, SameSite) cho Web và Desktop; trả loại người dùng (Staff/Customer) và danh sách quyền.
- [ ] Chính sách phân quyền dùng `StaffPermissionReader`: thiếu quyền → 403, chưa đăng nhập → 401 (JSON, không trả HTML đăng nhập cho Desktop).
- [ ] Antiforgery cho các API ghi dùng cookie; ghi rõ cách Desktop lấy token (header) trong hợp đồng.
- [ ] UC18: `POST /api/auth/register-customer` tạo `IdentityUser` + `CustomerProfile` **trong một transaction**; tên trùng hoặc dữ liệu sai thì không tạo gì.
- [ ] `GET /api/menu` (công khai): món đang bán, giá hiện hành từ `PriceHistory`, biến thể, tùy chọn (`AllowedAddon`, `MaxPerUnit`), có `categoryId` và `q`.
- [ ] Xong khi: có test tích hợp cho login đúng/sai, tài khoản khóa, thiếu quyền (KT01–KT03, KT15, KT17, KT21) và B, C gọi được `/api/menu` và `/api/auth/login`.

### T5 – 12/11: đặt đơn và tiếp nhận (phần khó nhất)
- [ ] UC02 `POST /api/orders`: máy chủ **tự tính giá** từ `PriceHistory`, không tin giá client; tạo `SalesOrder` (Web, Pending, `OrderCode`, `PickupCode`, `ExpectedPickupAt`, liên hệ), `OrderRound` 1 (Draft), `OrderLine` và `OrderLineAddon` có ảnh chụp tên/giá. Nếu giá khác `expectedUnitPrice` của khách → 409 `PRICE_CHANGED` kèm giá mới. Chống gửi trùng bằng `RequestId` (dùng cơ chế `IdempotencyRecord`; `BeginRequestAsync` đang private, hãy tách dùng chung).
- [ ] UC04: `GET /api/orders/mine`, `GET /api/orders/{id}` (đơn người khác → 404), `POST /api/orders/{id}/cancel` (chỉ khi Pending, kiểm tra chủ sở hữu và trạng thái, 409 nếu đã tiếp nhận).
- [ ] UC03: `GET /api/staff/orders?status=Pending`; `POST /api/staff/orders/{id}/accept` gọi `AcceptRoundAsync` (đã chuyển đơn sang Serving); `POST /api/staff/orders/{id}/reject` ghi `Status=Rejected`, `RejectReason`, `RejectedBy`, `RejectedAt` (CK_SalesOrder_3 đòi cả ba cột).
- [ ] Ánh xạ lỗi: `InvalidOperationException` từ `CafeDataCommands` → 409 với `code` rõ (ví dụ `INSUFFICIENT_STOCK`), không để lộ chi tiết nội bộ.
- [ ] Xong khi: test tích hợp cho giá đổi, gửi trùng cùng mã, hủy khi đã tiếp nhận, khách B xem đơn khách A (KT07, KT08, KT10, KT12, KT14, KT16).

### T6 – 13/11: quản lý món và đóng băng
- [ ] UC17 API (quyền `MENU_MANAGE`): thêm/sửa món, biến thể, **giá mới theo khoảng hiệu lực**, ngừng bán, công thức. Quy tắc: món/biến thể mới để `IsActive = false` cho đến khi **có giá và công thức** (vì `AcceptRound` đòi công thức hiệu lực); chống chồng khoảng đã có trigger, hãy bắt lỗi 51001 → 409.
- [ ] Giao API UC17 cho C **trước thứ 5 chiều** nếu có thể (C làm màn D06 ở T5).
- [ ] Sửa lỗi tích hợp với B, C; **đóng băng chức năng 18:00**.
- [ ] Viết báo cáo: 5.1.2, 5.1.3, 5.1.4 (phần API và bảo mật), 5.2.1.

### T7 – 14/11
- [ ] Chạy lại `dotnet build`, toàn bộ test, `has-pending-model-changes`; cập nhật `docs/verification.md` (số test, hash, giờ TRX).
- [ ] Cập nhật dòng trạng thái ở tài liệu 01–06 và `docs/business-rules.md` theo kết quả thực.
- [ ] Ghép báo cáo: gộp phần của B, C, xóa các đoạn `[Nhóm điền: ...]`, cập nhật mục lục (Ctrl+A rồi F9), đọc soát chính tả.
- [ ] Chuẩn bị bản chạy demo (cơ sở dữ liệu sạch, tài khoản mẫu, kịch bản).

### Mức 2 (chỉ khi Mức 1 xong trước thứ 5 tối)
`POST /api/staff/orders/{id}/payments` (UC08, gọi `RecordCashPaymentAsync`, cần mở ca), `POST /api/staff/units/{id}/start` (UC07, `StartUnitAsync`), mở/đóng ca.

## Quy tắc cần nhớ
- Không dùng EnsureCreated, không xóa/đặt lại DB, không hạ migration; thay đổi mô hình = migration mới, đọc `Up`/`Down` trước (`CLAUDE.md`).
- Không đưa `ICafeDataCommands` ra API thô: luôn qua lớp dịch vụ có kiểm tra quyền, validation, giá từ máy chủ.
- Thời gian lưu UTC; không ghi mật khẩu, token vào log.
- Không báo "đạt" khi test bị Skipped; test chạy với `CAFE_TEST_SQLSERVER`.

## Cần nộp (bằng chứng)
Mã + test cho mỗi API ở trên, `TestResults/*.trx`, bản cập nhật `docs/verification.md`, các mục báo cáo 5.1.2–5.1.4, 5.2.1.

## Hợp đồng API nháp (chốt tối nay, JSON, tiền tệ VNĐ, thời gian UTC)

| Phương thức, đường dẫn | Ai gọi / quyền | Đầu vào chính | Kết quả |
|---|---|---|---|
| `POST /api/auth/login` | mọi người | `userName`, `password` | 200 + cookie, `{userKind, displayName, permissions[]}`; 401 |
| `POST /api/auth/logout`, `GET /api/auth/me` | đã đăng nhập | — | 204 / thông tin phiên |
| `POST /api/auth/register-customer` | khách | `userName`, `password`, `fullName`, `phone` | 201; 400/409 (trùng) |
| `GET /api/menu?categoryId=&q=` | công khai | — | món đang bán, biến thể, giá hiện hành, tùy chọn + `maxPerUnit` |
| `POST /api/orders` | khách | `requestId`, `pickupAt`, `contactName?`, `contactPhone?`, `lines[{variantId, quantity, addons[{addonVariantId, quantity}], expectedUnitPrice}]` | 201 `{orderId, orderCode, pickupCode, status, total}`; 409 `PRICE_CHANGED` |
| `GET /api/orders/mine`, `GET /api/orders/{id}` | khách (chủ đơn) | — | danh sách / chi tiết; đơn người khác → 404 |
| `POST /api/orders/{id}/cancel` | khách (chủ đơn) | — | 204; 409 nếu không còn Pending |
| `GET /api/staff/orders?status=Pending` | `ORDER_RECEIVE` | — | đơn chờ kèm dòng món, liên hệ, giờ nhận |
| `POST /api/staff/orders/{id}/accept` | `ORDER_RECEIVE` | — | 200 `{ticketId}`; 409 `INSUFFICIENT_STOCK` |
| `POST /api/staff/orders/{id}/reject` | `ORDER_RECEIVE` | `reason` | 204; 409 |
| `GET /api/staff/menu` | `MENU_MANAGE` | — | toàn bộ món kể cả đang ngừng |
| `POST/PUT /api/staff/products`, `/api/staff/variants` | `MENU_MANAGE` | tên, danh mục, loại (Drink/Addon) | 201/200 |
| `POST /api/staff/variants/{id}/price` | `MENU_MANAGE` | `unitPrice`, `effectiveFrom` | 204; 409 chồng khoảng |
| `PUT /api/staff/variants/{id}/recipe` | `MENU_MANAGE` | `lines[{ingredientId, quantity}]`, `effectiveFrom` | 204 |
| `POST /api/staff/variants/{id}/deactivate` | `MENU_MANAGE` | — | 204 |

Lỗi luôn có dạng `{code, message}`; mã trạng thái 400 (đầu vào sai), 401, 403, 404, 409 (xung đột trạng thái hoặc phiên bản).
