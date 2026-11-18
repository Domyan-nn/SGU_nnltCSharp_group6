# Phân tích chuẩn hóa (1NF đến BCNF) của cơ sở dữ liệu

Ngày 16/11/2026, cập nhật 18/11/2026. Đối tượng là CSDL `CafeManager_Group6_Dev` sau 5 migration (bản 16/11 tính sau 4 migration): **40 bảng nghiệp vụ** (không tính 4 bảng ASP.NET Core Identity, vốn do framework định nghĩa, và `__EFMigrationsHistory`). Tài liệu này thay thế phần "chưa kết luận" của [relation-mapping.md](relation-mapping.md) và mục 4.2.5 của báo cáo bằng kết luận cho đủ 40 bảng.

> **Cập nhật 18/11/2026: đã chọn hướng 2 (chuẩn hóa).** Sau migration thứ 5 `PreparationUnit3NF`, **cả 40/40 bảng đạt BCNF**. Bảng ở mục 2 và mục 3 đã sửa theo lược đồ mới; mục 4 giữ lại phân tích cũ để thấy vì sao phải đổi.
>
> - **Vì sao làm mới:** nhánh thử cũ (`feat/db-3nf-preparation-unit`, PR #9, ngày 16/11) không gộp thẳng được. Ngày 18/11, gộp thử vào `main` 017b7b0 thì build lỗi 5 chỗ ở `StaffPreparationService.cs`, vì API pha chế (A3) viết sau đó vẫn đọc `PreparationUnit.TicketId`. Nhóm quyết định bỏ nhánh cũ và làm mới từ `main` (nhánh `feat/a17-chuan-hoa-preparation-unit`), thay vì sửa nhánh cũ.
> - **Làm mới gồm:** migration do EF tạo lại theo model hiện tại; viết lại thứ tự `Up`/`Down` để không mất dữ liệu; sửa API pha chế để lấy phiếu qua `OrderLine.RoundId`; trigger lấy lại bản của PR #9 và chặt thêm một điều: không đổi được công thức của dòng khi dòng đã có ly.
> - **Kiểm chứng ngày 18/11:** 149/149 test đạt, trong đó có một ca mới kiểm các trigger thay khóa ngoại. Thử trên bản sao CSDL Dev có 63 ly, 2 phiếu: `Up` xong cả 63 ly suy ra đúng phiếu và công thức như trước; `Down` khôi phục đúng hai cột và 4 khóa ngoại; `Up` lần hai vẫn đúng.

## 1. Phương pháp và giới hạn

- **Nguồn cấu trúc:** danh mục hệ thống của SQL Server (`sys.tables`, `sys.columns`, `sys.indexes`, `sys.foreign_keys`, `sys.check_constraints`, `sys.triggers`) đọc trực tiếp từ CSDL Dev, đối chiếu với [schema.md](schema.md) và mã nguồn (`Entities.cs`, `CafeDataCommands.cs`). Tên 40 bảng trong bảng dưới khớp đúng với danh mục thực.
- **Khóa ứng viên** = khóa chính cộng các chỉ mục UNIQUE không lọc. Chỉ mục UNIQUE có lọc (một phiên mở trên một bàn, một ca mở trên một quầy, `Payment.ExternalReference`) chỉ là bất biến có điều kiện, không tính là khóa ứng viên của cả quan hệ.
- **Phụ thuộc hàm (FD)** được xác định từ nghiệp vụ và từ các ràng buộc (CHECK, trigger, mã dịch vụ). CSDL Dev gần như trống nên không thể chứng minh FD bằng dữ liệu; FD là giả định thiết kế, được bảo vệ bằng ràng buộc và bộ kiểm thử (105 ca ngày 16/11, 148 ca ngày 18/11). Vì vậy kết luận dưới đây là kết luận theo tập FD đã nêu, không phải chứng minh toán học rằng không còn FD nào khác.
- **Định nghĩa:** 1NF: mọi thuộc tính đơn trị, nguyên tử theo miền nghiệp vụ. 2NF: 1NF và không có thuộc tính không khóa phụ thuộc vào một phần của khóa ứng viên. 3NF: 2NF và không có phụ thuộc bắc cầu của thuộc tính không khóa vào khóa. BCNF: mọi định thức (vế trái của FD không tầm thường) đều là siêu khóa.

## 2. Kết quả tổng hợp

| Chuẩn | Số bảng đạt | Bảng không đạt |
|---|---|---|
| 1NF | 40/40 | không |
| 2NF | 40/40 | không |
| 3NF | 40/40 | không |
| BCNF | 40/40 | không |

**Kết luận:** cả 40 bảng đạt BCNF, nên đạt cả 3NF.
- Trước migration `PreparationUnit3NF` (bản 16/11), kết quả là 39/40. Ngoại lệ khi đó là `PreparationUnit`: bảng vi phạm 2NF và 3NF do hai phụ thuộc riêng phần `LineId → TicketId` và `LineId → RecipeVersionId` (xem mục 4).
- Migration đã bỏ hai cột đó và chuyển `RecipeVersionId` lên `OrderLine`.

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
| 12 | OrderLine | LineId | LineId → RoundId, VariantId, Quantity, ProductNameSnapshot, VariantNameSnapshot, UnitPriceSnapshot, RecipeVersionId | BCNF | VariantId không xác định các cột Snapshot (tên và giá tại thời điểm bán) nên không có phụ thuộc bắc cầu. VariantId cũng không xác định RecipeVersionId: một biến thể có nhiều phiên bản công thức theo thời gian, và dòng giữ phiên bản đã chốt lúc tiếp nhận (cột thêm ở migration PreparationUnit3NF). |
| 13 | OrderLineAddon | LineAddonId | LineAddonId → LineId, AddonVariantId, Quantity, AddonNameSnapshot, UnitPriceSnapshot | BCNF | Cùng lập luận Snapshot. (LineId, AddonVariantId) nên UNIQUE nhưng chưa khai báo. |
| 14 | OrderRound | RoundId; (OrderId, RoundNumber) | RoundId ↔ (OrderId, RoundNumber); → Status, AcceptedAt | BCNF | Status liên hệ AcceptedAt bằng CHECK. |
| 15 | Payment | PaymentId; PaymentCode; RequestId; ExternalReference (UNIQUE có lọc) | Mỗi khóa → OrderId, ShiftId, Amount, Method, Status, PaidAt, CreatedBy | BCNF | Mọi vế trái đều là khóa ứng viên. |
| 16 | Permission | PermissionId; PermissionCode | PermissionId ↔ PermissionCode | BCNF |  |
| 17 | PreparationAttempt | AttemptId; (UnitId, AttemptNo) | AttemptId ↔ (UnitId, AttemptNo); → Outcome, CreatedAt | BCNF |  |
| 18 | PreparationIngredient | (UnitId, IngredientId) | (UnitId, IngredientId) → QuantitySnapshot, QuantityReserved | BCNF | Cả hai thuộc tính phụ thuộc đủ khóa ghép; CHECK 0 ≤ giữ ≤ snapshot. |
| 19 | PreparationTicket | TicketId; RoundId | TicketId ↔ RoundId; → Status, CreatedAt | BCNF | Status là tổng hợp từ trạng thái các PreparationUnit (dữ liệu dẫn xuất liên bảng, duy trì bằng giao dịch). |
| 20 | PreparationUnit | UnitId; (LineId, UnitNumber) | UnitId ↔ (LineId, UnitNumber) → Status, DeliveredAt, DeliveredBy | BCNF | Trước migration PreparationUnit3NF bảng chỉ đạt 1NF vì có thêm TicketId, RecipeVersionId phụ thuộc riêng phần vào LineId (mục 4). Nay phiếu suy ra qua OrderLine.RoundId, công thức nằm ở OrderLine. |
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

## 4. Ngoại lệ cũ đã xử lý: PreparationUnit

Mục này phân tích lược đồ **trước** migration `PreparationUnit3NF` (bản 16/11), để thấy vì sao phải đổi. `PreparationUnit` khi đó có hai khóa ứng viên là `UnitId` (khóa chính) và `(LineId, UnitNumber)` (UNIQUE). Các thuộc tính không khóa là `TicketId`, `Status`, `RecipeVersionId`, `RowVersion`, `DeliveredAt`, `DeliveredBy`.

- **`LineId → TicketId`:** mỗi dòng món thuộc đúng một lượt (`OrderLine.RoundId`), mỗi lượt có tối đa một phiếu (`PreparationTicket.RoundId` là UNIQUE), nên mọi đơn vị của cùng một dòng có cùng phiếu. Trigger `TR_PreparationUnit_Round` bắt buộc phiếu và dòng của một đơn vị cùng lượt.
- **`LineId → RecipeVersionId`:** khi tiếp nhận lượt, `AcceptRoundAsync` chọn một phiên bản công thức cho mỗi dòng (`CurrentRecipeAsync(line.VariantId, utcNow)`) rồi gán cho mọi đơn vị của dòng đó.
- `LineId` chỉ là một phần của khóa ứng viên `(LineId, UnitNumber)` và `TicketId`, `RecipeVersionId` là thuộc tính không khóa, nên đây là phụ thuộc riêng phần: vi phạm 2NF, và do vế trái không phải siêu khóa nên cũng vi phạm 3NF và BCNF.
- **Hệ quả:** dữ liệu `TicketId` và `RecipeVersionId` lặp lại ở mọi đơn vị của một dòng (ví dụ một dòng 20 ly lưu 20 lần). Nếu sửa tay một đơn vị sang phiếu khác, dữ liệu mâu thuẫn; trigger chỉ bắt được trường hợp khác lượt chứ không bắt được việc `RecipeVersionId` của các đơn vị trong một dòng khác nhau.

**Hai hướng đã cân nhắc** (nhóm chọn hướng 2 ngày 18/11):

1. **Giữ và giải thích (phi chuẩn có kiểm soát).** Lý do: `TicketId` giúp truy vấn đơn vị theo phiếu không cần nối qua `OrderLine` và `OrderRound` (màn hình pha chế sau này), `RecipeVersionId` giữ vết công thức đã dùng cho từng đơn vị. Cần bổ sung một kiểm tra (trigger hoặc CHECK qua hàm) bảo đảm các đơn vị cùng dòng có cùng `RecipeVersionId`, và nêu rõ đây là đánh đổi có chủ ý.
2. **Chuẩn hóa.** Bỏ cột `TicketId` khỏi `PreparationUnit` (suy ra qua `OrderLine.RoundId` rồi `PreparationTicket.RoundId`), chuyển `RecipeVersionId` lên `OrderLine` (hoặc bảng `OrderLineRecipe(LineId, RecipeVersionId)`), bỏ trigger `TR_PreparationUnit_Round`. Cần một migration mới (thêm cột mới, chuyển dữ liệu, rồi bỏ cột cũ), sửa chỗ tạo đơn vị trong `CafeDataCommands.AcceptRoundAsync` (nơi duy nhất ghi `TicketId` và `RecipeVersionId` của `PreparationUnit` trong mã), cấu hình `PreparationUnit` trong `CafeDbContext`, và rà các test có tạo `PreparationUnit`. Sau đó cả 40 bảng đạt BCNF.

**Đã làm (18/11): hướng 2**, chuyển `RecipeVersionId` lên `OrderLine`, không tạo bảng riêng. Lý do chọn: đề yêu cầu CSDL đạt 3NF; và hướng 1 vẫn phải thêm một kiểm tra mới, đồng thời vẫn để dữ liệu lặp ở mọi ly.

Ba trigger không bỏ mà được viết lại, giữ tên và mã lỗi 51002, để thay vai trò của hai khóa ngoại đã bỏ:
- ly chỉ thuộc lượt đã tiếp nhận và có phiếu;
- số thứ tự ly không vượt số lượng của dòng;
- dòng đã chốt công thức đúng biến thể;
- không đổi công thức, lượt hay biến thể của dòng khi dòng đã có ly;
- không xóa phiếu khi lượt còn ly.

Mã đổi ở hai nơi:
- `CafeDataCommands.AcceptRoundAsync` gán `OrderLine.RecipeVersionId` trước khi tạo ly;
- `StaffPreparationService` lấy phiếu qua `OrderLine.RoundId` (5 chỗ).

## 5. Phi chuẩn có chủ ý nhưng không vi phạm chuẩn

- **Snapshot lịch sử:** `OrderLine.ProductNameSnapshot`, `VariantNameSnapshot`, `UnitPriceSnapshot`, `OrderLineAddon.AddonNameSnapshot`, `UnitPriceSnapshot`, `PreparationIngredient.QuantitySnapshot`, `StockCount.BookQty`. Chúng giống thuộc tính của `ProductVariant`, `PriceHistory` hoặc `StockBalance` nhưng là giá trị **tại thời điểm giao dịch**; cùng một `VariantId` có thể có nhiều tên hoặc giá khác nhau trên các dòng khác nhau, nên `VariantId` không xác định chúng và không có phụ thuộc bắc cầu. Đây là yêu cầu nghiệp vụ (BR02: đổi thực đơn không sửa giao dịch cũ).
- **Dữ liệu dẫn xuất liên bảng hoặc liên dòng:** `StockBalance.QuantityReserved`, `PreparationTicket.Status`, `PriceHistory.EffectiveTo`, `ServiceSession.CurrentTableId`, `TableTransferHistory.FromTableId`, `SalesOrder.OrderType`. Chúng suy ra được từ dữ liệu khác nhưng không tạo FD trong nội bộ quan hệ nên không vi phạm 3NF; chúng được giữ vì hiệu năng hoặc kiểm soát đồng thời và phải nhất quán nhờ transaction, trigger và CHECK.
- **Cặp trạng thái và thời điểm:** `Status` với `ApprovedBy`, `ApprovedAt`, `DeliveredBy`, `DeliveredAt`, `RejectedBy` được ràng buộc bằng CHECK. `Status` không xác định giá trị của các cột này nên không phải FD.

## 6. Điểm yếu liên quan nhưng không thuộc chuẩn hóa

- **Khóa nghiệp vụ chưa khai báo UNIQUE:** `Role.RoleName`, `Ingredient.IngredientName`, `Product.ProductName`, `ProductVariant(ProductId, VariantName)`, `OrderLineAddon(LineId, AddonVariantId)`, `SalesOrder.PickupCode`. Nếu nghiệp vụ coi chúng duy nhất thì đó là khóa ứng viên chưa khai báo và FD tương ứng không được CSDL bảo vệ. Cần nhóm xác nhận rồi thêm bằng migration.
- **Tham chiếu đa hình không có khóa ngoại:** `StockMovement(SourceType, SourceId)`, `AuditEvent(EntityName, EntityReference)`, `IdempotencyRecord.ResultReference`. Chấp nhận được cho bảng nhật ký chỉ ghi thêm, đổi lại CSDL không bảo đảm tham chiếu tồn tại.
- **Ràng buộc liên bảng ngoài khả năng của FD:** `RefundRequest.PaymentId` và `AdjustmentId` phải cùng đơn hàng; `CancellationItem` phải cùng đơn với yêu cầu hủy. Một phần có trigger, phần còn lại do dịch vụ bảo đảm.

## 7. Kết luận và việc nên làm

1. **Báo cáo mục 4.2.5:** sửa theo bảng 40 dòng ở trên. Kết luận đổi từ 39/40 thành 40/40 BCNF. Ghi PreparationUnit là ngoại lệ đã xử lý, kèm lý do chọn hướng 2 (mục 4).
2. **Mỗi máy chạy `--migrate` một lần** sau khi gộp, để CSDL Dev có migration `PreparationUnit3NF`. Không hạ hay xóa migration cũ.
3. Thêm UNIQUE cho các khóa nghiệp vụ ở mục 6 sau khi xác nhận quy tắc.
4. Khi có migration mới, cập nhật lại bảng này bằng cách trích lại cấu trúc từ danh mục hệ thống.
5. Muốn trình bày mô hình gọn hơn mà không đổi CSDL: chia 30 bảng lõi và 10 bảng của bốn mô-đun mở rộng (số ngày 18/11; bản 16/11 là 25 và 15), xem [mo-hinh-du-lieu-rut-gon.md](mo-hinh-du-lieu-rut-gon.md).
