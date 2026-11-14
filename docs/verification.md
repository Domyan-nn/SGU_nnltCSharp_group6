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
| Kiểm thử SQL Server | 105/105 Passed, 0 Failed, 0 Skipped (theo TRX 14/11/2026: 34 test CSDL, 8 dịch vụ, 7 API qua HTTP, 15 máy khách API, 4 luồng Desktop D02/D06 qua ApiClient thật, 37 logic hiển thị/kiểm tra đầu vào Desktop); exit 0 |
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

- Bắt đầu TRX: 2026-11-14T14:23:51.5747391+07:00.
- Kết thúc TRX: 2026-11-14T14:24:11.6622144+07:00.
- Lệnh: `dotnet test CafeManager.sln --no-build --nologo --logger 'trx;LogFileName=database-tests.trx' --results-directory TestResults` với CAFE_TEST_SQLSERVER trỏ LocalDB, Windows Authentication.
- TRX local: `TestResults/database-tests.trx` (không track artifact sinh ra).
- SHA-256 TRX: `4D4FE08C06E3D6D912DADB87E53623A1948AC04DD353DF8E9795CBD414C475E0`.
- Kết quả: 105 Passed, 0 Failed, 0 Skipped (34 CSDL, 8 dịch vụ, 7 API qua HTTP, 15 máy khách API, 4 luồng Desktop qua ApiClient thật, 37 logic hiển thị và kiểm tra đầu vào của Desktop). Mã nguồn tại lúc chạy: nhánh feat/desktop-t6 (sau commit dabc415 của main).
- Không dùng SQLite hay EF InMemory. Các ca cạnh tranh là cặp tác vụ kiểm thử xác định, không là bằng chứng load test hoặc NFR latency.

| Kiểm thử thực tế | Kết quả | Thời gian |
|---|---|---|
| Accept_messages_distinguish_stock_state_unknown_and_permission | Passed | 00:00:00.0015296 |
| Addon_quantity_is_per_unit_and_ingredients_are_snapshotted | Passed | 00:00:00.2782646 |
| Allowed_addon_requires_addon_variant_and_positive_limit | Passed | 00:00:00.2325905 |
| Anonymous_calls_get_401_json_and_the_public_menu_is_open | Passed | 00:00:00.5635606 |
| Antiforgery_header_is_sent_only_when_a_token_is_set | Passed | 00:00:00.0010468 |
| Cancellation_units_cannot_cross_orders_and_decisions_are_immutable | Passed | 00:00:00.4603735 |
| Closed_shift_rejects_receipts | Passed | 00:00:00.1795603 |
| Compound_keys_and_positive_quantity_are_enforced | Passed | 00:00:00.2104708 |
| Concurrent_acceptance_cannot_overreserve_last_stock | Passed | 00:00:03.1083431 |
| Concurrent_open_table_allows_one_session | Passed | 00:00:00.1682612 |
| Concurrent_overlapping_prices_allow_only_one_commit | Passed | 00:00:02.9183289 |
| Concurrent_refunds_serialize_on_original_payment | Passed | 00:00:00.2539872 |
| Conflict_with_price_changes_is_parsed | Passed | 00:00:00.1056747 |
| Create_order_prices_on_server_snapshots_lines_and_is_idempotent | Passed | 00:00:00.8009060 |
| Create_order_rejects_price_change_bad_addon_hours_past_time_and_non_customers | Passed | 00:00:00.6214833 |
| Customers_see_only_their_orders_and_cancel_only_pending_ones | Passed | 00:00:00.8478778 |
| D02_cashier_lists_accepts_rejects_and_sees_cancelled_orders_through_the_real_client | Passed | 00:00:01.7909131 |
| D02_insufficient_stock_keeps_the_order_pending_with_a_clear_message | Passed | 00:00:02.9970077 |
| D06_is_forbidden_without_the_menu_permission_and_expired_sessions_raise_the_event | Passed | 00:00:00.5818584 |
| D06_manager_builds_a_sellable_item_step_by_step_and_it_reaches_the_public_menu | Passed | 00:00:02.1325439 |
| Dev_accounts_seed_creates_roles_permissions_and_is_repeatable | Passed | 00:00:00.5075366 |
| Each_client_has_its_own_cookie_container | Passed | 00:00:00.0019456 |
| Effective_price_and_recipe_intervals_cannot_overlap | Passed | 00:00:00.1481709 |
| Foreign_key_and_history_delete_are_rejected | Passed | 00:00:00.0976275 |
| Html_error_pages_fall_back_to_status_code_mapping(status: 400, code: "VALIDATION") | Passed | 00:00:00.0003342 |
| Html_error_pages_fall_back_to_status_code_mapping(status: 403, code: "FORBIDDEN") | Passed | 00:00:00.0000283 |
| Html_error_pages_fall_back_to_status_code_mapping(status: 404, code: "NOT_FOUND") | Passed | 00:00:00.0011922 |
| Html_error_pages_fall_back_to_status_code_mapping(status: 409, code: "CONFLICT") | Passed | 00:00:00.0000192 |
| Identity_profiles_are_unique_and_business_permissions_use_only_business_roles | Passed | 00:00:00.2033968 |
| Identity_UserManager_creates_hash_and_profiles_reference_same_user | Passed | 00:00:00.2320644 |
| Independent_approval_and_per_unit_cancellation_key_are_enforced | Passed | 00:00:00.1863212 |
| Insufficient_stock_rolls_back_entire_acceptance | Passed | 00:00:00.2235546 |
| Line_total_matches_server_formula_with_addons_per_unit | Passed | 00:00:00.0135254 |
| Login_distinguishes_wrong_password_lockout_and_account_kind | Passed | 00:00:00.9673354 |
| Login_errors_lockout_and_logout | Passed | 00:00:02.4499273 |
| Login_succeeds_for_staff_and_sends_camel_case_json | Passed | 00:00:00.0071392 |
| Login_with_customer_account_is_rejected_and_logged_out | Passed | 00:00:00.0015874 |
| Menu_lists_only_sellable_drinks_with_allowed_addons | Passed | 00:00:00.5051549 |
| Menu_management_requires_price_and_recipe_before_selling_and_rejects_overlaps | Passed | 00:00:02.4720627 |
| Menu_messages_explain_overlap_and_unknown | Passed | 00:00:00.0006873 |
| Menu_rows_show_why_a_variant_cannot_be_sold_and_which_actions_apply | Passed | 00:00:00.0090128 |
| Menu_search_matches_product_variant_and_category_without_diacritics(search: "  ", expected: 7) | Passed | 00:00:00.0003858 |
| Menu_search_matches_product_variant_and_category_without_diacritics(search: "ca phe", expected: 5) | Passed | 00:00:00.0000170 |
| Menu_search_matches_product_variant_and_category_without_diacritics(search: "khong co", expected: 0) | Passed | 00:00:00.0000415 |
| Menu_search_matches_product_variant_and_category_without_diacritics(search: "tra dao", expected: 1) | Passed | 00:00:00.0000146 |
| Menu_search_matches_product_variant_and_category_without_diacritics(search: "TRÀ", expected: 2) | Passed | 00:00:00.0158341 |
| Migration_creates_expected_schema_and_no_pending_model_changes | Passed | 00:00:00.5035309 |
| Names_and_reasons_are_trimmed_required_and_length_limited | Passed | 00:00:00.0084183 |
| Network_failure_is_unknown | Passed | 00:00:00.0012244 |
| Only_one_open_session_and_one_open_shift | Passed | 00:00:00.1001305 |
| Order_channel_requires_customer_or_session_correctly | Passed | 00:00:00.1350568 |
| Order_flow_create_price_change_replay_accept_and_cancel_rules | Passed | 00:00:01.9480084 |
| Order_rejection_fields_are_all_or_nothing_and_need_rejected_status | Passed | 00:00:00.1280751 |
| Other_calls_with_401_raise_session_expired | Passed | 00:00:00.0011881 |
| Payment_external_reference_is_unique_when_present | Passed | 00:00:00.1513519 |
| Payment_replay_returns_same_result_and_different_payload_is_rejected | Passed | 00:00:00.2128543 |
| Pending_round_has_no_ticket_then_accept_is_atomic_and_repeatable | Passed | 00:00:00.2064158 |
| Permissions_are_enforced_on_the_server_and_writes_need_an_antiforgery_token | Passed | 00:00:01.1353498 |
| Picked_local_time_is_sent_as_utc | Passed | 00:00:00.0034757 |
| Preparation_unit_cannot_reference_another_round_line | Passed | 00:00:00.2750169 |
| Price_accepts_vietnamese_number_formats(text: " 12000 ", expected: 12000) | Passed | 00:00:00.0006277 |
| Price_accepts_vietnamese_number_formats(text: "0", expected: 0) | Passed | 00:00:00.0000022 |
| Price_accepts_vietnamese_number_formats(text: "1.250.000", expected: 1250000) | Passed | 00:00:00.0157286 |
| Price_accepts_vietnamese_number_formats(text: "35.000", expected: 35000) | Passed | 00:00:00.0000029 |
| Price_accepts_vietnamese_number_formats(text: "35.000,5", expected: 35000,5) | Passed | 00:00:00.0001728 |
| Price_accepts_vietnamese_number_formats(text: "35000", expected: 35000) | Passed | 00:00:00.0000106 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "", messageStart: "Nhập giá.") | Passed | 00:00:00.0002420 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "-5000", messageStart: "Giá không được âm.") | Passed | 00:00:00.0000101 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "10,123", messageStart: "Giá không hợp lệ") | Passed | 00:00:00.0004536 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "100.000.001", messageStart: "Giá tối đa") | Passed | 00:00:00.0000206 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "35.00", messageStart: "Giá không hợp lệ") | Passed | 00:00:00.0000077 |
| Price_rejects_bad_input_with_a_message_near_the_field(text: "abc", messageStart: "Giá không hợp lệ") | Passed | 00:00:00.0000142 |
| Quantity_accepts_comma_decimals_and_dot_thousands(text: "0,5", expected: 0,5) | Passed | 00:00:00.0000033 |
| Quantity_accepts_comma_decimals_and_dot_thousands(text: "1.000", expected: 1000) | Passed | 00:00:00.0002505 |
| Quantity_accepts_comma_decimals_and_dot_thousands(text: "12,125", expected: 12,125) | Passed | 00:00:00.0000130 |
| Quantity_accepts_comma_decimals_and_dot_thousands(text: "20", expected: 20) | Passed | 00:00:00.0083694 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "") | Passed | 00:00:00.0001148 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "-3") | Passed | 00:00:00.0000014 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "0") | Passed | 00:00:00.0000074 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "0.5") | Passed | 00:00:00.0000033 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "1,2345") | Passed | 00:00:00.0001787 |
| Quantity_rejects_ambiguous_zero_negative_and_too_precise_values(text: "1.000.001") | Passed | 00:00:00.0000051 |
| Reconciliation_counts_successful_cash_and_other_posted_entries_only | Passed | 00:00:02.2893519 |
| Refund_cannot_exceed_original_payment_even_across_requests | Passed | 00:00:00.2435416 |
| Refund_replay_preserves_result_and_request_history | Passed | 00:00:00.4932031 |
| Register_creates_account_with_profile_and_rejects_duplicates_and_bad_input | Passed | 00:00:00.2809807 |
| Register_customer_over_http_then_login | Passed | 00:00:00.3748327 |
| Reject_unknown_result_warns_not_to_repeat_blindly | Passed | 00:00:00.0092249 |
| Reload_happens_after_success_unknown_not_found_and_conflict_but_not_validation | Passed | 00:00:00.0005484 |
| Revalidation_fails_after_staff_is_deactivated_or_user_is_locked | Passed | 00:00:00.5423973 |
| Revoked_staff_loses_the_session_on_the_next_request | Passed | 00:00:00.7639407 |
| Search_ignores_case_and_vietnamese_diacritics | Passed | 00:00:00.0001469 |
| Seed_can_repeat_and_does_not_overwrite_edited_demo_or_create_accounts | Passed | 00:00:00.2985810 |
| Server_error_is_unknown_not_a_definite_failure | Passed | 00:00:00.0009291 |
| Snapshot_survives_menu_and_price_changes | Passed | 00:00:00.1642142 |
| Staff_accepts_reserving_stock_rejects_with_reason_and_stock_shortage_leaves_order_pending | Passed | 00:00:03.0817283 |
| Staff_login_fetches_the_antiforgery_token_and_sends_it_on_later_writes | Passed | 00:00:00.0133421 |
| Starting_unit_consumes_snapshot_once | Passed | 00:00:00.3009677 |
| Stock_check_and_rowversion_prevent_invalid_or_stale_write | Passed | 00:00:00.1115006 |
| Stock_movements_and_audits_are_append_only | Passed | 00:00:00.1774168 |
| Successful_payment_cannot_be_edited_or_deleted | Passed | 00:00:00.1964198 |
| Timeout_is_unknown_but_caller_cancellation_is_propagated | Passed | 00:00:00.1111418 |
| Times_are_shown_in_vietnam_time_and_money_in_dong | Passed | 00:00:00.0040281 |
| Unit_delivery_fields_are_paired_and_need_delivered_status | Passed | 00:00:00.3117306 |
| Wrong_password_is_401_without_session_expired_event_and_not_unknown | Passed | 00:00:00.0011615 |

## Kiểm chứng HTTPS cục bộ (13/11/2026)

Phạm vi: máy chủ Web trên máy phát triển, không phải máy chủ thật. Cách làm: tạo CSDL tạm `CafeManager_Scratch_https`, áp dụng 4 migration, chạy Web từ cùng một bản build ở hai chế độ rồi gọi bằng script; sau khi chạy đã xóa CSDL tạm, `CafeManager_Group6_Dev` không bị chạm. Chế độ Development nghe `https://localhost:7096` và `http://localhost:5097` (`ASPNETCORE_HTTPS_PORT=7096`) bằng chứng chỉ dev của ASP.NET (CN=localhost, SHA-1 `4D2D0416B2E00E6A6237F62DCA10BBADEB8EF01C`, hiệu lực 08/11/2026–08/11/2027). Chế độ Production nghe `http://localhost:5098`. Script gọi HTTPS ở chế độ không xác minh chứng chỉ (chỉ trên loopback, tới máy chủ tạm); việc Windows có tin chứng chỉ hay không được kiểm riêng ở dòng cuối bảng.

| Hạng mục | Kết quả | Bằng chứng |
|---|---|---|
| Giao thức TLS | Đạt | TLSv1.3, TLS_AES_256_GCM_SHA384; máy chủ dùng đúng chứng chỉ dev nêu trên |
| `GET /api/menu` qua HTTPS | Đạt | 200 |
| HTTP chuyển sang HTTPS | Đạt | `GET` HTTP trả 307 tới `https://localhost:7096/...`; `POST /api/auth/login` qua HTTP cũng trả 307, không xử lý qua HTTP |
| Đăng ký và đăng nhập khách qua HTTPS | Đạt | 201 và 200 |
| Cờ cookie `.CafeManager.Auth` (Development, HTTPS) | Đạt | `path=/; secure; samesite=lax; httponly` |
| Cờ cookie ở chế độ Production | Đạt | cùng các cờ; `secure` có cả khi yêu cầu đến qua HTTP (`CookieSecurePolicy.Always`) |
| Phiên và antiforgery | Đạt | có cookie `GET /api/auth/me` trả 200, không cookie trả 401; ghi không có token trả 400; `logout` trả 204 không cần token (theo hợp đồng đã chốt) |
| Windows tin chứng chỉ dev (cách .NET/Desktop nhìn thấy) | **Không đạt** | `Invoke-WebRequest https://localhost:7096/api/menu` báo "Could not establish trust relationship for the SSL/TLS secure channel". `ApiClient` của Desktop (mặc định `https://localhost:7034`) sẽ lỗi SSL cho tới khi chạy `dotnet dev-certs https --trust` trên từng máy; chưa chạy lại sau khi trust |

Script tự động cho 15/15 mục đầu đạt (các dòng TLS, chứng chỉ, HTTPS, chuyển hướng, đăng ký, đăng nhập, cờ cookie, phiên, antiforgery, Production); dòng cuối chạy riêng. Script nằm ở thư mục tạm của phiên làm việc, chưa đưa vào repo.

Chưa kiểm chứng:
- Máy chủ thật: tên miền, chứng chỉ do CA cấp, reverse proxy và `ForwardedHeaders`.
- Production chỉ chạy HTTP nên ghi cảnh báo "Failed to determine the https port for redirect" và không chuyển hướng; triển khai thật cần gắn HTTPS vào Kestrel hoặc cấu hình `ForwardedHeaders` khi có proxy.
- Chưa bật HSTS (`UseHsts`).
- `AllowedHosts` trong `appsettings.json` đang là `localhost`: chạy bằng tên miền khác sẽ bị trả 400 cho tới khi đổi.
- Chưa chạy ứng dụng Desktop thực sự qua HTTPS; mới kiểm gián tiếp qua việc Windows có tin chứng chỉ hay không.

## Kiểm thử giao diện Desktop D02 và D06 (14/11/2026)

Phạm vi: các form Windows Forms `OrderReceivingForm` (D02) và `MenuManagementForm` (D06) chạy thật, gọi máy chủ Web thật (Kestrel, `http://localhost:5099`, Development) trên CSDL tạm `CafeManager_Scratch_desktop` (4 migration, dữ liệu mẫu nhỏ, đã xóa sau khi chạy; `CafeManager_Group6_Dev` không bị chạm). Một chương trình điều khiển tạm (WinExe .NET 10 tham chiếu project Desktop) mở form, bấm nút bằng `PerformClick`, tự trả lời hộp thoại xác nhận/cảnh báo và hộp thoại nhập liệu, chụp ảnh form, rồi so với kỳ vọng. Kết quả: **22/22 mục đạt**.

| Luồng | Quan sát |
|---|---|
| D02 mở, đăng nhập thu ngân | hiện 3 đơn chờ với mã, giờ đặt/nhận, khách, tổng; chi tiết dòng món có tùy chọn; nút Tiếp nhận/Từ chối bật khi chọn đơn |
| D02 tiếp nhận | hộp xác nhận nêu mã đơn; đơn rời danh sách; thanh trạng thái "Đã tiếp nhận…Còn 2 đơn chờ" |
| D02 từ chối | lý do trống bị chặn ngay trong hộp thoại; có lý do thì thành công; CSDL ghi `Rejected` kèm lý do |
| D02 khách hủy trước khi tiếp nhận | cảnh báo "Khách đã hủy đơn này", danh sách tải lại và rỗng |
| D06 danh sách | hiện món đang bán với giá, hiệu lực, công thức, trạng thái |
| D06 thêm món/biến thể | tên trống bị chặn; món mới "Chưa có biến thể"; biến thể mới "Chưa bán: thiếu giá và công thức", nút Bật bán tắt |
| D06 giá/công thức | giá âm và chữ bị chặn; định lượng "0.5" (mơ hồ) bị chặn với lỗi gần dòng; 35.000 và 15 được nhận; trạng thái chuyển dần tới "sẵn sàng bật bán" |
| D06 bật/ngừng bán | sau Bật bán món có trong `GET /api/menu` giá 35000; Ngừng bán (có xác nhận nêu tên món) làm món biến mất khỏi menu công khai; ngày hiệu lực quá khứ bị máy chủ từ chối kèm cảnh báo |
| D06 tìm kiếm | gõ "tra dao" (không dấu) lọc ra món "Trà đào" |

Giới hạn (không thay thế KT01–KT19 chạy tay): nút mở D02/D06 trên màn hình chính `Form1`, việc tự đóng cửa sổ con khi hết phiên (KT15), bộ hẹn giờ tự làm mới 5 giây và đăng nhập D00 chưa được điều khiển tự động; chạy bằng HTTP, một lần, một máy, độ phân giải/DPI mặc định; chương trình điều khiển và ảnh chụp không nằm trong repo. Hộp thoại do chương trình điều khiển trả lời, nên cách trình bày thật với người dùng cần kiểm bằng mắt khi chạy tay. Luồng dữ liệu bên dưới các form cũng có test tự động `DesktopFlowTests` (4 ca) và `StaffPresentationTests` (37 ca) trong bộ 105 ca.

## Phần chưa thực hiện

Chưa kiểm thử tay có biên bản và ảnh cho các ca KT/TC trong tài liệu 06 (xem [kiem-thu-tay.md](kiem-thu-tay.md) để chạy). Giao diện Web (B) và Desktop D02/D06 đã có mã và được kiểm tự động như các mục trên nhưng chưa nghiệm thu bằng người dùng; chưa có giao diện cho thu tiền/đóng ca (UC08/14), pha chế/bàn (UC05–07, 09), hủy/hoàn trọn vẹn (UC10–12), nhập/kiểm kê (UC13), báo cáo và quản trị (UC15/16), ngân hàng, in ấn; chưa đo tải hệ thống, backup/restore; production TLS mới kiểm cục bộ (xem mục HTTPS). Xem [ma trận BR và quyết định cần chốt](business-rules.md). Không đánh dấu các ca KT/TC trong tài liệu gốc đã đạt chỉ vì có test dữ liệu liên quan.
