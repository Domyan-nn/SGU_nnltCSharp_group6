# Hợp đồng API giữa máy chủ, Web và Desktop

Bảng đường dẫn, quyền, dữ liệu gửi và nhận, mã lỗi của API. DTO nằm trong `src/CafeManager.Core/ApiContracts.cs`, hàm gọi phía Desktop nằm trong `src/CafeManager.Client/StaffApi.cs`. Nội dung chuyển nguyên văn từ cuối file nhiệm vụ cũ của A (đã xóa ngày 19/11); đổi hợp đồng sau khi nhóm đã chốt thì phải báo cả nhóm.

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
