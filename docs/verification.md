# Kết quả kiểm chứng phần dữ liệu

Ngày 08/11/2026, workspace local trên nhánh main chưa có commit (đến 10/11/2026 đã có 2 commit cục bộ, chưa đẩy lên GitHub). Đây là bằng chứng phạm vi dữ liệu, không là nghiệm thu 19 UC hay sản phẩm Web–Desktop.

## Kết quả thực tế

| Hạng mục | Kết quả |
|---|---|
| Đọc yêu cầu | Truy cập được cả 6 tài liệu, đối chiếu dữ liệu 04 §5.7, §6 và SRS BR01–12 |
| SDK / IDE | .NET SDK 10.0.403; Visual Studio 18.11.12328.437 |
| SQL Server đã kết nối | (localdb)\\MSSQLLocalDB, SQL Server 2025 Express LocalDB 17.0.4025.3 |
| CSDL phát triển | CafeManager_Group6_Dev; xác nhận chưa tồn tại trước lần tạo, không reset/drop CSDL cũ |
| Build toàn solution | 5 project thành công, 0 warning, 0 error; dotnet build CafeManager.sln --no-restore |
| Migration đã tạo và áp dụng | 20261108055627_InitialSchema; 20261108055637_IntegrityGuards; 20261108060223_HistoryProtection; 20261110082113_SchemaGapColumns (thêm cột/bảng, áp dụng 10/11/2026) |
| So sánh model/migration | dotnet ef migrations has-pending-model-changes: không có thay đổi chưa đưa vào migration |
| Schema thực | 40 bảng nghiệp vụ + 4 Identity + 1 lịch sử migration = 45; 68 FK NO ACTION, 52 CHECK, 13 trigger; 8 cột RowVersion (số liệu CSDL Dev sau migration thứ 4, 10/11/2026) |
| Dữ liệu mẫu | 1 Category, 3 Product/Variant, 2 Ingredient, công thức/giá, 3 CafeTable, 1 Counter; seed đã chạy lại; 0 AspNetUsers ở CSDL phát triển |
| Kiểm thử SQL Server | 34/34 Passed, 0 Failed, 0 Skipped (theo TRX 10/11/2026 sau migration thứ 4 và lệnh tài khoản mẫu; lần chạy 08/11/2026 là 29/29); exit 0 |
| CSDL kiểm thử | Fixture tự tạo CafeManager_Group6_Test_<GUID>, áp dụng 4 migration từ schema rỗng; cleanup hoàn tất; truy vấn sau chạy chỉ còn CafeManager_Group6_Dev với tiền tố dự án |
| Audit gói phụ thuộc | dotnet list CafeManager.sln package --vulnerable --include-transitive: không có gói được báo có lỗ hổng theo nguồn NuGet tại thời điểm kiểm tra |

Lần restore ban đầu thất bại NU1902/NU1903 cho phụ thuộc Pkcs 9.0.11. Đã thêm tham chiếu Pkcs 10.0.14, restore/build/audit cuối đều thành công. Lệnh migration không đặt Development bị guard từ chối trước khi thay đổi dữ liệu; lần chạy đúng Development áp dụng thành công. Không xem các lần lỗi là kết quả đạt.

## Đối chiếu tối thiểu 12 nhóm yêu cầu kiểm thử

| # | Nhóm yêu cầu | Bằng chứng và giới hạn |
|---|---|---|
| 1 | Migration từ đầu | Fixture tạo CSDL riêng và áp dụng 4 migration; kiểm tra 44 bảng mô hình, PK, trigger, RowVersion, trusted FK và model đồng bộ |
| 2 | PK/FK/UNIQUE/CHECK/khóa ghép | Chèn trùng RecipeDetail/OrderRound/CancellationItem; sai FK, xóa danh mục có tham chiếu; số lượng không dương đều bị SQL từ chối |
| 3 | Phiên/ca mở không trùng | Lệnh SQL và kiểm thử hai context cùng mở bàn; một phiên thắng. Ca mở trùng bị filtered UNIQUE chặn; chưa có handler mở/đóng ca hoàn chỉnh |
| 4 | Khoảng giá/công thức | INSERT chồng bị trigger chặn, giá cạnh tranh chỉ một commit, khoảng tiếp giáp hợp lệ; chưa stress concurrent update nhiều phiên bản/công thức |
| 5 | Snapshot cũ | Sửa tên/giá danh mục không làm đổi dòng bán; sửa định lượng công thức không đổi lượng dùng snapshot |
| 6 | Không giữ quá tồn | Hai lượt cùng cần 80 trên tồn 100 chỉ một thành công; thiếu tồn rollback phiếu, đơn, lượt, giữ; CHECK và RowVersion chặn ghi sai/cũ |
| 7 | Kênh đơn | Web thiếu khách hoặc có phiên bàn bị CHECK chặn; Table thiếu phiên bị chặn |
| 8 | Pha chế đồng lượt | Sửa Unit sang dòng lượt khác và sửa RoundId của Line đã có Unit bị trigger chặn |
| 9 | Chống trùng | Hai thu đồng thời cùng RequestId trả một ID; 30000/30000.00 cùng hash; khác nội dung lỗi; refund gửi lại trả ID cũ |
| 10 | Hoàn không vượt thu | Kiểm tra dịch vụ, lệnh SQL vượt hạn mức, hai hoàn cạnh tranh và chứng từ thành công bất biến |
| 11 | Đối soát ca | Opening + Cash Succeeded − Cash Refund Succeeded + OtherIn Posted − OtherOut Posted; loại BankTransfer/Voided; ca đóng từ chối khoản thu. Chưa có handler đóng ca cạnh tranh |
| 12 | Identity/hồ sơ | UserManager tạo/kiểm tra hash, FK hồ sơ và UNIQUE IdentityUserId; quyền qua bảng nghiệp vụ, nhân viên inactive mất quyền. Chưa kiểm thử UC18/19 qua giao diện/cookie |

Các ca về khóa đơn vị hủy, người duyệt độc lập, hủy cùng đơn, journal append-only, addon theo từng ly và seed không ghi đè cũng đã chạy. Tên ca cụ thể bên dưới.

## Bằng chứng lần chạy cuối

- Bắt đầu TRX: 2026-11-10T16:32:09+07:00.
- Kết thúc TRX: 2026-11-10T16:32:21+07:00.
- Lệnh: `dotnet test CafeManager.sln --no-build --no-restore --logger 'trx;LogFileName=database-tests.trx' --results-directory TestResults` với CAFE_TEST_SQLSERVER trỏ LocalDB, Windows Authentication.
- TRX local: `TestResults/database-tests.trx` (không track artifact sinh ra).
- SHA-256 TRX: `F5DF258CFFD46131B5C89784C92215CC3CF9D64D2D3FD20D31F76EDCB945D564`.
- Không dùng SQLite hay EF InMemory. Các ca cạnh tranh là cặp tác vụ kiểm thử xác định, không là bằng chứng load test hoặc NFR latency.

| Kiểm thử thực tế | Kết quả | Thời gian |
|---|---|---|
| Addon_quantity_is_per_unit_and_ingredients_are_snapshotted | Passed | 00:00:00.1110488 |
| Cancellation_units_cannot_cross_orders_and_decisions_are_immutable | Passed | 00:00:00.1142993 |
| Closed_shift_rejects_receipts | Passed | 00:00:00.0717863 |
| Compound_keys_and_positive_quantity_are_enforced | Passed | 00:00:00.0495552 |
| Concurrent_acceptance_cannot_overreserve_last_stock | Passed | 00:00:04.2328825 |
| Concurrent_open_table_allows_one_session | Passed | 00:00:00.0433752 |
| Concurrent_overlapping_prices_allow_only_one_commit | Passed | 00:00:00.0272056 |
| Concurrent_refunds_serialize_on_original_payment | Passed | 00:00:00.0543489 |
| Effective_price_and_recipe_intervals_cannot_overlap | Passed | 00:00:00.0305267 |
| Foreign_key_and_history_delete_are_rejected | Passed | 00:00:00.0254565 |
| Identity_profiles_are_unique_and_business_permissions_use_only_business_roles | Passed | 00:00:00.1033347 |
| Identity_UserManager_creates_hash_and_profiles_reference_same_user | Passed | 00:00:00.2648394 |
| Independent_approval_and_per_unit_cancellation_key_are_enforced | Passed | 00:00:00.0441982 |
| Insufficient_stock_rolls_back_entire_acceptance | Passed | 00:00:00.0933235 |
| Migration_creates_expected_schema_and_no_pending_model_changes | Passed | 00:00:00.2453456 |
| Only_one_open_session_and_one_open_shift | Passed | 00:00:00.0262406 |
| Order_channel_requires_customer_or_session_correctly | Passed | 00:00:00.0404294 |
| Payment_replay_returns_same_result_and_different_payload_is_rejected | Passed | 00:00:00.0449003 |
| Pending_round_has_no_ticket_then_accept_is_atomic_and_repeatable | Passed | 00:00:00.0735130 |
| Preparation_unit_cannot_reference_another_round_line | Passed | 00:00:00.0458848 |
| Reconciliation_counts_successful_cash_and_other_posted_entries_only | Passed | 00:00:01.9095908 |
| Refund_cannot_exceed_original_payment_even_across_requests | Passed | 00:00:00.0620770 |
| Refund_replay_preserves_result_and_request_history | Passed | 00:00:00.0691760 |
| Seed_can_repeat_and_does_not_overwrite_edited_demo_or_create_accounts | Passed | 00:00:00.1498636 |
| Snapshot_survives_menu_and_price_changes | Passed | 00:00:00.0304508 |
| Starting_unit_consumes_snapshot_once | Passed | 00:00:00.1180570 |
| Stock_check_and_rowversion_prevent_invalid_or_stale_write | Passed | 00:00:00.0309019 |
| Stock_movements_and_audits_are_append_only | Passed | 00:00:00.0539357 |
| Successful_payment_cannot_be_edited_or_deleted | Passed | 00:00:00.0476479 |

## Phần chưa thực hiện

Web/Desktop chỉ build khung. Chưa xác nhận giao diện thực chạy, thông tin tài khoản thực, liên kết end-to-end, quyền sở hữu đơn từ API, chốt bill/đóng đơn/ca, hủy và giải phóng kho/điều chỉnh tiền, thu chi khác chống trùng nguồn, luồng nhập/kiểm kê, batch traceability, ngân hàng, in ấn, tải hệ thống, backup/restore và production TLS. Xem [ma trận BR và quyết định cần chốt](business-rules.md). Không đánh dấu các ca KT/TC trong tài liệu gốc đã đạt chỉ vì có test dữ liệu liên quan.

