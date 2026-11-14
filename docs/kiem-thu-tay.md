# Hướng dẫn kiểm thử tay (nhóm thực hiện và ghi biên bản)

Cập nhật 14/11/2026. Tài liệu này giúp ba người chạy các ca KT trong tài liệu 06 trên **giao diện thật** (Web và Desktop) và ghi biên bản có bằng chứng. Kết quả thật do người chạy điền; **tài liệu này không ghi sẵn "Đạt" cho bất kỳ ca nào**.

## 1. Nguyên tắc
- Mỗi ca chỉ có bốn trạng thái: **Đạt**, **Không đạt**, **Bị chặn** (không chạy được, ghi lý do), **Chưa kiểm thử**. Không ghi Đạt khi thiếu ảnh chụp hoặc kết quả truy vấn SQL kèm theo.
- Quan sát khác kỳ vọng thì ghi đúng điều quan sát được và đánh Không đạt; không sửa kỳ vọng cho khớp. Lỗi tìm thấy ghi vào mục "Lỗi" và báo A (API, CSDL), B (Web), C (Desktop).
- Mỗi lần chạy ghi: ngày giờ, người chạy, `git rev-parse --short HEAD`, hệ điều hành, địa chỉ máy chủ, trình duyệt (Web). Chạy lại sau khi sửa lỗi thì ghi "lần 2" chứ không xóa lần 1.
- Ảnh chụp lưu ở `docs/bang-chung/` (thư mục tự tạo), đặt tên `KT07-lan1-01.png`; không chụp mật khẩu. Truy vấn SQL dán vào biên bản kèm kết quả (văn bản).

## 2. Chuẩn bị môi trường (mỗi người, máy riêng)
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
| `dev.manager` | Nhân viên | MENU_MANAGE | D06 quản lý món |
| `dev.cashier` | Nhân viên | ORDER_RECEIVE | D02 tiếp nhận đơn |
| `dev.customer` | Khách | — | Web (khách A) |
| khách B | Khách | — | Tự đăng ký trên Web (KT16, KT17) |

- Dùng HTTPS (`https://localhost:7034`) thì chạy một lần `dotnet dev-certs https --trust`; nếu không Desktop báo lỗi SSL.
- Ngoài tài khoản mẫu, dữ liệu mẫu có vài món cà phê đang bán. Các ca dưới gọi "Món A" là một món đang bán giá 30.000 đ; nếu món mẫu khác thì dùng món đó và ghi rõ tên trong biên bản (hoặc tạo Món A bằng D06 như KT04).
- Giờ nhận hàng của khách: 07:00–22:00 giờ Việt Nam, không ở quá khứ. Tài khoản nhập sai mật khẩu 5 lần bị khóa 15 phút.
- Truy vấn SQL: SSMS hoặc `sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -d CafeManager_Group6_Dev -Q "..."`.

## 3. Phân công theo kế hoạch
| Người | Ca |
|---|---|
| B – Lê Nam Thuận | KT06, KT07, KT10, KT14, KT16, KT17, KT21 (Web) |
| C – Nguyễn Quốc An | KT01–KT05, KT08, KT09, KT15, KT18, KT19 (Desktop) |
| A – Ngô Nguyên | hỗ trợ SQL/API; chạy lại ca bất kỳ bị Không đạt sau khi sửa; kịch bản liên thông (mục 5) |

KT09 cần cả Web lẫn Desktop; C chạy, B hỗ trợ phần Web. Thứ tự gợi ý: KT04 (tạo Món A) → KT06 → KT07 → KT08 → KT09 → KT10 → KT14 → còn lại.

## 4. Các ca

Mỗi ca có: các bước, kết quả mong đợi, kiểm tra SQL (nếu có), bằng chứng cần chụp, kiểm thử tự động đã có (chỉ tham khảo, **không thay thế** chạy tay trên giao diện) và bảng kết quả để điền.

### KT01 – Đăng nhập hợp lệ (Desktop) – C
1. Mở Desktop, cửa sổ đăng nhập D00 hiện ra. 2. Nhập `dev.manager` và mật khẩu đã ghi, bấm Đăng nhập.
- Mong đợi: vào màn hình chính "Xin chào Quản lý thử"; chỉ có nút **Quản lý món** và **Đăng xuất** (không có Tiếp nhận đơn); cửa sổ dòng lệnh của Web/Desktop không in mật khẩu.
- Bằng chứng: ảnh màn hình chính; ảnh log máy chủ (không có mật khẩu).
- Tự động: `ApiTests.Login_errors_lockout_and_logout`, `ApiClientTests.Login_succeeds_for_staff_and_sends_camel_case_json`.

### KT02 – Đăng nhập sai (Desktop) – C
1. Mở D00. 2. Nhập `dev.manager` với mật khẩu sai, bấm Đăng nhập.
- Mong đợi: không vào màn hình chính; thông báo "Sai tên tài khoản hoặc mật khẩu."; ô mật khẩu được xóa. Tắt máy chủ rồi thử lại: thông báo nói **chưa xác định/chưa kết nối được**, không báo "sai mật khẩu".
- Bằng chứng: ảnh hai thông báo trên.
- Tự động: `ApiClientTests.Wrong_password_is_401_without_session_expired_event_and_not_unknown`, `ApiClientTests.Network_failure_is_unknown`.

### KT03 – Chặn thao tác thiếu quyền – C
1. Đăng nhập Desktop bằng `dev.cashier`. 2. Màn hình chính chỉ có **Tiếp nhận đơn**, không có Quản lý món. 3. Gọi thẳng API bằng phiên thu ngân:
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
1. D06: chọn biến thể đã có giá hiện hành. 2. **Đổi giá** với hiệu lực từ **trùng hoặc trước** thời điểm giá hiện hành (chọn lùi đủ để không bị chặn bởi "quá khứ", tối đa 5 phút trước). 3. **Đổi giá** với hiệu lực sau (ví dụ 30 phút nữa).
- Mong đợi: bước 2 bị từ chối và hộp báo lỗi hướng dẫn chọn thời điểm muộn hơn; bước 3 được nhận, giá hiện hành chưa đổi cho đến lúc đó.
- SQL: không có hai dòng `PriceHistory` của cùng biến thể chồng khoảng: dòng trước có `EffectiveTo` bằng `EffectiveFrom` của dòng sau.
- Bằng chứng: ảnh thông báo; SQL.
- Tự động: `DesktopFlowTests.D06_manager_builds_...`, `DatabaseTests.Effective_price_and_recipe_intervals_cannot_overlap`, `DatabaseTests.Concurrent_overlapping_prices_allow_only_one_commit`.

### KT19 – Quản lý danh mục và xóa – C
1. Dùng một món đã có đơn (KT07) và một món mới chưa có đơn. 2. D06: tìm, **Sửa món**, **Ngừng bán** biến thể. 3. Kiểm tra Web và đơn cũ.
- **Lưu ý phạm vi:** hệ thống **không có chức năng xóa món** (cố ý: giữ lịch sử); chỉ ngừng bán. Ca gốc nói "xóa theo quyền khi đủ điều kiện", nên ghi biên bản đúng thực tế: "không có thao tác xóa; chỉ ngừng bán" — **không ghi Đạt toàn phần nếu kỳ vọng gốc là xóa**; nêu là hạn chế/quyết định thiết kế.
- Mong đợi: món ngừng bán biến mất khỏi Web nhưng còn trong D06 và bật lại được; đơn cũ vẫn thấy tên và giá chốt ở "Đơn của tôi".
- Bằng chứng: ảnh D06, ảnh Web trước/sau, ảnh đơn cũ.
- Tự động: `DatabaseTests.Foreign_key_and_history_delete_are_rejected`, `DesktopFlowTests.D06_manager_builds_...`.

### KT21 – Đăng nhập tài khoản khách – B
1. `/tai-khoan/dang-nhap`: `dev.customer` đúng mật khẩu → có phiên. 2. Đăng xuất. 3. Nhập sai mật khẩu → không có phiên. 4. Nhập sai 5 lần liên tiếp → thông báo khóa; thử mật khẩu **đúng** khi đang khóa. 5. Mở khóa để thử tiếp: `UPDATE AspNetUsers SET LockoutEnd = NULL, AccessFailedCount = 0 WHERE UserName = N'dev.customer';`.
- Mong đợi: sai/khóa không cấp phiên (vào `/don-cua-toi` bị chuyển về đăng nhập); mật khẩu đúng khi đang khóa vẫn bị từ chối; sau KT21 quyền sở hữu đơn vẫn đúng (xem KT16).
- Bằng chứng: ảnh thông báo sai/khóa; ảnh bị chuyển về đăng nhập.
- Tự động: `ServiceTests.Login_distinguishes_wrong_password_lockout_and_account_kind`, `ApiTests.Login_errors_lockout_and_logout`.

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

Bảng điền nhanh (một dòng mỗi ca; mặc định chưa chạy):

| Ca | Người | Ngày giờ | Commit | Trạng thái | Bằng chứng |
|---|---|---|---|---|---|
| KT01 | C | | | Chưa kiểm thử | |
| KT02 | C | | | Chưa kiểm thử | |
| KT03 | C | | | Chưa kiểm thử | |
| KT04 | C | | | Chưa kiểm thử | |
| KT05 | C | | | Chưa kiểm thử | |
| KT06 | B | | | Chưa kiểm thử | |
| KT07 | B | | | Chưa kiểm thử | |
| KT08 | C | | | Chưa kiểm thử | |
| KT09 | C | | | Chưa kiểm thử | |
| KT10 | B | | | Chưa kiểm thử | |
| KT14 | B | | | Chưa kiểm thử | |
| KT15 | C | | | Chưa kiểm thử | |
| KT16 | B | | | Chưa kiểm thử | |
| KT17 | B | | | Chưa kiểm thử | |
| KT18 | C | | | Chưa kiểm thử | |
| KT19 | C | | | Chưa kiểm thử | |
| KT21 | B | | | Chưa kiểm thử | |
| LT01 liên thông | A | | | Chưa kiểm thử | |

## 8. Lưu ý khi thử
- Mỗi lần chạy lại KT07/KT08 nên dùng đơn mới; một đơn đã tiếp nhận không tiếp nhận lại được.
- D02 tự làm mới mỗi 5 giây; thấy đơn mới sau tối đa 5 giây hoặc bấm **Tải lại (F5)**.
- Giá mới có hiệu lực từ thời điểm chọn ở hộp **Đổi giá** (giờ máy); không chọn quá khứ hơn 5 phút. Món mới chỉ bán được khi có cả giá và công thức hiệu lực.
- Sau KT15/KT21 nhớ khôi phục tài khoản (lệnh SQL trong từng ca) để các ca sau không bị ảnh hưởng.
