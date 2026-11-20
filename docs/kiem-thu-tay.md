# Hướng dẫn kiểm thử tay (nhóm thực hiện và ghi biên bản)

Cập nhật 14/11/2026; bổ sung 19/11/2026 (C10: thêm KT23, KT24, sửa kỳ vọng KT19, cập nhật tài khoản mẫu và nút menu). Tài liệu này giúp ba người chạy các ca KT trong tài liệu 06 trên **giao diện thật** (Web và Desktop) và ghi biên bản có bằng chứng. Kết quả thật do người chạy điền; **tài liệu này không ghi sẵn "Đạt" cho bất kỳ ca nào**.

## 1. Nguyên tắc
- Mỗi ca chỉ có bốn trạng thái: **Đạt**, **Không đạt**, **Bị chặn** (không chạy được, ghi lý do), **Chưa kiểm thử**. Không ghi Đạt khi thiếu ảnh chụp hoặc kết quả truy vấn SQL kèm theo.
- Quan sát khác kỳ vọng thì ghi đúng điều quan sát được và đánh Không đạt; không sửa kỳ vọng cho khớp. Lỗi tìm thấy ghi vào mục "Lỗi" và báo A (API, CSDL), B (Web), C (Desktop).
- Mỗi lần chạy ghi: ngày giờ, người chạy, `git rev-parse --short HEAD`, hệ điều hành, địa chỉ máy chủ, trình duyệt (Web). Chạy lại sau khi sửa lỗi thì ghi "lần 2" chứ không xóa lần 1.
- Ảnh chụp lưu ở `docs/bang-chung/` (thư mục tự tạo), đặt tên `KT07-lan1-01.png`; không chụp mật khẩu. Truy vấn SQL dán vào biên bản kèm kết quả (văn bản).

## 2. Chuẩn bị môi trường (mỗi người, máy riêng)
**Bước 0 – lấy đúng mã:** `git checkout main && git pull` (từ 18/11 mọi màn hình Desktop và Web đã nằm trên `main`; `main` được bảo vệ, thay đổi đi qua PR). Chạy lại `--migrate` nếu vừa kéo migration mới. Ghi `git rev-parse --short HEAD` vào biên bản.

**Bước 1 – kiểm tra máy (chỉ đọc, ~1 phút):**
```powershell
powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-may.ps1          # thêm -BoQuaBuild để bỏ bước biên dịch
```
Script kiểm .NET SDK, LocalDB, `sqlcmd`, cổng 5055, phiên bản mã, CSDL Dev (đủ migration, đủ 3 tài khoản mẫu, có món đang bán) và biên dịch; dòng LỖI phải sửa trước khi kiểm thử, dòng CHÚ Ý chỉ cần đọc. Hướng dẫn dưới đây đã được chạy thử nguyên văn trên bản sao mới và CSDL trống ngày 14/11 ([verification.md](verification.md), mục "Chạy thử trước kiểm thử tay").

Làm theo [huong-dan-demo.md](huong-dan-demo.md) mục 1–3. Tóm tắt:
```powershell
dotnet build CafeManager.sln
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/CafeManager.Web --no-launch-profile -- --migrate --seed --seed-accounts   # in mật khẩu MỘT lần: ghi lại ngay
dotnet run --project src/CafeManager.Web --launch-profile http                                   # Web + API: http://localhost:5055
```
Desktop (cửa sổ khác): `$env:CAFE_API_BASE_URL='http://localhost:5055'; dotnet run --project src/CafeManager.Desktop`.

| Tài khoản | Loại | Quyền | Dùng cho |
|---|---|---|---|
| `dev.manager` | Nhân viên | MENU_MANAGE | D06 quản lý món, Danh mục (C1), Nguyên liệu (C2) |
| `dev.cashier` | Nhân viên | ORDER_RECEIVE, PAYMENT_COLLECT | D02 tiếp nhận đơn, Thu ngân (A4: mở/đóng ca, thu tiền mặt) |
| `dev.barista` | Nhân viên | PREPARATION_WORK | Pha chế (C3) |
| `dev.customer` | Khách | — | Web (khách A) |
| khách B | Khách | — | Tự đăng ký trên Web (KT16, KT17) |

- Dùng HTTPS (`https://localhost:7034`) thì chạy một lần `dotnet dev-certs https --trust`; nếu không Desktop báo lỗi SSL.
- Ngoài tài khoản mẫu, dữ liệu mẫu có vài món cà phê đang bán. Các ca dưới gọi "Món A" là một món đang bán giá 30.000 đ; nếu món mẫu khác thì dùng món đó và ghi rõ tên trong biên bản (hoặc tạo Món A bằng D06 như KT04).
- Giờ nhận hàng của khách: 07:00–22:00 giờ Việt Nam, không ở quá khứ. Tài khoản nhập sai mật khẩu 5 lần bị khóa 15 phút.
- Truy vấn SQL: SSMS hoặc `sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -I -b -d CafeManager_Group6_Dev -Q "..."`. **Bắt buộc có `-I`** (bật QUOTED_IDENTIFIER): thiếu cờ này, lệnh `UPDATE AspNetUsers …` ở KT21 báo lỗi "UPDATE failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'" và không đổi gì (SSMS đã bật sẵn). Cờ `-b` làm `sqlcmd` trả mã lỗi khác 0 khi câu lệnh lỗi, để không tưởng nhầm là đã chạy.

## 3. Phân công theo kế hoạch
| Người | Ca |
|---|---|
| B – Lê Nam Thuận | KT06, KT07, KT10, KT14, KT16, KT17, KT21 (Web) |
| C – Nguyễn Quốc An | KT01–KT05, KT08, KT09, KT15, KT18, KT19 (Desktop) |
| A – Ngô Nguyên | hỗ trợ SQL/API; chạy lại ca bất kỳ bị Không đạt sau khi sửa; kịch bản liên thông (mục 5) |

| C – Nguyễn Quốc An | (ca do C viết, C10) KT23, KT24 cho trang Web của B; người chạy là C hoặc A vì B là tác giả của hai trang này |

KT09 cần cả Web lẫn Desktop; C chạy, B hỗ trợ phần Web. Thứ tự gợi ý: KT04 (tạo Món A) → KT06 → KT07 → KT08 → KT09 → KT10 → KT14 → còn lại.

## 4. Các ca

Mỗi ca có: các bước, kết quả mong đợi, kiểm tra SQL (nếu có), bằng chứng cần chụp, kiểm thử tự động đã có (chỉ tham khảo, **không thay thế** chạy tay trên giao diện) và bảng kết quả để điền.

### KT01 – Đăng nhập hợp lệ (Desktop) – C
1. Mở Desktop, cửa sổ đăng nhập D00 hiện ra. 2. Nhập `dev.manager` và mật khẩu đã ghi, bấm Đăng nhập.
- Mong đợi: vào màn hình chính "Xin chào Quản lý thử"; chỉ có các nút **Quản lý món**, **Danh mục**, **Nguyên liệu** và **Đăng xuất** (không có Tiếp nhận đơn, Thu ngân, Pha chế; từ 18/11 menu có thêm Danh mục và Nguyên liệu); cửa sổ dòng lệnh của Web/Desktop không in mật khẩu.
- Bằng chứng: ảnh màn hình chính; ảnh log máy chủ (không có mật khẩu).
- Tự động: `ApiTests.Login_errors_lockout_and_logout`, `ApiClientTests.Login_succeeds_for_staff_and_sends_camel_case_json`.

### KT02 – Đăng nhập sai (Desktop) – C
1. Mở D00. 2. Nhập `dev.manager` với mật khẩu sai, bấm Đăng nhập.
- Mong đợi: không vào màn hình chính; thông báo "Sai tên tài khoản hoặc mật khẩu."; ô mật khẩu được xóa. Tắt máy chủ rồi thử lại: thông báo nói **chưa xác định/chưa kết nối được**, không báo "sai mật khẩu".
- Bằng chứng: ảnh hai thông báo trên.
- Tự động: `ApiClientTests.Wrong_password_is_401_without_session_expired_event_and_not_unknown`, `ApiClientTests.Network_failure_is_unknown`.

### KT03 – Chặn thao tác thiếu quyền – C
1. Đăng nhập Desktop bằng `dev.cashier`. 2. Màn hình chính chỉ có **Tiếp nhận đơn** và **Thu ngân (thu tiền, mở/đóng ca)** (từ 18/11 thu ngân có thêm quyền thu tiền), không có Quản lý món, Danh mục, Nguyên liệu, Pha chế. 3. Gọi thẳng API bằng phiên thu ngân:
```powershell
$b = 'http://localhost:5055'; $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Invoke-RestMethod "$b/api/auth/login" -Method Post -WebSession $s -ContentType 'application/json' -Body '{"userName":"dev.cashier","password":"<mật khẩu>"}'
try { Invoke-RestMethod "$b/api/staff/menu" -WebSession $s } catch { $_.Exception.Response.StatusCode.value__ }
```
- Mong đợi: mã **403**; dữ liệu món không đổi (so sánh `SELECT COUNT(*), MAX(ProductId) FROM Product` trước và sau).
- Bằng chứng: ảnh màn hình chính thu ngân; ảnh kết quả lệnh (mã 403).
- Tự động: `ApiTests.Permissions_are_enforced_on_the_server_and_writes_need_an_antiforgery_token`, `DesktopFlowTests.D06_is_forbidden_without_the_menu_permission_and_expired_sessions_raise_the_event`.

### KT04 – Quản lý món (D06) – C
1. Đăng nhập `dev.manager` → **Quản lý món**. 2. **Thêm món** "Món A", chọn danh mục. 3. **Thêm biến thể** (ví dụ "Vừa", loại Đồ uống). 4. **Đổi giá**: 30.000, hiệu lực từ bây giờ. 5. **Công thức**: một nguyên liệu, một ly cần ví dụ 15. 6. **Bật bán**. 7. **Đổi giá** thành 35.000. 8. Gõ "mon a" vào ô **Tìm** (không dấu cũng tìm được).
- Mong đợi: dòng của Món A chuyển từ "Chưa bán: thiếu giá và công thức" → "Chưa bán: thiếu công thức" → "Chưa bán: sẵn sàng bật bán" → (sau Bật bán) "Đang bán"; mỗi lần đổi giá ghi một phiên bản mới; ô Tìm thấy món; sau khi bật bán món hiện trên Web (`/danh-muc`) trong vài giây.
- SQL: `SELECT PriceHistoryId, UnitPrice, EffectiveFrom, EffectiveTo FROM PriceHistory WHERE VariantId = <id> ORDER BY EffectiveFrom;` — giá cũ giữ nguyên, có `EffectiveTo` đóng lại khi giá mới bắt đầu.
- Bằng chứng: ảnh lưới sau từng bước chính; kết quả truy vấn.
- Tự động: `DesktopFlowTests.D06_manager_builds_a_sellable_item_step_by_step_and_it_reaches_the_public_menu`.

### KT05 – Dữ liệu món không hợp lệ – C
1. D06 → **Thêm món** để trống tên, bấm Lưu. 2. **Đổi giá** nhập `-1`, bấm Lưu. 3. Thử thêm `abc` và `35.00`.
- Mong đợi: hộp thoại không đóng, báo lỗi gần trường ("Nhập tên món.", "Giá không được âm.", "Giá không hợp lệ…"); không có món/giá mới. SQL: số dòng `Product` và `PriceHistory` không đổi.
- Bằng chứng: ảnh từng thông báo lỗi; SQL đếm trước/sau.
- Tự động: `StaffPresentationTests` (kiểm tra đầu vào), `DesktopFlowTests.D06_...` (máy chủ từ chối giá âm).

### KT06 – Tìm và xem món Web – B
1. Mở `http://localhost:5055/`. 2. Gõ tên Món A vào ô tìm, mở chi tiết, chọn biến thể và tùy chọn. 3. Tìm một tên không tồn tại. 4. Thu nhỏ cửa sổ về 360 px.
- Mong đợi: đúng món, giá hiện hành và tùy chọn theo dữ liệu; tên không có → thông báo "không tìm thấy" và nút quay lại; không tràn ngang ở 360 px.
- Bằng chứng: ảnh 1280 px và 360 px; ảnh trang trống.
- Tự động: `ApiTests.Anonymous_calls_get_401_json_and_the_public_menu_is_open`, `ServiceTests.Menu_lists_only_sellable_drinks_with_allowed_addons` (API, không kiểm giao diện).

### KT07 – Đặt đơn Web – B
1. Đăng nhập `dev.customer`. 2. Thêm 2 Món A (30.000 đ, không tùy chọn) vào giỏ. 3. `/gio-hang` → Đặt hàng (`/dat-hang`), chọn giờ nhận hợp lệ, kiểm tra tạm tính 60.000 đ, xác nhận.
- Mong đợi: nhận mã đơn và mã nhận hàng, trạng thái "Chờ tiếp nhận"; chưa giữ kho, chưa có phiếu.
- SQL (thay mã đơn):
```sql
SELECT o.OrderCode, o.Status, o.ExpectedPickupAt, o.ContactName, l.ProductNameSnapshot, l.Quantity, l.UnitPriceSnapshot
FROM SalesOrder o JOIN OrderRound r ON r.OrderId = o.OrderId JOIN OrderLine l ON l.RoundId = r.RoundId WHERE o.OrderCode = N'<mã đơn>';
SELECT COUNT(*) AS SoPhieu FROM PreparationTicket t JOIN OrderRound r ON r.RoundId = t.RoundId JOIN SalesOrder o ON o.OrderId = r.OrderId WHERE o.OrderCode = N'<mã đơn>';   -- mong đợi 0
SELECT IngredientId, QuantityOnHand, QuantityReserved FROM StockBalance;   -- QuantityReserved chưa đổi
```
- Bằng chứng: ảnh giỏ, ảnh xác nhận có mã đơn, kết quả SQL.
- Tự động: `ServiceTests.Create_order_prices_on_server_snapshots_lines_and_is_idempotent`, `ApiTests.Order_flow_create_price_change_replay_accept_and_cancel_rules`.

### KT08 – Tiếp nhận đơn Web (D02) – C
Điều kiện: có đơn từ KT07. 1. Đăng nhập Desktop `dev.cashier` → **Tiếp nhận đơn**. 2. Chọn đơn KT07 (theo mã đơn), xem chi tiết. 3. **Tiếp nhận đơn** → Có. 4. Thử thêm: tạo một đơn khác rồi **Từ chối đơn…** (để trống lý do → bị chặn; nhập lý do → thành công).
- Mong đợi: danh sách hiện đúng đơn, món, tùy chọn, giá chốt, giờ nhận; sau khi tiếp nhận đơn rời danh sách chờ, thanh trạng thái báo đã tiếp nhận; trên Web "Đơn của tôi" hiện "Đang phục vụ"; đơn bị từ chối hiện "Bị từ chối" kèm lý do.
- SQL: `SELECT Status FROM SalesOrder WHERE OrderCode=N'<mã>'` = `Serving`; có đúng một dòng ở `PreparationTicket` của đơn; `StockBalance.QuantityReserved` tăng theo công thức (ví dụ 2 ly × 15 = 30); đơn từ chối: `Status='Rejected'`, `RejectReason`, `RejectedBy`, `RejectedAt` có giá trị và không có phiếu.
- Bằng chứng: ảnh D02 trước/sau; ảnh hộp thoại lý do bị chặn; kết quả SQL.
- Tự động: `DesktopFlowTests.D02_cashier_lists_accepts_rejects_and_sees_cancelled_orders_through_the_real_client`, `ServiceTests.Staff_accepts_reserving_stock_rejects_with_reason_and_stock_shortage_leaves_order_pending`.

### KT09 – Dữ liệu dùng chung – C (B hỗ trợ Web)
1. Sau KT04, KT07, KT08: so sánh giá Món A trên Web (`/danh-muc`), D06 và SQL (`PriceHistory`). 2. So sánh mã đơn, dòng món, giá chốt trên Web ("Đơn của tôi"), D02 (đơn chờ) và SQL (truy vấn KT07). 3. Đổi giá Món A rồi mở lại đơn cũ.
- Mong đợi: ba nơi khớp nhau theo thời điểm chốt; đơn cũ vẫn hiện giá chốt cũ sau khi đổi giá.
- Bằng chứng: ba ảnh đặt cạnh nhau + kết quả SQL, cùng mã đơn.
- Tự động: `DatabaseTests.Snapshot_survives_menu_and_price_changes`.

### KT10 – Giá thay đổi trước xác nhận – B (cần C hoặc A đổi giá)
1. Khách `dev.customer` bỏ Món A (giá hiện 30.000) vào giỏ, mở trang đặt hàng nhưng **chưa gửi**. 2. Người khác dùng D06 đổi giá Món A thành 35.000 (hiệu lực ngay, không chọn quá khứ). 3. Khách bấm xác nhận gửi.
- Mong đợi: Web báo giá đã thay đổi, hiện giá cũ và giá mới, **không tạo đơn**; khách đồng ý (xác nhận lại) thì đơn lưu theo 35.000.
- SQL: không có `SalesOrder` mới sau bước 3; sau khi xác nhận lại `UnitPriceSnapshot = 35000`.
- Bằng chứng: ảnh cảnh báo giá, SQL đếm đơn trước/sau.
- Tự động: `ServiceTests.Create_order_rejects_price_change_bad_addon_hours_past_time_and_non_customers`.

### KT14 – Hủy đơn chờ – B
1. Khách có một đơn đang chờ (chưa tiếp nhận). 2. `/don-cua-toi` → chọn đơn → **Hủy** → hộp xác nhận nêu đúng mã đơn → xác nhận. 3. Mở lại đơn. 4. Với một đơn **đã được tiếp nhận** (KT08): thử hủy.
- Mong đợi: đơn chờ chuyển "Đã hủy"; không có phiếu pha chế và `QuantityReserved` không đổi. Đơn đã tiếp nhận không hủy được (nút không hiện hoặc thông báo lý do).
- Bằng chứng: ảnh hộp xác nhận, ảnh trạng thái sau hủy, SQL.
- Tự động: `ServiceTests.Customers_see_only_their_orders_and_cancel_only_pending_ones`.

### KT15 – Hết phiên (Desktop) – C
1. Đăng nhập Desktop `dev.cashier` → mở D02 (tự làm mới 5 giây). 2. Trong SQL vô hiệu hóa nhân viên:
```sql
UPDATE StaffAccount SET IsActive = 0 WHERE FullName = N'Thu ngân thử';
```
3. Đợi tối đa 5 giây. 4. Khôi phục: `UPDATE StaffAccount SET IsActive = 1 WHERE FullName = N'Thu ngân thử';`.
- Mong đợi: D02 tự đóng, hiện "Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại." và quay về màn hình đăng nhập; không còn thao tác được bằng phiên cũ (có thể thử lại lệnh `Invoke-RestMethod` ở KT03 với phiên cũ → 401).
- Bằng chứng: ảnh thông báo; ảnh/kết quả lệnh 401.
- Tự động: `ApiTests.Revoked_staff_loses_the_session_on_the_next_request`, `DesktopFlowTests.D06_is_forbidden_without_the_menu_permission_and_expired_sessions_raise_the_event`. Hành vi đóng cửa sổ trên giao diện **chỉ kiểm bằng ca này**.

### KT16 – Quyền sở hữu đơn – B
1. Khách A đặt một đơn, ghi `id` đơn (xem trên URL `/don-cua-toi/<id>`). 2. Đăng nhập khách B (trình duyệt/cửa sổ ẩn danh khác). 3. B mở `/don-cua-toi/<id của A>` và `/don-cua-toi/<id của A>/huy`; thử bấm hủy. 4. A mở lại đơn.
- Mong đợi: B nhận trang "không tìm thấy" (404), không xem và không hủy được; đơn của A không đổi; A vẫn xem được.
- SQL: `SELECT Status FROM SalesOrder WHERE OrderId = <id>` không đổi.
- Bằng chứng: ảnh trang 404 của B; ảnh đơn của A; SQL.
- Tự động: `ServiceTests.Customers_see_only_their_orders_and_cancel_only_pending_ones`.

### KT17 – Đăng ký tài khoản khách – B
1. `/tai-khoan/dang-ky`: nhập tên đăng nhập mới hợp lệ, mật khẩu ≥ 6 ký tự, họ tên, số điện thoại → đăng ký → đăng nhập. 2. Đăng ký lại cùng tên. 3. Thử dữ liệu sai: mật khẩu ngắn, xác nhận không khớp, số điện thoại sai.
- Mong đợi: lần 1 thành công; trùng tên và dữ liệu sai bị từ chối, lỗi gần trường, giữ lại giá trị đã nhập (trừ mật khẩu); không có tài khoản dở.
- SQL:
```sql
SELECT u.UserName, c.FullName FROM AspNetUsers u LEFT JOIN CustomerProfile c ON c.IdentityUserId = u.Id WHERE u.UserName = N'<tên>';   -- đúng 1 dòng, có FullName
SELECT COUNT(*) FROM AspNetUsers u WHERE NOT EXISTS (SELECT 1 FROM CustomerProfile c WHERE c.IdentityUserId = u.Id) AND NOT EXISTS (SELECT 1 FROM StaffAccount s WHERE s.IdentityUserId = u.Id);   -- 0
```
- Bằng chứng: ảnh từng trường hợp; SQL.
- Tự động: `ServiceTests.Register_creates_account_with_profile_and_rejects_duplicates_and_bad_input`, `ApiTests.Register_customer_over_http_then_login`.

### KT18 – Khoảng hiệu lực giá – C
1. D06: chọn biến thể đã có giá hiện hành. 2. **Đổi giá** với hiệu lực **30 phút nữa** (ví dụ 31.000): được nhận, giá hiện hành chưa đổi. 3. **Đổi giá** lần nữa với hiệu lực **bây giờ** (hoặc sớm hơn mốc 30 phút đó nhưng không quá 5 phút trước).
- Mong đợi: bước 2 được nhận; bước 3 bị từ chối vì đã có giá hiệu lực từ cùng hoặc sau thời điểm này, hộp báo lỗi hướng dẫn chọn thời điểm muộn hơn phiên bản hiện có; giá hiện hành vẫn là giá cũ cho đến lúc mốc 30 phút tới. Không làm bước 3 trước bước 2: giá mẫu có hiệu lực từ 01/01/2026 nên một giá "bây giờ" khi chưa có giá hẹn trước chỉ **thay** giá hiện hành, không chồng lấn, nên được nhận chứ không bị từ chối. Sau ca này biến thể đó có một giá hẹn trước 30 phút, nên mọi lần "Đổi giá hiệu lực ngay" trên chính biến thể đó (KT09, KT10) sẽ bị từ chối cho tới khi mốc ấy qua: **chạy KT18 sau KT09 và KT10**, hoặc làm trên một biến thể riêng (ví dụ "[THỬ] Cà phê đen").
- SQL: không có hai dòng `PriceHistory` của cùng biến thể chồng khoảng: dòng trước có `EffectiveTo` bằng `EffectiveFrom` của dòng sau.
- Bằng chứng: ảnh thông báo; SQL.
- Tự động: `DesktopFlowTests.D06_manager_builds_...`, `DatabaseTests.Effective_price_and_recipe_intervals_cannot_overlap`, `DatabaseTests.Concurrent_overlapping_prices_allow_only_one_commit`.

### KT19 – Quản lý danh mục và xóa – C
**Kỳ vọng gốc (tài liệu 06, giữ nguyên trong hồ sơ):** quản lý và xóa theo quyền khi đủ điều kiện.

**Kỳ vọng sửa (C10, 19/11/2026).** Đây là thay đổi phạm vi có chủ đích, **không phải ca đã đạt theo kỳ vọng gốc**; cả nhóm phải đồng ý và báo cáo (mục giới hạn, việc C7) phải nêu như một quyết định thiết kế. Kỳ vọng gốc được tách theo từng đối tượng, mỗi phần đúng với điều hệ thống làm:
- **(a) Món (D06):** không có thao tác xóa món, chỉ ngừng bán và bật bán lại. Lý do: giữ lịch sử đơn (đơn cũ cần tên và giá chốt); xóa trực tiếp trong CSDL bị chặn (`DatabaseTests.Foreign_key_and_history_delete_are_rejected`).
- **(b) Danh mục (Desktop, nút Danh mục):** xóa được danh mục không còn món (có hộp xác nhận); danh mục còn món không bị xóa, thông báo nêu tên và số món; có thể bỏ chọn "Đang dùng" để ngừng sử dụng.
- **(c) Nguyên liệu (Desktop, nút Nguyên liệu):** xóa được nguyên liệu mới tạo, chưa nằm trong công thức và chưa có tồn hoặc phát sinh kho; nguyên liệu đang trong công thức, hoặc đã có tồn/phát sinh kho, không bị xóa và có thông báo rõ; có thể ngừng dùng.

**Các bước.**
1. Phần (a): dùng một món đã có đơn (KT07) và một món mới chưa có đơn. D06: tìm, **Sửa món**, **Ngừng bán** biến thể rồi bật bán lại. Kiểm tra Web và đơn cũ.
2. Phần (b): đăng nhập `dev.manager` → **Danh mục**. (i) Chọn danh mục có "Số món" > 0 → **Xóa…**. (ii) **Thêm…** "Danh mục thử KT19", **Sửa…** đổi tên, thử tìm theo tên. (iii) Chọn danh mục vừa thêm → **Xóa…** → trả lời **Không**, rồi lại **Xóa…** → trả lời **Có**. (iv) Thêm lại một danh mục trống, dùng D06 gán một món vào đó, quay lại **Danh mục** bấm **Tải lại** rồi **Xóa…**.
3. Phần (c): **Nguyên liệu**. (i) Chọn nguyên liệu mẫu có "Dùng trong công thức" > 0 → **Xóa…**. (ii) **Thêm…** "Nguyên liệu thử KT19", đơn vị `g` → **Xóa…** → **Có**.

**Mong đợi.**
- (a) Món ngừng bán biến mất khỏi Web nhưng còn trong D06 và bật lại được; đơn cũ vẫn thấy tên và giá chốt ở "Đơn của tôi"; không có nút nào xóa món.
- (b)(i) Hiện thông báo "Danh mục "…" còn N món nên không thể xóa…" và danh mục vẫn còn trong danh sách. (ii) Danh mục mới xuất hiện, đổi tên được, tìm thấy; thêm trùng tên báo "Đã có danh mục trùng tên.". (iii) **Không** thì giữ nguyên; **Có** thì danh mục biến mất và thanh trạng thái báo đã xóa. (iv) Không xóa được, vẫn còn trong danh sách.
- (c)(i) Hiện thông báo nguyên liệu đang nằm trong N công thức nên không thể xóa; vẫn còn trong danh sách. (ii) Nguyên liệu thử biến mất sau khi xác nhận.
- SQL: `SELECT COUNT(*) FROM Category WHERE CategoryName = N'Danh mục thử KT19';` và `SELECT COUNT(*) FROM Ingredient WHERE IngredientName = N'Nguyên liệu thử KT19';` bằng 0 sau khi xóa thành công; `SELECT COUNT(*) FROM Product;` không đổi sau mọi thao tác (a).
- Bằng chứng: ảnh D06, Web trước/sau, đơn cũ; ảnh từng thông báo chặn; ảnh hộp xác nhận; ảnh danh sách trước và sau khi xóa; kết quả SQL.
- Tự động: `DatabaseTests.Foreign_key_and_history_delete_are_rejected`, `DesktopFlowTests.D06_manager_builds_...`; quy tắc phần (b), (c) nằm ở `StaffCatalogService.DeleteCategoryAsync` và `DeleteIngredientAsync`; phần giao diện của (b), (c) chỉ kiểm bằng ca này.
- **Ghi biên bản:** kết quả lần 1 (15/11, Không đạt vì lúc đó chưa có thao tác xóa nào) giữ nguyên. Lần 2 ghi riêng từng phần (a), (b), (c); chỉ ghi Đạt khi cả ba phần đúng kỳ vọng sửa, và vẫn ghi rõ kỳ vọng gốc đã được đổi thành quyết định thiết kế.

### KT21 – Đăng nhập tài khoản khách – B
1. `/tai-khoan/dang-nhap`: `dev.customer` đúng mật khẩu → có phiên. 2. Đăng xuất. 3. Nhập sai mật khẩu → không có phiên. 4. Nhập sai 5 lần liên tiếp → thông báo khóa; thử mật khẩu **đúng** khi đang khóa. 5. Mở khóa để thử tiếp (chạy trong SSMS, hoặc `sqlcmd` **có cờ `-I`**, xem mục 2): `UPDATE AspNetUsers SET LockoutEnd = NULL, AccessFailedCount = 0 WHERE UserName = N'dev.customer';` rồi kiểm tra `SELECT LockoutEnd FROM AspNetUsers WHERE UserName = N'dev.customer'` là NULL. Nếu không chạy được thì chờ 15 phút hết khóa.
- Mong đợi: sai/khóa không cấp phiên (vào `/don-cua-toi` bị chuyển về đăng nhập); mật khẩu đúng khi đang khóa vẫn bị từ chối; sau KT21 quyền sở hữu đơn vẫn đúng (xem KT16).
- Bằng chứng: ảnh thông báo sai/khóa; ảnh bị chuyển về đăng nhập.
- Tự động: `ServiceTests.Login_distinguishes_wrong_password_lockout_and_account_kind`, `ApiTests.Login_errors_lockout_and_logout`.

### KT23 – Chi tiết món trên Web (B1) – ca do C viết (C10); người chạy: C hoặc A
Điều kiện: món mẫu [THỬ] Cà phê sữa đang bán (có biến thể, có tùy chọn thêm nếu dữ liệu mẫu có), một món đã **Ngừng bán** bằng D06 (KT19), khách `dev.customer`.
1. Mở `/`, bấm tên món để vào `/mon/<id>`. 2. Đối chiếu nội dung trang với D06 và SQL. 3. Chọn một biến thể, đặt tùy chọn thêm 1 (nếu có), số lượng 2 → **Thêm vào giỏ** → mở `/gio-hang`. 4. Thử số lượng không hợp lệ: `0`, `51`, `abc` (xóa thuộc tính `min`/`max` của ô bằng DevTools của trình duyệt để biểu mẫu vẫn gửi được); thử tùy chọn thêm vượt "tối đa". 5. Mở `/mon/999999999` và `/mon/<id món vừa ngừng bán>` (chờ vài giây vì thực đơn được lưu đệm). 6. Dùng D06 đổi giá món rồi tải lại trang chi tiết. 7. Thu nhỏ cửa sổ về 360 px; dùng phím Tab đi qua trang.
- Mong đợi:
  - Bước 1–2: tiêu đề là tên món, có tên danh mục và nhãn "Đang bán", dòng "N kích cỡ, giá từ X" (X là giá thấp nhất), mỗi biến thể một khối gồm tên, giá, các tùy chọn dạng "tên (+giá), tối đa N" và ô số lượng mặc định 1; giá khớp giá hiện hành trong D06 và `PriceHistory`.
  - Bước 3: sau khi thêm, giỏ có đúng món, biến thể, số lượng 2 (và tùy chọn đã chọn); thành tiền = số lượng × (giá + tùy chọn).
  - Bước 4: không thêm được, giỏ không đổi, có thông báo lỗi rõ: "Số lượng phải là số nguyên dương." với `0` và `abc`; "Số lượng tối đa 50 mỗi dòng." với `51`; tùy chọn vượt mức: "«<tên tùy chọn>» tối đa N mỗi ly."
  - Bước 5: mã HTTP 404, trang "Món không còn bán" và nút "Quay về thực đơn"; không hiện lỗi hệ thống.
  - Bước 6: giá mới hiện sau khi tải lại (có thể chậm vài giây); đơn đã đặt trước đó vẫn giữ giá chốt cũ (xem KT09).
  - Bước 7: không tràn ngang ở 360 px; thứ tự Tab hợp lý (tùy chọn → số lượng → **Thêm vào giỏ**), mỗi ô có nhãn đọc được.
- SQL: giá hiện hành `SELECT VariantId, UnitPrice FROM PriceHistory WHERE VariantId = <id> AND EffectiveTo IS NULL;`; `SELECT COUNT(*) FROM SalesOrder;` không đổi sau bước 3–4 (giỏ chưa phải đơn).
- Bằng chứng: ảnh trang chi tiết ở 1280 px và 360 px; ảnh giỏ; ảnh từng thông báo lỗi; ảnh trang 404; kết quả SQL.
- Tự động: chưa có test riêng cho trang `/mon/{id}` và luật số lượng trong giỏ; dữ liệu thực đơn được kiểm ở `ServiceTests.Menu_lists_only_sellable_drinks_with_allowed_addons` (mức dịch vụ, không kiểm giao diện).

### KT24 – Tiến độ pha chế và thanh toán trong "Đơn của tôi" (B2) – ca do C viết (C10); người chạy: C hoặc A
Điều kiện: CSDL Dev đã `--seed-accounts`; ba cửa sổ: Web (khách `dev.customer`), Desktop `dev.cashier` (đã **Mở ca**), Desktop `dev.barista`. Món A giá 30.000.
1. Khách đặt đơn 2 ly Món A (60.000). Mở `/don-cua-toi` và mở chi tiết đơn. 2. `dev.cashier`: **Tiếp nhận đơn**. Khách bấm **Tải lại trạng thái**. 3. `dev.barista` → **Pha chế**: **Bắt đầu làm** ly 1, tải lại trên Web; **Làm xong** ly 1, tải lại; làm tiếp ly 2 đến **Sẵn sàng**; **Đã giao cho khách** cả hai ly, tải lại sau mỗi bước. 4. `dev.cashier` → **Thu ngân**: chọn đơn, nhập tiền khách đưa 100.000, **Thu tiền mặt**; khách tải lại. 5. Tạo thêm một đơn rồi **Hủy** (khách) và một đơn **Từ chối** (thu ngân, có lý do); xem `/don-cua-toi` và chi tiết hai đơn đó.
- Mong đợi (chữ hiển thị đúng như dưới đây):
  - Bước 1: đơn "Chờ tiếp nhận"; trong danh sách **không có** dòng Pha chế/Thanh toán; trang chi tiết ghi "Đơn đang chờ quán tiếp nhận nên chưa bắt đầu pha chế." và có nút **Hủy đơn**.
  - Bước 2: "Đang phục vụ"; Pha chế: "Đang chờ pha chế"; "Chờ làm 2 · Đang làm 0 · Sẵn sàng nhận 0 · Đã giao 0"; Thanh toán: "Chưa thanh toán"; không còn nút Hủy ("Đơn không còn chờ tiếp nhận nên không có nút hủy.").
  - Bước 3: sau khi bắt đầu ly 1: "Đang pha chế", "Chờ làm 1 · Đang làm 1 · Sẵn sàng nhận 0 · Đã giao 0"; khi có ly sẵn sàng: "Có ly sẵn sàng, mời bạn nhận" và số "Sẵn sàng nhận" tăng; giao đủ cả hai ly: "Đã giao đủ", "Đã giao 2".
  - Bước 4: Thu ngân báo tiền thối 40.000 đ; Web: Thanh toán "Đã thanh toán" (nhãn xanh), cả ở danh sách lẫn chi tiết. Trạng thái đơn trên Web (Đang phục vụ / Đang thanh toán / Hoàn tất) ghi lại đúng như quan sát được và phải khớp SQL, không đoán trước.
  - Danh sách và chi tiết cho cùng một đơn luôn hiện cùng số liệu.
  - Bước 5: đơn đã hủy hoặc bị từ chối không có dòng tiến độ ở danh sách; chi tiết đơn đã hủy ghi "Đơn không còn được thực hiện."; đơn bị từ chối hiện "Lý do từ chối" đúng nội dung thu ngân nhập.
- Giới hạn đã biết: trạng thái "Đã thanh toán một phần" **không tạo được từ giao diện** (Thu ngân chỉ thu đủ số còn phải thu); nhánh này chỉ kiểm bằng dữ liệu/API, không ghi Đạt cho luồng người dùng.
- SQL (thay mã đơn):
```sql
SELECT Status FROM SalesOrder WHERE OrderCode = N'<mã đơn>';
SELECT u.Status, COUNT(*) AS SoLy FROM PreparationUnit u JOIN OrderLine l ON l.LineId = u.LineId
  JOIN OrderRound r ON r.RoundId = l.RoundId JOIN SalesOrder o ON o.OrderId = r.OrderId WHERE o.OrderCode = N'<mã đơn>' GROUP BY u.Status;
SELECT p.Amount, p.Method, p.Status, p.PaidAt FROM Payment p JOIN SalesOrder o ON o.OrderId = p.OrderId WHERE o.OrderCode = N'<mã đơn>';
```
Số ly theo trạng thái và số đã thu trên Web phải khớp ba truy vấn này sau mỗi bước.
- Bằng chứng: ảnh Web sau từng bước chính (bước 1, 2, 3 có ly đang làm, 3 có ly sẵn sàng, 3 giao đủ, 4), ảnh Desktop tương ứng, kết quả SQL.
- Tự động: `StaffA123ApiTests` kiểm `Progress` ở mức API (số đã trả, số ly); chữ hiển thị `OrderProgressText` và hai trang Web chưa có test riêng.

## 5. Kịch bản liên thông (A điều phối; ghi như một biên bản riêng "LT01")
Trên CSDL Dev sạch (xem [huong-dan-demo.md](huong-dan-demo.md) mục 5): quản lý tạo món (D06, KT04) → khách đăng ký và đặt đơn trên Web (KT17, KT07) → thu ngân tiếp nhận trên Desktop (KT08) → khách thấy "Đang phục vụ" trên Web → đối chiếu SQL (KT09). Ghi thời gian từng bước và ảnh.

## 6. Ca chưa có giao diện (không chạy tay; ghi đúng thực tế)
Các ca sau cần màn hình thu tiền, ca làm việc, hoàn tiền, đối soát hoặc nhập kho mà dự án **chưa xây dựng giao diện**. Ghi trong báo cáo là "kiểm thử tự động ở mức dữ liệu/dịch vụ; chưa kiểm thử trên giao diện", không ghi Đạt cho luồng người dùng.

| Ca | Kiểm thử tự động đang có | Giới hạn |
|---|---|---|
| KT11, TC04 gửi lặp yêu cầu thu | `DatabaseTests.Payment_replay_returns_same_result_and_different_payload_is_rejected` | không có giao diện thu tiền |
| KT12, TC03 cạnh tranh tồn kho | `DatabaseTests.Concurrent_acceptance_cannot_overreserve_last_stock` | hai tác vụ đồng thời trong bộ kiểm thử, không phải thử tải |
| KT13, TC07 ca đã đóng | `DatabaseTests.Closed_shift_rejects_receipts` | chưa có màn đóng/mở ca; chưa có thử đóng ca đồng thời |
| KT20 ràng buộc quan hệ | `DatabaseTests.Order_channel_requires_customer_or_session_correctly`, `Concurrent_open_table_allows_one_session`, `Preparation_unit_cannot_reference_another_round_line` | — |
| KT22 đối soát tiền mặt | `DatabaseTests.Reconciliation_counts_successful_cash_and_other_posted_entries_only` | chưa có giao diện |
| TC02, TC05, TC06, TC09, TC10 | `Concurrent_open_table_allows_one_session`, `Independent_approval_and_per_unit_cancellation_key_are_enforced`, `Refund_cannot_exceed_original_payment_even_across_requests`, `Snapshot_survives_menu_and_price_changes`, `Pending_round_has_no_ticket_then_accept_is_atomic_and_repeatable` | mức dữ liệu |
| TC08 khóa/thu hồi quyền | `ApiTests.Revoked_staff_loses_the_session_on_the_next_request` | giao diện kiểm ở KT15 |
| TC01, TC11 | chưa có | gọi thêm sau lượt đầu; hiệu năng/phục hồi: **chưa thực hiện**, không ghi số liệu |

## 7. Mẫu biên bản cho một lần chạy
Sao chép vào báo cáo hoặc tệp riêng, điền sau khi chạy.

| Trường | Nội dung |
|---|---|
| Mã ca / lần chạy | KT__ – lần __ |
| Ngày giờ, người chạy | |
| Phiên bản và môi trường | commit ____; Windows ____; .NET 10.0.__; SQL Server LocalDB; trình duyệt ____ (Web) |
| Dữ liệu, tiền điều kiện | tài khoản, mã đơn, trạng thái trước khi chạy |
| Các bước đã làm | |
| Kết quả thực tế | giá trị quan sát được; đường dẫn ảnh `docs/bang-chung/…`; kết quả SQL |
| Trạng thái | Đạt / Không đạt / Bị chặn / Chưa kiểm thử |
| Lỗi và chạy lại | mô tả, mức ảnh hưởng, commit sửa, kết quả lần chạy lại |

Bảng điền nhanh (một dòng mỗi ca). **Cập nhật 15/11/2026:** các dòng dưới được điền từ [biên bản kiểm thử tự động](bien-ban-kiem-thu.md) (Web điều khiển bằng Edge không đầu, Desktop bằng chương trình điều khiển; **không phải thao tác chuột bằng tay**, xem mục 1 của biên bản về phương pháp và giới hạn). Ca nào nhóm tự chạy tay thì thêm lần chạy mới ("lần 2") thay vì xóa dòng này:

| Ca | Người | Ngày giờ | Commit | Trạng thái | Bằng chứng |
|---|---|---|---|---|---|
| KT01 | C (chạy tự động) | 15/11/2026 09:03 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt01), 2 ảnh, 5/5 kiểm tra |
| KT02 | C (chạy tự động) | 15/11/2026 09:03 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt02), 2 ảnh, 8/8 kiểm tra |
| KT03 | C (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt03), 1 ảnh, 5/5 kiểm tra |
| KT04 | C (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt04), 11 ảnh, 10/10 kiểm tra |
| KT05 | C (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt05), 4 ảnh, 8/8 kiểm tra |
| KT06 | B (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt06), 4 ảnh, 4/4 kiểm tra |
| KT07 | B (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt07), 3 ảnh, 5/5 kiểm tra |
| KT08 | C (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt08), 7 ảnh, 13/13 kiểm tra |
| KT09 | C (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt09), 9 ảnh, 11/11 kiểm tra |
| KT10 | B (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt10), 6 ảnh, 6/6 kiểm tra |
| KT14 | B (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt14), 6 ảnh, 7/7 kiểm tra |
| KT15 | C (chạy tự động) | 15/11/2026 09:05 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt15), 2 ảnh, 9/9 kiểm tra |
| KT16 | B (chạy tự động) | 15/11/2026 09:05 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt16), 3 ảnh, 5/5 kiểm tra |
| KT17 | B (chạy tự động) | 15/11/2026 09:04 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt17), 6 ảnh, 9/9 kiểm tra |
| KT18 | C (chạy tự động) | 15/11/2026 09:05 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt18), 5 ảnh, 5/5 kiểm tra |
| KT19 | C (chạy tự động) | 15/11/2026 09:05 | 7a8d253 | Không đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt19), 9 ảnh, 10/10 kiểm tra; không có thao tác xóa |
| KT21 | B (chạy tự động) | 15/11/2026 09:05 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#kt21), 4 ảnh, 10/10 kiểm tra |
| LT01 liên thông | A (chạy tự động) | 15/11/2026 09:06 | 7a8d253 | Đạt | Tự động; [biên bản](bien-ban-kiem-thu.md#lt01), 15 ảnh, 17/17 kiểm tra |

## 8. Lưu ý khi thử
- Mỗi lần chạy lại KT07/KT08 nên dùng đơn mới; một đơn đã tiếp nhận không tiếp nhận lại được.
- D02 tự làm mới mỗi 5 giây; thấy đơn mới sau tối đa 5 giây hoặc bấm **Tải lại (F5)**.
- Giá mới có hiệu lực từ thời điểm chọn ở hộp **Đổi giá** (giờ máy); không chọn quá khứ hơn 5 phút. Món mới chỉ bán được khi có cả giá và công thức hiệu lực.
- Sau KT15/KT21 nhớ khôi phục tài khoản (lệnh SQL trong từng ca) để các ca sau không bị ảnh hưởng.
