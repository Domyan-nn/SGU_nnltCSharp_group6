# Phân tích chuẩn hóa (1NF đến BCNF) của cơ sở dữ liệu

Ngày 16/11/2026. Đối tượng là CSDL `CafeManager_Group6_Dev` sau 4 migration: **40 bảng nghiệp vụ** (không tính 4 bảng ASP.NET Core Identity, vốn do framework định nghĩa, và `__EFMigrationsHistory`). Tài liệu này thay thế phần "chưa kết luận" của [relation-mapping.md](relation-mapping.md) và mục 4.2.5 của báo cáo bằng kết luận cho đủ 40 bảng.

> **Cập nhật 18/11/2026.** Lược đồ vẫn ở 4 migration nên kết luận bên dưới không đổi.
>
> - **Hướng 2 ở mục 4 đã được làm thử** trên nhánh `feat/db-3nf-preparation-unit` (PR #9, ngày 16/11). Kết quả: 105/105 test đạt; nâng cấp và hạ cấp trên một CSDL có đơn thật đều đúng.
> - **PR #9 đã đóng mà chưa gộp, và nhóm chưa quyết định** gộp hay giữ nguyên.
> - **Gộp thử vào `main` (017b7b0) ngày 18/11:** không có xung đột chữ, nhưng build lỗi 5 chỗ ở `StaffPreparationService.cs` (dòng 37, 64, 76, 86, 114). Lý do: API pha chế (A3, PR #11) viết sau ngày 16/11 vẫn đọc cột `PreparationUnit.TicketId`.
> - **Muốn gộp thì phải:** sửa 5 chỗ đó để lấy phiếu qua `OrderLine.RoundId`, chạy lại toàn bộ test (hiện có 148 ca), và mỗi máy chạy `--migrate` một lần.

## 1. Phương pháp và giới hạn

- **Nguồn cấu trúc:** danh mục hệ thống của SQL Server (`sys.tables`, `sys.columns`, `sys.indexes`, `sys.foreign_keys`, `sys.check_constraints`, `sys.triggers`) đọc trực tiếp từ CSDL Dev, đối chiếu với [schema.md](schema.md) và mã nguồn (`Entities.cs`, `CafeDataCommands.cs`). Tên 40 bảng trong bảng dưới khớp đúng với danh mục thực.
- **Khóa ứng viên** = khóa chính cộng các chỉ mục UNIQUE không lọc. Chỉ mục UNIQUE có lọc (một phiên mở trên một bàn, một ca mở trên một quầy, `Payment.ExternalReference`) chỉ là bất biến có điều kiện, không tính là khóa ứng viên của cả quan hệ.
- **Phụ thuộc hàm (FD)** được xác định từ nghiệp vụ và từ các ràng buộc (CHECK, trigger, mã dịch vụ). CSDL Dev gần như trống nên không thể chứng minh FD bằng dữ liệu; FD là giả định thiết kế, được bảo vệ bằng ràng buộc và bộ kiểm thử (105 ca ngày 16/11, 148 ca ngày 18/11). Vì vậy kết luận dưới đây là kết luận theo tập FD đã nêu, không phải chứng minh toán học rằng không còn FD nào khác.
- **Định nghĩa:** 1NF: mọi thuộc tính đơn trị, nguyên tử theo miền nghiệp vụ. 2NF: 1NF và không có thuộc tính không khóa phụ thuộc vào một phần của khóa ứng viên. 3NF: 2NF và không có phụ thuộc bắc cầu của thuộc tính không khóa vào khóa. BCNF: mọi định thức (vế trái của FD không tầm thường) đều là siêu khóa.

## 2. Kết quả tổng hợp

| Chuẩn | Số bảng đạt | Bảng không đạt |
|---|---|---|
| 1NF | 40/40 | không |
| 2NF | 39/40 | PreparationUnit |
| 3NF | 39/40 | PreparationUnit |
| BCNF | 39/40 | PreparationUnit |

**Kết luận:** 39 trong 40 bảng đạt BCNF (nên đạt cả 3NF). Một ngoại lệ là `PreparationUnit`, vi phạm 2NF và 3NF do hai phụ thuộc riêng phần; đây là phi chuẩn có chủ ý của thiết kế, được phân tích ở mục 4. Lược đồ vì vậy **chưa thể tuyên bố đạt 3NF toàn bộ** nếu không kèm giải thích ngoại lệ này; có hai hướng xử lý nêu ở mục 4.

## 3. Phân tích từng bảng

| # | Bảng | Khóa ứng viên | Phụ thuộc hàm | Chuẩn đạt được | Nhận xét |
|---|---|---|---|---|---|
| 1 | AllowedAddon | (ProductId, AddonVariantId) | (ProductId, AddonVariantId) → MaxPerUnit | BCNF | Thuộc tính không khóa phụ thuộc đủ khóa ghép. |
| 2 | AuditEvent | AuditId | AuditId → StaffId, ActionCode, EntityName, EntityReference, Reason, CreatedAt | BCNF | (EntityName, EntityReference) là tham chiếu đa hình, không có khóa ngoại; bảng chỉ ghi thêm (trigger). |
| 3 | CafeTable | TableId; TableCode | TableId ↔ TableCode; TableId → Status | BCNF | Hai khóa ứng viên xác định nhau. |
| 4 | CancellationItem | (CancellationId, UnitId) | Không có thuộc tính ngoài khóa | BCNF | Bảng toàn khóa; trigger bảo đảm đơn vị món và yêu cầu hủy thuộc cùng đơn. |
| 5 | CancellationRequest | CancellationId | CancellationId → OrderId, RequestedBy, ApprovedBy, Reason, Status, CreatedAt, ApprovedAt | BCNF | Status ràng buộc việc có hay không ApprovedBy, ApprovedAt bằng CHECK (ràng buộc, không phải phụ thuộc giá trị). |
| 6 | Category | CategoryId; CategoryName | CategoryId ↔ CategoryName; CategoryId → IsActive | BCNF |  |
| 7 | Counter | CounterId; CounterCode | CounterId ↔ CounterCode; CounterId → IsActive | BCNF |  |
| 8 | CustomerProfile | CustomerId; IdentityUserId | CustomerId ↔ IdentityUserId; CustomerId → FullName, Phone | BCNF | Tên đăng nhập và băm mật khẩu chỉ ở AspNetUsers, không sao chép. |
| 9 | IdempotencyRecord | RequestId | RequestId → RequestHash, ResultReference, CreatedAt | BCNF | ResultReference là chuỗi tham chiếu đa hình, không có khóa ngoại. |
| 10 | Ingredient | IngredientId | IngredientId → IngredientName, UnitName, IsActive | BCNF | UnitName chỉ là nhãn, không thuộc tính nào phụ thuộc nó nên không có phụ thuộc bắc cầu. IngredientName chưa UNIQUE. |
| 11 | IngredientBatch | BatchId | BatchId → IngredientId, QuantityReceived, ExpiryDate | BCNF | Bảng tùy chọn, chưa có mã dùng. |
| 12 | OrderLine | LineId | LineId → RoundId, VariantId, Quantity, ProductNameSnapshot, VariantNameSnapshot, UnitPriceSnapshot | BCNF | VariantId không xác định các cột Snapshot (tên và giá tại thời điểm bán) nên không có phụ thuộc bắc cầu. |
| 13 | OrderLineAddon | LineAddonId | LineAddonId → LineId, AddonVariantId, Quantity, AddonNameSnapshot, UnitPriceSnapshot | BCNF | Cùng lập luận Snapshot. (LineId, AddonVariantId) nên UNIQUE nhưng chưa khai báo. |
| 14 | OrderRound | RoundId; (OrderId, RoundNumber) | RoundId ↔ (OrderId, RoundNumber); → Status, AcceptedAt | BCNF | Status liên hệ AcceptedAt bằng CHECK. |
| 15 | Payment | PaymentId; PaymentCode; RequestId; ExternalReference (UNIQUE có lọc) | Mỗi khóa → OrderId, ShiftId, Amount, Method, Status, PaidAt, CreatedBy | BCNF | Mọi vế trái đều là khóa ứng viên. |
| 16 | Permission | PermissionId; PermissionCode | PermissionId ↔ PermissionCode | BCNF |  |
| 17 | PreparationAttempt | AttemptId; (UnitId, AttemptNo) | AttemptId ↔ (UnitId, AttemptNo); → Outcome, CreatedAt | BCNF |  |
| 18 | PreparationIngredient | (UnitId, IngredientId) | (UnitId, IngredientId) → QuantitySnapshot, QuantityReserved | BCNF | Cả hai thuộc tính phụ thuộc đủ khóa ghép; CHECK 0 ≤ giữ ≤ snapshot. |
| 19 | PreparationTicket | TicketId; RoundId | TicketId ↔ RoundId; → Status, CreatedAt | BCNF | Status là tổng hợp từ trạng thái các PreparationUnit (dữ liệu dẫn xuất liên bảng, duy trì bằng giao dịch). |
| 20 | PreparationUnit | UnitId; (LineId, UnitNumber) | UnitId ↔ (LineId, UnitNumber) → TicketId, Status, RecipeVersionId, DeliveredAt, DeliveredBy; thêm LineId → TicketId và LineId → RecipeVersionId | **Chỉ 1NF (không đạt 2NF, 3NF)** | LineId → TicketId và LineId → RecipeVersionId là phụ thuộc riêng phần vào khóa ứng viên (LineId, UnitNumber). Phi chuẩn có chủ ý, trigger TR_PreparationUnit_Round bảo vệ nhất quán; xem mục 4. |
| 21 | PriceHistory | PriceHistoryId; (VariantId, EffectiveFrom) | PriceHistoryId ↔ (VariantId, EffectiveFrom); → UnitPrice, EffectiveTo | BCNF | EffectiveTo suy ra được từ EffectiveFrom của bản kế tiếp (dẫn xuất liên dòng); trigger chống chồng khoảng. |
| 22 | Product | ProductId | ProductId → CategoryId, ProductName, IsActive | BCNF | ProductName chưa UNIQUE. |
| 23 | ProductVariant | VariantId | VariantId → ProductId, VariantName, VariantType, IsActive | BCNF | Giá tách sang PriceHistory; (ProductId, VariantName) chưa UNIQUE. |
| 24 | RecipeDetail | (RecipeVersionId, IngredientId) | (RecipeVersionId, IngredientId) → QuantityRequired | BCNF |  |
| 25 | RecipeVersion | RecipeVersionId; (VariantId, EffectiveFrom) | RecipeVersionId ↔ (VariantId, EffectiveFrom); → EffectiveTo | BCNF | Trigger chống chồng khoảng hiệu lực. |
| 26 | RefundRequest | RefundId; RefundCode | RefundId ↔ RefundCode; → PaymentId, AdjustmentId, Amount, Status, RequestedBy, ApprovedBy, ApprovedAt | BCNF |  |
| 27 | RefundTransaction | RefundTransactionId; RequestId | RefundTransactionId ↔ RequestId; → RefundId, ShiftId, Amount, Method, Status, ExternalReference, ExecutedAt, CreatedBy | BCNF |  |
| 28 | Role | RoleId | RoleId → RoleName | BCNF | RoleName chưa UNIQUE. |
| 29 | RolePermission | (RoleId, PermissionId) | Không có thuộc tính ngoài khóa | BCNF | Bảng toàn khóa. |
| 30 | SalesAdjustment | AdjustmentId | AdjustmentId → OrderId, AmountSigned, Reason, CreatedBy, CreatedAt | BCNF |  |
| 31 | SalesOrder | OrderId; OrderCode | OrderId ↔ OrderCode; → CustomerId, SessionId, OrderType, Status, CreatedAt, ExpectedPickupAt, ContactName, ContactPhone, PickupCode, RejectReason, RejectedAt, RejectedBy | BCNF | CustomerId không xác định ContactName, ContactPhone (người nhận món có thể khác hồ sơ). OrderType suy ra được từ việc SessionId có NULL hay không (CHECK). PickupCode chưa UNIQUE. |
| 32 | ServiceSession | SessionId | SessionId → CurrentTableId, OpenedAt, ClosedAt | BCNF | UNIQUE có lọc (một phiên mở trên một bàn) là bất biến có điều kiện, không phải khóa toàn bảng. CurrentTableId suy ra được từ lịch sử chuyển bàn. |
| 33 | Shift | ShiftId | ShiftId → CounterId, OpenedBy, OpenedAt, OpeningCash, CountedCash, ClosedBy, ClosedAt, CloseNote | BCNF | UNIQUE có lọc (một ca mở trên một quầy) là bất biến có điều kiện. |
| 34 | ShiftCashEntry | CashEntryId; CashEntryCode | CashEntryId ↔ CashEntryCode; → ShiftId, EntryType, Amount, Reason, CreatedBy, CreatedAt, Status | BCNF |  |
| 35 | StaffAccount | StaffId; IdentityUserId | StaffId ↔ IdentityUserId; → FullName, IsActive | BCNF |  |
| 36 | StaffRole | (StaffId, RoleId) | Không có thuộc tính ngoài khóa | BCNF | Bảng toàn khóa. |
| 37 | StockBalance | IngredientId | IngredientId → QuantityOnHand, QuantityReserved | BCNF | QuantityReserved là tổng hợp suy ra được từ các dòng giữ; lưu để chặn giữ quá tồn bằng một câu lệnh cập nhật có điều kiện. |
| 38 | StockCount | CountId | CountId → IngredientId, BookQty, CountedQty, CountedAt, CountedBy, ApprovedBy, Status | BCNF | BookQty là snapshot tồn sổ tại thời điểm kiểm kê. |
| 39 | StockMovement | MovementId; MovementCode | MovementId ↔ MovementCode; → IngredientId, MovementType, Quantity, SourceType, SourceId, CreatedAt | BCNF | (SourceType, SourceId) là tham chiếu đa hình, không có khóa ngoại; bảng chỉ ghi thêm. |
| 40 | TableTransferHistory | TransferId | TransferId → SessionId, FromTableId, ToTableId, TransferAt | BCNF | FromTableId suy ra được từ lần chuyển trước, giữ lại để lưu lịch sử. |

## 4. Ngoại lệ: PreparationUnit

`PreparationUnit` có hai khóa ứng viên là `UnitId` (khóa chính) và `(LineId, UnitNumber)` (UNIQUE). Các thuộc tính không khóa là `TicketId`, `Status`, `RecipeVersionId`, `RowVersion`, `DeliveredAt`, `DeliveredBy`.

- **`LineId → TicketId`:** mỗi dòng món thuộc đúng một lượt (`OrderLine.RoundId`), mỗi lượt có tối đa một phiếu (`PreparationTicket.RoundId` là UNIQUE), nên mọi đơn vị của cùng một dòng có cùng phiếu. Trigger `TR_PreparationUnit_Round` bắt buộc phiếu và dòng của một đơn vị cùng lượt.
- **`LineId → RecipeVersionId`:** khi tiếp nhận lượt, `AcceptRoundAsync` chọn một phiên bản công thức cho mỗi dòng (`CurrentRecipeAsync(line.VariantId, utcNow)`) rồi gán cho mọi đơn vị của dòng đó.
- `LineId` chỉ là một phần của khóa ứng viên `(LineId, UnitNumber)` và `TicketId`, `RecipeVersionId` là thuộc tính không khóa, nên đây là phụ thuộc riêng phần: vi phạm 2NF, và do vế trái không phải siêu khóa nên cũng vi phạm 3NF và BCNF.
- **Hệ quả:** dữ liệu `TicketId` và `RecipeVersionId` lặp lại ở mọi đơn vị của một dòng (ví dụ một dòng 20 ly lưu 20 lần). Nếu sửa tay một đơn vị sang phiếu khác, dữ liệu mâu thuẫn; trigger chỉ bắt được trường hợp khác lượt chứ không bắt được việc `RecipeVersionId` của các đơn vị trong một dòng khác nhau.

**Hai hướng xử lý** (nhóm chọn một và ghi vào báo cáo):

1. **Giữ và giải thích (phi chuẩn có kiểm soát).** Lý do: `TicketId` giúp truy vấn đơn vị theo phiếu không cần nối qua `OrderLine` và `OrderRound` (màn hình pha chế sau này), `RecipeVersionId` giữ vết công thức đã dùng cho từng đơn vị. Cần bổ sung một kiểm tra (trigger hoặc CHECK qua hàm) bảo đảm các đơn vị cùng dòng có cùng `RecipeVersionId`, và nêu rõ đây là đánh đổi có chủ ý.
2. **Chuẩn hóa.** Bỏ cột `TicketId` khỏi `PreparationUnit` (suy ra qua `OrderLine.RoundId` rồi `PreparationTicket.RoundId`), chuyển `RecipeVersionId` lên `OrderLine` (hoặc bảng `OrderLineRecipe(LineId, RecipeVersionId)`), bỏ trigger `TR_PreparationUnit_Round`. Cần một migration mới (thêm cột mới, chuyển dữ liệu, rồi bỏ cột cũ), sửa chỗ tạo đơn vị trong `CafeDataCommands.AcceptRoundAsync` (nơi duy nhất ghi `TicketId` và `RecipeVersionId` của `PreparationUnit` trong mã), cấu hình `PreparationUnit` trong `CafeDbContext`, và rà các test có tạo `PreparationUnit`. Sau đó cả 40 bảng đạt BCNF.

Khuyến nghị: hướng 2 nếu còn thời gian trước khi nộp (thầy chấm tiêu chí "đạt chuẩn 3"); hướng 1 nếu đã đóng băng mã, kèm giải thích trong báo cáo.

## 5. Phi chuẩn có chủ ý nhưng không vi phạm chuẩn

- **Snapshot lịch sử:** `OrderLine.ProductNameSnapshot`, `VariantNameSnapshot`, `UnitPriceSnapshot`, `OrderLineAddon.AddonNameSnapshot`, `UnitPriceSnapshot`, `PreparationIngredient.QuantitySnapshot`, `StockCount.BookQty`. Chúng giống thuộc tính của `ProductVariant`, `PriceHistory` hoặc `StockBalance` nhưng là giá trị **tại thời điểm giao dịch**; cùng một `VariantId` có thể có nhiều tên hoặc giá khác nhau trên các dòng khác nhau, nên `VariantId` không xác định chúng và không có phụ thuộc bắc cầu. Đây là yêu cầu nghiệp vụ (BR02: đổi thực đơn không sửa giao dịch cũ).
- **Dữ liệu dẫn xuất liên bảng hoặc liên dòng:** `StockBalance.QuantityReserved`, `PreparationTicket.Status`, `PriceHistory.EffectiveTo`, `ServiceSession.CurrentTableId`, `TableTransferHistory.FromTableId`, `SalesOrder.OrderType`. Chúng suy ra được từ dữ liệu khác nhưng không tạo FD trong nội bộ quan hệ nên không vi phạm 3NF; chúng được giữ vì hiệu năng hoặc kiểm soát đồng thời và phải nhất quán nhờ transaction, trigger và CHECK.
- **Cặp trạng thái và thời điểm:** `Status` với `ApprovedBy`, `ApprovedAt`, `DeliveredBy`, `DeliveredAt`, `RejectedBy` được ràng buộc bằng CHECK. `Status` không xác định giá trị của các cột này nên không phải FD.

## 6. Điểm yếu liên quan nhưng không thuộc chuẩn hóa

- **Khóa nghiệp vụ chưa khai báo UNIQUE:** `Role.RoleName`, `Ingredient.IngredientName`, `Product.ProductName`, `ProductVariant(ProductId, VariantName)`, `OrderLineAddon(LineId, AddonVariantId)`, `SalesOrder.PickupCode`. Nếu nghiệp vụ coi chúng duy nhất thì đó là khóa ứng viên chưa khai báo và FD tương ứng không được CSDL bảo vệ. Cần nhóm xác nhận rồi thêm bằng migration.
- **Tham chiếu đa hình không có khóa ngoại:** `StockMovement(SourceType, SourceId)`, `AuditEvent(EntityName, EntityReference)`, `IdempotencyRecord.ResultReference`. Chấp nhận được cho bảng nhật ký chỉ ghi thêm, đổi lại CSDL không bảo đảm tham chiếu tồn tại.
- **Ràng buộc liên bảng ngoài khả năng của FD:** `RefundRequest.PaymentId` và `AdjustmentId` phải cùng đơn hàng; `CancellationItem` phải cùng đơn với yêu cầu hủy. Một phần có trigger, phần còn lại do dịch vụ bảo đảm.

## 7. Kết luận và việc nên làm

1. Báo cáo mục 4.2.5 dùng bảng 40 dòng ở trên và nêu rõ ngoại lệ `PreparationUnit` (đã cập nhật cùng ngày).
2. Nhóm quyết định hướng 1 hay hướng 2 cho `PreparationUnit` (mục 4). Hướng 2 là migration mới, không được hạ hay xóa migration cũ. **Đến 18/11 vẫn chưa quyết**, và cần quyết trước giờ đóng băng mã (22:00 ngày 19/11).
   - Chọn hướng 2: làm thêm các việc nêu ở ghi chú đầu tài liệu.
   - Chọn hướng 1: thêm kiểm tra bảo đảm các đơn vị cùng một dòng có cùng `RecipeVersionId`. Theo mục 4, CSDL hiện chưa có kiểm tra này.
3. Thêm UNIQUE cho các khóa nghiệp vụ ở mục 6 sau khi xác nhận quy tắc.
4. Khi có migration mới, cập nhật lại bảng này bằng cách trích lại cấu trúc từ danh mục hệ thống.
5. Muốn trình bày mô hình gọn hơn mà không đổi CSDL: chia 30 bảng lõi và 10 bảng của bốn mô-đun mở rộng (số ngày 18/11; bản 16/11 là 25 và 15), xem [mo-hinh-du-lieu-rut-gon.md](mo-hinh-du-lieu-rut-gon.md).
