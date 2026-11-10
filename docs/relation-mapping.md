# Đối chiếu 40 quan hệ và chuẩn hóa

Đọc tài liệu 04, mục 5.7, 6.1 và 6.2 ngày 08/11/2026; xem [nguồn](sources.md). 40 entity nghiệp vụ (39 theo tài liệu và bảng mở rộng AllowedAddon) nằm trong `src/CafeManager.Core/Entities.cs`, cấu hình trong `CafeDbContext.cs`. Tên entity = tên bảng `dbo`; giữ nguyên tên PK/FK của từ điển. Tất cả 40 quan hệ dưới đây có trong migration `InitialSchema`; `IntegrityGuards` và `HistoryProtection` bổ sung bảo vệ liên bảng/lịch sử.

| # | Quan hệ / entity / bảng | PK | Khóa thay thế được khai báo | Trạng thái / khác biệt |
|---|---|---|---|---|
| 1 | Category | CategoryId | CategoryName | Danh mục thực đơn |
| 2 | Product | ProductId | Không giả định thêm | Món hiện hành |
| 3 | ProductVariant | VariantId | Không giả định thêm | Biến thể đồ uống hoặc tùy chọn |
| 4 | PriceHistory | PriceHistoryId | VariantId+EffectiveFrom | Giá theo khoảng nửa mở [từ, đến) |
| 5 | CafeTable | TableId | TableCode | Tình trạng bàn |
| 6 | CustomerProfile | CustomerId | IdentityUserId | Liên hệ khách, không chứa thông tin xác thực |
| 7 | ServiceSession | SessionId | CurrentTableId (có lọc, không là khóa toàn bảng) | Phiên phục vụ hiện tại và lịch sử |
| 8 | TableTransferHistory | TransferId | Không giả định thêm | Lịch sử chuyển bàn |
| 9 | SalesOrder | OrderId | OrderCode; SessionId (có lọc, không là khóa toàn bảng) | Đơn theo kênh, không lưu tổng tiền suy diễn |
| 10 | OrderRound | RoundId | OrderId+RoundNumber | Lượt gọi trong đơn |
| 11 | OrderLine | LineId | Không giả định thêm | Nhóm đơn vị có cùng biến thể, giá và tùy chọn |
| 12 | OrderLineAddon | LineAddonId | Không giả định thêm | Tùy chọn cho MỖI đơn vị món |
| 13 | PreparationTicket | TicketId | RoundId | Một phiếu cho lượt tiếp nhận |
| 14 | PreparationUnit | UnitId | LineId+UnitNumber | Tiến độ từng đơn vị món |
| 15 | PreparationAttempt | AttemptId | UnitId+AttemptNo | Từng lần thực hiện/làm lại |
| 16 | PreparationIngredient | UnitId + IngredientId | Không giả định thêm | Định lượng đã chốt, gồm nguyên liệu tùy chọn được gộp |
| 17 | Counter | CounterId | CounterCode | Quầy |
| 18 | Shift | ShiftId | CounterId (có lọc, không là khóa toàn bảng) | Ca quầy và số kiểm đếm |
| 19 | Payment | PaymentId | PaymentCode; RequestId | Chứng từ thu |
| 20 | SalesAdjustment | AdjustmentId | Không giả định thêm | Điều chỉnh nghĩa vụ có dấu |
| 21 | RefundRequest | RefundId | RefundCode | Đề nghị và phê duyệt hoàn |
| 22 | RefundTransaction | RefundTransactionId | RequestId | Kết quả hoàn tách khỏi phê duyệt |
| 23 | Ingredient | IngredientId | Không giả định thêm | Nguyên liệu theo đơn vị chuẩn |
| 24 | RecipeVersion | RecipeVersionId | VariantId+EffectiveFrom | Phiên bản công thức theo hiệu lực |
| 25 | RecipeDetail | RecipeVersionId + IngredientId | Không giả định thêm | Định lượng cho một đơn vị biến thể |
| 26 | IngredientBatch | BatchId | Không giả định thêm | Thông tin lô tùy chọn; CHƯA truy xuất tiêu hao theo lô |
| 27 | StockBalance | IngredientId | Không giả định thêm | Tổng hợp một kho |
| 28 | StockMovement | MovementId | MovementCode | Sổ biến động không sửa/xóa |
| 29 | StockCount | CountId | Không giả định thêm | Biên bản kiểm đếm và người duyệt |
| 30 | StaffAccount | StaffId | IdentityUserId | Hồ sơ nhân viên, không chứa mật khẩu |
| 31 | Role | RoleId | Không giả định thêm | Vai trò nghiệp vụ |
| 32 | Permission | PermissionId | PermissionCode | Mã hành động |
| 33 | StaffRole | StaffId + RoleId | Không giả định thêm | Phân vai nhân viên |
| 34 | RolePermission | RoleId + PermissionId | Không giả định thêm | Cấp quyền cho vai trò |
| 35 | AuditEvent | AuditId | Không giả định thêm | Nhật ký chỉ ghi thêm |
| 36 | IdempotencyRecord | RequestId | Không giả định thêm | Mã yêu cầu, dấu băm và tham chiếu kết quả |
| 37 | CancellationRequest | CancellationId | Không giả định thêm | Đề nghị và quyết định hủy |
| 38 | CancellationItem | CancellationId + UnitId | Không giả định thêm | Mỗi dòng là một đơn vị món bị đề nghị hủy |
| 39 | ShiftCashEntry | CashEntryId | CashEntryCode | Thu/chi tiền mặt khác ngoài bán hàng |
| 40 | AspNetUsers / IdentityUser | Id | NormalizedUserName có lọc khác NULL | Framework quản lý xác thực |

IdentityUserContext sinh thêm `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`: tổng 44 bảng mô hình (gồm AllowedAddon). `__EFMigrationsHistory` là bảng kỹ thuật migration thứ 45. Không tạo AspNetRoles, AspNetUserRoles, AspNetRoleClaims; không cấu hình Identity role store. Claims không được dùng làm nguồn quyền nghiệp vụ. Quyền tra từ StaffRole → RolePermission → Permission qua StaffPermissionReader; các API sau này phải gọi cơ chế này.

## Rà 1NF, 2NF, 3NF từng quan hệ

1NF trên mô hình: mỗi cột là một giá trị; tùy chọn, nguyên liệu, vai trò/quyền được tách thành bảng dòng. Văn bản Reason không được dùng làm danh sách FK. Cần xác nhận lại giả định đơn trị khi nhóm mở rộng nghiệp vụ. PK xác định toàn bộ thuộc tính được liệt kê trong [từ điển triển khai](schema.md), kể cả RowVersion kỹ thuật; dấu `PK → còn lại` dưới đây dùng đúng nghĩa đó.

| Quan hệ | Phụ thuộc hàm và kiểm tra 2NF | Nhận xét 3NF / giả định cần xác nhận |
|---|---|---|
| Category | CategoryId → CategoryName, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| Product | ProductId → CategoryId, ProductName, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| ProductVariant | VariantId → ProductId, VariantName, VariantType, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| PriceHistory | PriceHistoryId → VariantId, UnitPrice, EffectiveFrom, EffectiveTo. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | (VariantId,EffectiveFrom) là khóa thay thế NOT NULL + UNIQUE. Giá/công thức theo thời gian; không giả định VariantId xác định một bản hiện hành. Không chồng khoảng là bất biến riêng do trigger kiểm tra. |
| CafeTable | TableId → TableCode, Status. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| CustomerProfile | CustomerId → IdentityUserId, FullName, Phone. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | IdentityUserId NOT NULL + UNIQUE là khóa ứng viên. UserName/PasswordHash chỉ ở AspNetUsers, không tạo phụ thuộc bắc cầu sao chép. |
| ServiceSession | SessionId → CurrentTableId, OpenedAt, ClosedAt, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| TableTransferHistory | TransferId → SessionId, FromTableId, ToTableId, TransferAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| SalesOrder | OrderId → OrderCode, CustomerId, SessionId, OrderType, Status, CreatedAt, ExpectedPickupAt, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| OrderRound | RoundId → OrderId, RoundNumber, Status, AcceptedAt, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| OrderLine | LineId → RoundId, VariantId, Quantity, ProductNameSnapshot, VariantNameSnapshot, UnitPriceSnapshot. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Snapshot là sự kiện lịch sử. KHÔNG giả định VariantId/AddonVariantId → tên hoặc giá snapshot; cùng biến thể có thể có nhiều tên/giá giao dịch theo thời gian. Không đồng bộ ngược danh mục. |
| OrderLineAddon | LineAddonId → LineId, AddonVariantId, Quantity, AddonNameSnapshot, UnitPriceSnapshot. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Snapshot là sự kiện lịch sử. KHÔNG giả định VariantId/AddonVariantId → tên hoặc giá snapshot; cùng biến thể có thể có nhiều tên/giá giao dịch theo thời gian. Không đồng bộ ngược danh mục. |
| PreparationTicket | TicketId → RoundId, Status, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| PreparationUnit | UnitId → TicketId, LineId, UnitNumber, Status, RecipeVersionId, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Có dư thừa có chủ đích theo thiết kế: LineId xác định RoundId và qua phiếu duy nhất xác định TicketId; LineId không là khóa của Unit. Vì vậy không khẳng định bảng này đạt 3NF theo FD đó. Trigger bảo vệ đồng lượt. RecipeVersionId thêm để truy vết công thức chính, dịch vụ dùng cùng phiên bản cho dòng; cần rà FD LineId → RecipeVersionId với nhóm. |
| PreparationAttempt | AttemptId → UnitId, AttemptNo, Outcome, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| PreparationIngredient | UnitId+IngredientId → QuantitySnapshot, QuantityReserved. Khóa ghép: các thuộc tính ngoài khóa giả định phụ thuộc toàn bộ cặp. | Thuộc tính lượng thuộc toàn cặp; tên nguyên liệu và thuộc tính cha không lưu lại. Giả định không có FD nghiệp vụ khác. |
| Counter | CounterId → CounterCode, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| Shift | ShiftId → CounterId, OpenedBy, OpenedAt, OpeningCash, CountedCash, ClosedBy, ClosedAt, CloseNote, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| Payment | PaymentId → PaymentCode, OrderId, ShiftId, Amount, Method, Status, PaidAt, RequestId, CreatedBy. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| SalesAdjustment | AdjustmentId → OrderId, AmountSigned, Reason, CreatedBy, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| RefundRequest | RefundId → RefundCode, PaymentId, AdjustmentId, Amount, Status, RequestedBy, ApprovedBy, ApprovedAt, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| RefundTransaction | RefundTransactionId → RefundId, ShiftId, Amount, Method, Status, ExternalReference, RequestId, ExecutedAt, CreatedBy. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| Ingredient | IngredientId → IngredientName, UnitName, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| RecipeVersion | RecipeVersionId → VariantId, EffectiveFrom, EffectiveTo. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | (VariantId,EffectiveFrom) là khóa thay thế NOT NULL + UNIQUE. Giá/công thức theo thời gian; không giả định VariantId xác định một bản hiện hành. Không chồng khoảng là bất biến riêng do trigger kiểm tra. |
| RecipeDetail | RecipeVersionId+IngredientId → QuantityRequired. Khóa ghép: các thuộc tính ngoài khóa giả định phụ thuộc toàn bộ cặp. | Thuộc tính lượng thuộc toàn cặp; tên nguyên liệu và thuộc tính cha không lưu lại. Giả định không có FD nghiệp vụ khác. |
| IngredientBatch | BatchId → IngredientId, QuantityReceived, ExpiryDate. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| StockBalance | IngredientId → QuantityOnHand, QuantityReserved, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Là tổng hợp vận hành, không phải sổ gốc; cần đối soát OnHand với Receipt/Consume/Waste/Adjustment, Reserved với Reserve/Release/Consume. Chuẩn hóa không chứng minh hai nguồn này luôn khớp. |
| StockMovement | MovementId → MovementCode, IngredientId, MovementType, Quantity, SourceType, SourceId, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| StockCount | CountId → IngredientId, BookQty, CountedQty, CountedAt, CountedBy, ApprovedBy, Status. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| StaffAccount | StaffId → IdentityUserId, FullName, IsActive. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | IdentityUserId NOT NULL + UNIQUE là khóa ứng viên. UserName/PasswordHash chỉ ở AspNetUsers, không tạo phụ thuộc bắc cầu sao chép. |
| Role | RoleId → RoleName. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| Permission | PermissionId → PermissionCode. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| StaffRole | StaffId+RoleId → (không có thuộc tính ngoài khóa). Khóa ghép: các thuộc tính ngoài khóa giả định phụ thuộc toàn bộ cặp. | Chỉ có thuộc tính khóa; không có thuộc tính không khóa phụ thuộc bộ phận/bắc cầu trong mô hình hiện tại. |
| RolePermission | RoleId+PermissionId → (không có thuộc tính ngoài khóa). Khóa ghép: các thuộc tính ngoài khóa giả định phụ thuộc toàn bộ cặp. | Chỉ có thuộc tính khóa; không có thuộc tính không khóa phụ thuộc bộ phận/bắc cầu trong mô hình hiện tại. |
| AuditEvent | AuditId → StaffId, ActionCode, EntityName, EntityReference, Reason, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| IdempotencyRecord | RequestId → RequestHash, ResultReference, CreatedAt. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| CancellationRequest | CancellationId → OrderId, RequestedBy, ApprovedBy, Reason, Status, CreatedAt, ApprovedAt, RowVersion. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| CancellationItem | CancellationId+UnitId → (không có thuộc tính ngoài khóa). Khóa ghép: các thuộc tính ngoài khóa giả định phụ thuộc toàn bộ cặp. | Chỉ có thuộc tính khóa; không có thuộc tính không khóa phụ thuộc bộ phận/bắc cầu trong mô hình hiện tại. |
| ShiftCashEntry | CashEntryId → CashEntryCode, ShiftId, EntryType, Amount, Reason, CreatedBy, CreatedAt, Status. Khóa PK đơn: không có phụ thuộc bộ phận vào PK; vẫn phải xét các khóa thay thế. | Không ghi tên của bảng cha; đối với các FD đã nêu, chưa thấy phụ thuộc bắc cầu khác. Đây là kết luận có điều kiện, không chứng minh tập FD đã đầy đủ. |
| AspNetUsers | Id → thuộc tính Identity; NormalizedUserName chỉ UNIQUE khi khác NULL, chưa coi là khóa ứng viên toàn quan hệ. | Framework có thuộc tính kỹ thuật; chưa chứng minh toàn bộ FD/3NF của Identity. |

Không tuyên bố toàn CSDL đạt 3NF. Đã kiểm tra PK, UNIQUE, FK và CHECK của lược đồ thực bằng SQL Server; việc kiểm tra ràng buộc không chứng minh tính đầy đủ của các FD. Tên vai trò, tên nguyên liệu và tên sản phẩm không mặc định duy nhất. Các chỉ mục có lọc cho phiên/ca/SessionId là bất biến có điều kiện, không là khóa ứng viên của toàn quan hệ.

