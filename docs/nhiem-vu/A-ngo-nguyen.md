# Nhiệm vụ của A – Ngô Nguyên: máy chủ, API, bảo mật

Hạn: thứ 7 14/11/2026 (đóng băng chức năng thứ 6 18:00). Xem phạm vi chung ở [ke-hoach-den-thu-7.md](../ke-hoach-den-thu-7.md). Bạn là **đường găng**: B và C cần API của bạn, nên giao sớm và đúng hợp đồng.

## Bạn sở hữu
`Core/`, `Infrastructure/` (kể cả migration), `Web/Program.cs`, `Web/Controllers/Api/`, `Web/Services/`, `tests/`. Chỉ bạn tạo migration và sửa `Program.cs`; B và C cần đổi gì thì nhắn bạn.

## Việc phải làm

### T3 – 10/11 (tối nay)
- [x] Đẩy `main` lên GitHub (`origin/main` đang "gone"), mời B và C, tạo nhánh `feat/api`, `feat/web`, `feat/desktop`.
- [ ] Họp 30 phút với B, C: chốt phạm vi Mức 1 và **hợp đồng API** (bản nháp ở cuối file); sau đó mọi thay đổi hợp đồng phải báo cả nhóm.
- [x] Đưa hợp đồng vào `Core/ApiContracts.cs` dưới dạng DTO (record) để B và C dùng chung (đã có; hằng số quyền `Permissions`, mã lỗi `ApiErrorCodes`).
- [x] Lệnh dev tạo dữ liệu mẫu (`--seed-accounts`, có test): vai trò **Manager**, **Cashier**; quyền `MENU_MANAGE`, `ORDER_RECEIVE`; một tài khoản nhân viên mỗi vai trò và một khách. Mật khẩu **ngẫu nhiên, in ra một lần**, không commit, không cố định (quy tắc dự án). Chỉ chạy được ở môi trường Development, đích CafeManager_Group6_Dev.
- [ ] Xong khi: B và C chạy được `dotnet build`, `dotnet test`, `--migrate --seed` trên máy mình.

### T4 – 11/11: đăng nhập và đọc thực đơn
- [x] UC19: `POST /api/auth/login`, `logout`, `me`; cookie xác thực (Secure, HttpOnly, SameSite) cho Web và Desktop; trả loại người dùng (Staff/Customer) và danh sách quyền.
- [x] Chính sách phân quyền dùng `StaffPermissionReader`: thiếu quyền → 403, chưa đăng nhập → 401 (JSON, không trả HTML đăng nhập cho Desktop).
- [x] Antiforgery cho các API ghi dùng cookie; ghi rõ cách Desktop lấy token (header) trong hợp đồng.
- [x] UC18: `POST /api/auth/register-customer` tạo `IdentityUser` + `CustomerProfile` **trong một transaction**; tên trùng hoặc dữ liệu sai thì không tạo gì.
- [x] `GET /api/menu` (công khai): món đang bán, giá hiện hành từ `PriceHistory`, biến thể, tùy chọn (`AllowedAddon`, `MaxPerUnit`), có `categoryId` và `q`.
- [x] Xong khi: có test tích hợp cho login đúng/sai, tài khoản khóa, thiếu quyền (KT01–KT03, KT15, KT17, KT21) và B, C gọi được `/api/menu` và `/api/auth/login`.

### T5 – 12/11: đặt đơn và tiếp nhận (phần khó nhất)
- [x] UC02 `POST /api/orders`: máy chủ **tự tính giá** từ `PriceHistory`, không tin giá client; tạo `SalesOrder` (Web, Pending, `OrderCode`, `PickupCode`, `ExpectedPickupAt`, liên hệ), `OrderRound` 1 (Draft), `OrderLine` và `OrderLineAddon` có ảnh chụp tên/giá. Nếu giá khác `expectedUnitPrice` của khách → 409 `PRICE_CHANGED` kèm giá mới. Chống gửi trùng bằng `RequestId` (dùng cơ chế `IdempotencyRecord`; `BeginRequestAsync` đang private, hãy tách dùng chung).
- [x] UC04: `GET /api/orders/mine`, `GET /api/orders/{id}` (đơn người khác → 404), `POST /api/orders/{id}/cancel` (chỉ khi Pending, kiểm tra chủ sở hữu và trạng thái, 409 nếu đã tiếp nhận).
- [x] UC03: `GET /api/staff/orders?status=Pending`; `POST /api/staff/orders/{id}/accept` gọi `AcceptRoundAsync` (đã chuyển đơn sang Serving); `POST /api/staff/orders/{id}/reject` ghi `Status=Rejected`, `RejectReason`, `RejectedBy`, `RejectedAt` (CK_SalesOrder_3 đòi cả ba cột).
- [x] Ánh xạ lỗi: `InvalidOperationException` từ `CafeDataCommands` → 409 với `code` rõ (ví dụ `INSUFFICIENT_STOCK`), không để lộ chi tiết nội bộ.
- [x] Xong khi: test tích hợp cho giá đổi, gửi trùng cùng mã, hủy khi đã tiếp nhận, khách B xem đơn khách A (KT07, KT08, KT10, KT12, KT14, KT16).

### T6 – 13/11: quản lý món và đóng băng
- [x] UC17 API (quyền `MENU_MANAGE`): thêm/sửa món, biến thể, **giá mới theo khoảng hiệu lực**, ngừng bán, công thức. Quy tắc: món/biến thể mới để `IsActive = false` cho đến khi **có giá và công thức** (vì `AcceptRound` đòi công thức hiệu lực); chống chồng khoảng đã có trigger, hãy bắt lỗi 51001 → 409.
- [x] Giao API UC17 cho C **trước thứ 5 chiều** nếu có thể (C làm màn D06 ở T5).
- [ ] Sửa lỗi tích hợp với B, C; **đóng băng chức năng 18:00**.
- [x] Viết báo cáo: 5.1.2, 5.1.3, 5.1.4 (phần API và bảo mật), 5.2.1 (số liệu 5.2.1 cập nhật 15/11/2026: 105/105).

### T7 – 14/11
- [ ] Chạy lại `dotnet build`, toàn bộ test, `has-pending-model-changes`; cập nhật `docs/verification.md` (số test, hash, giờ TRX).
- [ ] Cập nhật dòng trạng thái ở tài liệu 01–06 và `docs/business-rules.md` theo kết quả thực (tài liệu 01–06 đã cập nhật 15/11/2026; `business-rules.md` chưa).
- [ ] Ghép báo cáo: đã gộp phần của B, C và xóa các đoạn `[Nhóm điền: ...]` (15/11/2026); còn cập nhật mục lục trong Word (mở tệp, chọn Yes khi hỏi cập nhật trường, hoặc Ctrl+A rồi F9) và đọc soát chính tả.
- [ ] Chuẩn bị bản chạy demo (cơ sở dữ liệu sạch, tài khoản mẫu, kịch bản).

### Mức 2 (chỉ khi Mức 1 xong trước thứ 5 tối)
Thu tiền (UC08, `RecordCashPaymentAsync`, cần mở ca), pha chế (UC07, `StartUnitAsync`), mở/đóng ca: đường dẫn đã chốt lại ở mục "Hợp đồng bổ sung 16/11" cuối file.

## Quy tắc cần nhớ
- Không dùng EnsureCreated, không xóa/đặt lại DB, không hạ migration; thay đổi mô hình = migration mới, đọc `Up`/`Down` trước (`docs/quy-uoc-du-an.md`).
- Không đưa `ICafeDataCommands` ra API thô: luôn qua lớp dịch vụ có kiểm tra quyền, validation, giá từ máy chủ.
- Thời gian lưu UTC; không ghi mật khẩu, token vào log.
- Không báo "đạt" khi test bị Skipped; test chạy với `CAFE_TEST_SQLSERVER`.

## Cần nộp (bằng chứng)
Mã + test cho mỗi API ở trên, `TestResults/*.trx`, bản cập nhật `docs/verification.md`, các mục báo cáo 5.1.2–5.1.4, 5.2.1.

## Hợp đồng API (đã hiện thực 12/11; JSON, tiền tệ VNĐ, thời gian UTC)

**Antiforgery (đã chốt):** `login`, `logout`, `register-customer` không đòi token (chưa có phiên để bị giả mạo). Mọi yêu cầu ghi khác dùng cookie phải kèm header `X-CSRF-TOKEN`; token lấy ở `GET /api/auth/antiforgery` **sau khi đăng nhập** (token gắn với danh tính). `ApiClient` của Desktop tự lấy token sau khi đăng nhập nhân viên thành công. Thiếu hoặc sai token trả 400. Mã `ACCOUNT_LOCKED` (HTTP 403) dùng cho tài khoản bị khóa do nhập sai 5 lần (15 phút) hoặc nhân viên bị vô hiệu hóa; quyền được kiểm tra lại trong CSDL ở mỗi yêu cầu nên thu hồi quyền có hiệu lực ở yêu cầu kế tiếp.


| Phương thức, đường dẫn | Ai gọi / quyền | Đầu vào chính | Kết quả |
|---|---|---|---|
| `POST /api/auth/login` | mọi người | `userName`, `password` | 200 + cookie, `{userKind, displayName, permissions[]}`; 401 |
| `POST /api/auth/logout`, `GET /api/auth/me` | đã đăng nhập | — | 204 / thông tin phiên |
| `GET /api/auth/antiforgery` | mọi người (gọi sau đăng nhập) | — | `{token}` để gửi qua header `X-CSRF-TOKEN` |
| `POST /api/auth/register-customer` | khách | `userName`, `password`, `fullName`, `phone` | 201; 400/409 (trùng) |
| `GET /api/menu?categoryId=&q=` | công khai | — | món đang bán, biến thể, giá hiện hành, tùy chọn + `maxPerUnit` |
| `POST /api/orders` | khách | `requestId`, `pickupAt`, `contactName?`, `contactPhone?`, `lines[{variantId, quantity, addons[{addonVariantId, quantity}], expectedUnitPrice}]` | 201 `{orderId, orderCode, pickupCode, status, total}`; 409 `PRICE_CHANGED` |
| `GET /api/orders/mine`, `GET /api/orders/{id}` | khách (chủ đơn) | — | danh sách / chi tiết; đơn người khác → 404 |
| `POST /api/orders/{id}/cancel` | khách (chủ đơn) | — | 204; 409 nếu không còn Pending |
| `GET /api/staff/orders?status=Pending` | `ORDER_RECEIVE` | — | đơn chờ kèm dòng món, liên hệ, giờ nhận |
| `POST /api/staff/orders/{id}/accept` | `ORDER_RECEIVE` | — | 200 `{ticketId}`; 409 `INSUFFICIENT_STOCK` |
| `POST /api/staff/orders/{id}/reject` | `ORDER_RECEIVE` | `reason` | 204; 409 |
| `GET /api/staff/menu` | `MENU_MANAGE` | — | toàn bộ món kể cả đang ngừng |
| `POST/PUT /api/staff/products`, `/api/staff/variants` | `MENU_MANAGE` | tên, danh mục, loại (Drink/Addon) | 201/200 kèm `{id}`; biến thể mới luôn chưa bán, chỉ bật bán khi đã có giá và công thức hiệu lực |
| `POST /api/staff/variants/{id}/price` | `MENU_MANAGE` | `unitPrice`, `effectiveFrom` | 204; 409 chồng khoảng |
| `PUT /api/staff/variants/{id}/recipe` | `MENU_MANAGE` | `lines[{ingredientId, quantity}]`, `effectiveFrom` | 204 |
| `POST /api/staff/variants/{id}/deactivate` | `MENU_MANAGE` | — | 204 |

Lỗi luôn có dạng `{code, message}`; mã trạng thái 400 (đầu vào sai), 401, 403, 404, 409 (xung đột trạng thái hoặc phiên bản).

## Hợp đồng bổ sung 16/11 cho A1–A3 (máy chủ đã hiện thực 17/11)

DTO nằm trong `Core/ApiContracts.cs`, hàm gọi nằm trong `Client/StaffApi.cs`; test `StaffApiContractTests` khóa đường dẫn. Máy chủ hiện thực ở `Infrastructure/Services/StaffCatalogService.cs` (A1), `StaffCashierService.cs` (A2), `StaffPreparationService.cs` (A3) và `Web/Controllers/Api/StaffController.cs`; kiểm thử ở `StaffA123ApiTests` (HTTP) và `StaffApiClientA123Tests` (đúng lớp `StaffApi` của Desktop). Tài khoản mẫu chạy lại `--seed-accounts` để có quyền mới: `dev.cashier` thêm `PAYMENT_COLLECT`, tài khoản mới `dev.barista` có `PREPARATION_WORK`; quầy mẫu `DEMO-C01` có sau `--seed`.

Quy tắc máy chủ đã áp dụng (C cần biết để hiện thông báo đúng):
- **A1:** tên danh mục và tên nguyên liệu không trùng (409 `CONFLICT`, không phân biệt hoa thường); xóa chỉ khi chưa có tham chiếu: danh mục còn món, nguyên liệu nằm trong công thức hoặc đã có phát sinh kho (nhập/xuất, lô, kiểm kê, giữ cho ly) hoặc còn tồn đều trả 409, muốn ẩn thì đặt `isActive = false`. Nguyên liệu mới có sẵn dòng tồn bằng 0.
- **A2:** mỗi nhân viên và mỗi quầy một ca mở (409 `CONFLICT`); `summary` và `close` chỉ dùng cho ca của chính mình (ca người khác trả 404). Thu tiền cần đang có ca mở (409 `INVALID_STATE` nếu chưa mở), chỉ thu đơn đã tiếp nhận còn nợ; **số ghi sổ là số còn phải thu**, tiền khách đưa dư thành `change`, đưa thiếu trả 400 `VALIDATION`. Gửi lại cùng `requestId` và cùng nội dung trả đúng phiếu cũ (kể cả khi ca đã đóng), khác nội dung trả 409 `CONFLICT`. Hai người thu cùng một đơn được xếp hàng nên không thu vượt. Hoàn tiền chưa có trong hợp đồng, nên `paid` chưa trừ hoàn.
- **A3:** mỗi ly đi một nấc `Waiting → Preparing → Ready → Delivered`; sai thứ tự hoặc lặp lại trả 409 `INVALID_STATE`, hai người bắt đầu cùng một ly thì một người thành công và người kia nhận 409. Bắt đầu pha trừ nguyên liệu của đúng ly đó; giao ly ghi người giao và giờ giao. Trạng thái phiếu suy ra từ các ly (`Waiting → Preparing → Ready → Completed`).
- **Tiến độ đơn (B2):** `progress` chỉ có khi đơn đã được tiếp nhận (đã có ly); đơn chờ, bị từ chối hoặc đã hủy vẫn trả `null`. `paid` là tổng các lần thu thành công.
- Máy chủ đang bận (bế tắc hoặc hết hạn khóa) trả 409 `CONFLICT` với thông báo thử lại sau giây lát.

| Phương thức, đường dẫn | Quyền | Đầu vào chính | Kết quả | Hàm trong `StaffApi` |
|---|---|---|---|---|
| `GET /api/staff/categories?q=` | `MENU_MANAGE` | — | `[{categoryId, categoryName, isActive, productCount}]` | `CategoriesAsync` |
| `POST /api/staff/categories`, `PUT /api/staff/categories/{id}` | `MENU_MANAGE` | `categoryName`, `isActive` | 201/200 `{id}`; 400; 409 trùng tên | `SaveCategoryAsync` |
| `DELETE /api/staff/categories/{id}` | `MENU_MANAGE` | — | 204; 404; 409 `CONFLICT` khi danh mục còn món | `DeleteCategoryAsync` |
| `GET /api/staff/ingredients?q=` | `MENU_MANAGE` | — | `[{ingredientId, ingredientName, unitName, isActive, quantityOnHand, quantityReserved, recipeCount}]` | `IngredientsAsync` |
| `POST /api/staff/ingredients`, `PUT /api/staff/ingredients/{id}` | `MENU_MANAGE` | `ingredientName`, `unitName`, `isActive` | 201/200 `{id}`; 400; 409 | `SaveIngredientAsync` |
| `DELETE /api/staff/ingredients/{id}` | `MENU_MANAGE` | — | 204; 404; 409 khi đang nằm trong công thức hoặc đã có phát sinh kho | `DeleteIngredientAsync` |
| `GET /api/staff/counters` | `PAYMENT_COLLECT` | — | `[{counterId, counterCode}]` quầy đang hoạt động | `CountersAsync` |
| `GET /api/staff/shifts/current` | `PAYMENT_COLLECT` | — | 200 `{shift}`; `shift` = null khi chưa mở ca | `CurrentShiftAsync` |
| `POST /api/staff/shifts` | `PAYMENT_COLLECT` | `counterId`, `openingCash` | 201 `ShiftDto`; 409 khi quầy hoặc nhân viên đã có ca mở | `OpenShiftAsync` |
| `GET /api/staff/shifts/{id}/summary` | `PAYMENT_COLLECT` | — | `{openingCash, cashPayments, cashRefunds, otherIn, otherOut, expectedCash, countedCash, difference}` | `ShiftSummaryAsync` |
| `POST /api/staff/shifts/{id}/close` | `PAYMENT_COLLECT` | `countedCash`, `closeNote?` | 200 bảng đối soát có chênh lệch; 409 `INVALID_STATE` khi ca đã đóng | `CloseShiftAsync` |
| `GET /api/staff/payments/payable` | `PAYMENT_COLLECT` | — | đơn đã tiếp nhận còn phải thu: `[{orderId, orderCode, contactName, pickupCode, status, total, paid, remaining}]` | `PayableOrdersAsync` |
| `POST /api/staff/payments/cash` | `PAYMENT_COLLECT` | `requestId`, `orderId`, `amountReceived` | 201 `{paymentId, amountDue, amountReceived, change}`; 400 khi tiền đưa thiếu; 409 khi chưa mở ca hoặc đơn không còn nợ. Gửi lại cùng `requestId` trả lại phiếu cũ | `PayCashAsync` |
| `GET /api/staff/preparation` | `PREPARATION_WORK` | — | các ly chưa giao, cũ nhất trước: `[{unitId, ticketId, orderId, orderCode, pickupCode, productName, variantName, addons, unitNumber, lineQuantity, status, acceptedAt}]` | `PreparationQueueAsync` |
| `POST /api/staff/preparation/{unitId}/start` | `PREPARATION_WORK` | — | 204; 409 `INVALID_STATE` khi ly không còn `Waiting` | `StartUnitAsync` |
| `POST /api/staff/preparation/{unitId}/ready` | `PREPARATION_WORK` | — | 204; 409 khi ly không ở `Preparing` | `MarkReadyAsync` |
| `POST /api/staff/preparation/{unitId}/deliver` | `PREPARATION_WORK` | — | 204; 409 khi ly không ở `Ready` | `DeliverAsync` |

Web khách (B2): `OrderDetailDto` có thêm `progress` = `{waiting, preparing, ready, delivered, paid}`; trường này là `null` khi đơn chưa được tiếp nhận (chưa có ly), giao diện phải chịu được `null`.
