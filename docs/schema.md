# Từ điển lược đồ triển khai

Các trường dưới đây là toàn bộ thuộc tính entity nghiệp vụ, đối chiếu tài liệu 04 §5.7. SQL schema thực nằm trong migrations. BIGINT IDENTITY cho PK đơn (trừ StockBalance dùng FK làm PK); RequestId là GUID do bên gọi sinh; khóa ghép không IDENTITY. Chuỗi là Unicode, không có giá trị mặc định SQL ngầm. Dịch vụ/seed gán rõ giá trị. SQL DATETIME2 không giữ múi giờ: quy ước UTC, SaveChanges kiểm tra Kind=Utc khi ghi; SQL trực tiếp phải tự tuân thủ UTC. Đọc DATETIME2 có thể nhận Kind=Unspecified; chuyển rõ UTC tại DTO khi làm API.

Mọi FK đều NO ACTION, kể cả Identity; không xóa dây chuyền. Bảng chứng từ giữ lịch sử, danh mục dùng IsActive khi đã được tham chiếu; nếu chưa có tham chiếu có thể xóa có kiểm soát. CHECK không kiểm tra quyền người thực hiện. RowVersion là concurrency token trên 8 bảng; cập nhật qua EF sẽ kiểm tra giá trị cũ. SQL trực tiếp phải dùng điều kiện tương đương nếu cần.

## Category

Danh mục thực đơn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CategoryId | BIGINT | Không | PK |
| CategoryName | NVARCHAR(200) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: CategoryName.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Product

Món hiện hành.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| ProductId | BIGINT | Không | PK |
| CategoryId | BIGINT | Không | FK → Category |
| ProductName | NVARCHAR(200) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## ProductVariant

Biến thể đồ uống hoặc tùy chọn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| VariantId | BIGINT | Không | PK |
| ProductId | BIGINT | Không | FK → Product |
| VariantName | NVARCHAR(200) | Không | — |
| VariantType | NVARCHAR(30) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[VariantType] IN (N'Drink',N'Addon')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## PriceHistory

Giá theo khoảng nửa mở [từ, đến).

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| PriceHistoryId | BIGINT | Không | PK |
| VariantId | BIGINT | Không | FK → ProductVariant |
| UnitPrice | DECIMAL(18,2) | Không | — |
| EffectiveFrom | DATETIME2 | Không | — |
| EffectiveTo | DATETIME2 | Có | — |

UNIQUE: VariantId, EffectiveFrom.

CHECK:

- `[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]`
- `[UnitPrice] >= 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## CafeTable

Tình trạng bàn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| TableId | BIGINT | Không | PK |
| TableCode | NVARCHAR(40) | Không | — |
| Status | NVARCHAR(30) | Không | — |

UNIQUE: TableCode.

CHECK:

- `[Status] IN (N'Ready',N'Serving',N'NeedsCleaning')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## CustomerProfile

Liên hệ khách, không chứa thông tin xác thực.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CustomerId | BIGINT | Không | PK |
| IdentityUserId | NVARCHAR(450) | Không | FK → AspNetUsers |
| FullName | NVARCHAR(200) | Không | — |
| Phone | NVARCHAR(30) | Không | — |

UNIQUE: IdentityUserId.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## ServiceSession

Phiên phục vụ hiện tại và lịch sử.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| SessionId | BIGINT | Không | PK |
| CurrentTableId | BIGINT | Không | FK → CafeTable |
| OpenedAt | DATETIME2 | Không | — |
| ClosedAt | DATETIME2 | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: CurrentTableId, |[ClosedAt] IS NULL.

CHECK:

- `[ClosedAt] IS NULL OR [ClosedAt] >= [OpenedAt]`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## TableTransferHistory

Lịch sử chuyển bàn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| TransferId | BIGINT | Không | PK |
| SessionId | BIGINT | Không | FK → ServiceSession |
| FromTableId | BIGINT | Không | FK → CafeTable |
| ToTableId | BIGINT | Không | FK → CafeTable |
| TransferAt | DATETIME2 | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[FromTableId] <> [ToTableId]`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## SalesOrder

Đơn theo kênh, không lưu tổng tiền suy diễn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| OrderId | BIGINT | Không | PK |
| OrderCode | NVARCHAR(40) | Không | — |
| CustomerId | BIGINT | Có | FK → CustomerProfile |
| SessionId | BIGINT | Có | FK → ServiceSession |
| OrderType | NVARCHAR(30) | Không | — |
| Status | NVARCHAR(30) | Không | — |
| CreatedAt | DATETIME2 | Không | — |
| ExpectedPickupAt | DATETIME2 | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: OrderCode; SessionId, |[SessionId] IS NOT NULL.

CHECK:

- `([OrderType] = N'Web' AND [CustomerId] IS NOT NULL AND [SessionId] IS NULL) OR ([OrderType] = N'Table' AND [SessionId] IS NOT NULL)`
- `[Status] IN (N'Pending',N'Serving',N'Settling',N'Closed',N'Cancelled',N'Rejected')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Thêm (Status,CreatedAt) cho hàng đợi tác nghiệp. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## OrderRound

Lượt gọi trong đơn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RoundId | BIGINT | Không | PK |
| OrderId | BIGINT | Không | FK → SalesOrder |
| RoundNumber | INT | Không | — |
| Status | NVARCHAR(30) | Không | — |
| AcceptedAt | DATETIME2 | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: OrderId, RoundNumber.

CHECK:

- `[Status] IN (N'Draft',N'Accepted',N'Cancelled')`
- `([Status] = N'Draft' AND [AcceptedAt] IS NULL) OR ([Status] = N'Accepted' AND [AcceptedAt] IS NOT NULL) OR [Status] = N'Cancelled'`
- `[RoundNumber] > 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## OrderLine

Nhóm đơn vị có cùng biến thể, giá và tùy chọn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| LineId | BIGINT | Không | PK |
| RoundId | BIGINT | Không | FK → OrderRound |
| VariantId | BIGINT | Không | FK → ProductVariant |
| Quantity | INT | Không | — |
| ProductNameSnapshot | NVARCHAR(200) | Không | — |
| VariantNameSnapshot | NVARCHAR(200) | Không | — |
| UnitPriceSnapshot | DECIMAL(18,2) | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[Quantity] > 0`
- `[UnitPriceSnapshot] >= 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## OrderLineAddon

Tùy chọn cho MỖI đơn vị món.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| LineAddonId | BIGINT | Không | PK |
| LineId | BIGINT | Không | FK → OrderLine |
| AddonVariantId | BIGINT | Không | FK → ProductVariant |
| Quantity | INT | Không | — |
| AddonNameSnapshot | NVARCHAR(200) | Không | — |
| UnitPriceSnapshot | DECIMAL(18,2) | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[Quantity] > 0`
- `[UnitPriceSnapshot] >= 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## PreparationTicket

Một phiếu cho lượt tiếp nhận.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| TicketId | BIGINT | Không | PK |
| RoundId | BIGINT | Không | FK → OrderRound |
| Status | NVARCHAR(30) | Không | — |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: RoundId.

CHECK:

- `[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Completed',N'Cancelled')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Thêm (Status,CreatedAt) cho hàng đợi tác nghiệp. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## PreparationUnit

Tiến độ từng đơn vị món.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| UnitId | BIGINT | Không | PK |
| TicketId | BIGINT | Không | FK → PreparationTicket |
| LineId | BIGINT | Không | FK → OrderLine |
| UnitNumber | INT | Không | — |
| Status | NVARCHAR(30) | Không | — |
| RecipeVersionId | BIGINT | Không | FK → RecipeVersion |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: LineId, UnitNumber.

CHECK:

- `[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Delivered',N'Cancelled')`
- `[UnitNumber] > 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## PreparationAttempt

Từng lần thực hiện/làm lại.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| AttemptId | BIGINT | Không | PK |
| UnitId | BIGINT | Không | FK → PreparationUnit |
| AttemptNo | INT | Không | — |
| Outcome | NVARCHAR(30) | Không | — |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: UnitId, AttemptNo.

CHECK:

- `[AttemptNo] > 0`
- `[Outcome] IN (N'Started',N'Completed',N'Failed')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## PreparationIngredient

Định lượng đã chốt, gồm nguyên liệu tùy chọn được gộp.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| UnitId | BIGINT | Không | PK; FK → PreparationUnit |
| IngredientId | BIGINT | Không | PK; FK → Ingredient |
| QuantitySnapshot | DECIMAL(18,3) | Không | — |
| QuantityReserved | DECIMAL(18,3) | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[QuantitySnapshot] > 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantitySnapshot]`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Counter

Quầy.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CounterId | BIGINT | Không | PK |
| CounterCode | NVARCHAR(40) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: CounterCode.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Shift

Ca quầy và số kiểm đếm.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| ShiftId | BIGINT | Không | PK |
| CounterId | BIGINT | Không | FK → Counter |
| OpenedBy | BIGINT | Không | FK → StaffAccount |
| OpenedAt | DATETIME2 | Không | — |
| OpeningCash | DECIMAL(18,2) | Không | — |
| CountedCash | DECIMAL(18,2) | Có | — |
| ClosedBy | BIGINT | Có | FK → StaffAccount |
| ClosedAt | DATETIME2 | Có | — |
| CloseNote | NVARCHAR(500) | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: CounterId, |[ClosedAt] IS NULL.

CHECK:

- `[OpeningCash] >= 0 AND ([CountedCash] IS NULL OR [CountedCash] >= 0)`
- `([ClosedAt] IS NULL AND [ClosedBy] IS NULL AND [CountedCash] IS NULL) OR ([ClosedAt] IS NOT NULL AND [ClosedBy] IS NOT NULL AND [CountedCash] IS NOT NULL AND [ClosedAt] >= [OpenedAt])`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Payment

Chứng từ thu.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| PaymentId | BIGINT | Không | PK |
| PaymentCode | NVARCHAR(40) | Không | — |
| OrderId | BIGINT | Không | FK → SalesOrder |
| ShiftId | BIGINT | Không | FK → Shift |
| Amount | DECIMAL(18,2) | Không | — |
| Method | NVARCHAR(30) | Không | — |
| Status | NVARCHAR(30) | Không | — |
| PaidAt | DATETIME2 | Có | — |
| RequestId | UNIQUEIDENTIFIER | Không | FK → IdempotencyRecord |
| CreatedBy | BIGINT | Không | FK → StaffAccount |

UNIQUE: PaymentCode; RequestId.

CHECK:

- `[Amount] > 0`
- `[Method] IN (N'Cash',N'BankTransfer')`
- `[Status] IN (N'Pending',N'Succeeded',N'Failed')`
- `[Status] <> N'Succeeded' OR [PaidAt] IS NOT NULL`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## SalesAdjustment

Điều chỉnh nghĩa vụ có dấu.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| AdjustmentId | BIGINT | Không | PK |
| OrderId | BIGINT | Không | FK → SalesOrder |
| AmountSigned | DECIMAL(18,2) | Không | — |
| Reason | NVARCHAR(500) | Không | — |
| CreatedBy | BIGINT | Không | FK → StaffAccount |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## RefundRequest

Đề nghị và phê duyệt hoàn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RefundId | BIGINT | Không | PK |
| RefundCode | NVARCHAR(40) | Không | — |
| PaymentId | BIGINT | Không | FK → Payment |
| AdjustmentId | BIGINT | Có | FK → SalesAdjustment |
| Amount | DECIMAL(18,2) | Không | — |
| Status | NVARCHAR(30) | Không | — |
| RequestedBy | BIGINT | Không | FK → StaffAccount |
| ApprovedBy | BIGINT | Có | FK → StaffAccount |
| ApprovedAt | DATETIME2 | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: RefundCode.

CHECK:

- `[Amount] > 0`
- `[Status] IN (N'Pending',N'Approved',N'Rejected')`
- `([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## RefundTransaction

Kết quả hoàn tách khỏi phê duyệt.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RefundTransactionId | BIGINT | Không | PK |
| RefundId | BIGINT | Không | FK → RefundRequest |
| ShiftId | BIGINT | Không | FK → Shift |
| Amount | DECIMAL(18,2) | Không | — |
| Method | NVARCHAR(30) | Không | — |
| Status | NVARCHAR(30) | Không | — |
| ExternalReference | NVARCHAR(100) | Có | — |
| RequestId | UNIQUEIDENTIFIER | Không | FK → IdempotencyRecord |
| ExecutedAt | DATETIME2 | Có | — |
| CreatedBy | BIGINT | Không | FK → StaffAccount |

UNIQUE: RequestId.

CHECK:

- `[Amount] > 0`
- `[Method] IN (N'Cash',N'BankTransfer')`
- `[Status] IN (N'Pending',N'Succeeded',N'Failed')`
- `[Status] <> N'Succeeded' OR [ExecutedAt] IS NOT NULL`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Ingredient

Nguyên liệu theo đơn vị chuẩn.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| IngredientId | BIGINT | Không | PK |
| IngredientName | NVARCHAR(200) | Không | — |
| UnitName | NVARCHAR(30) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## RecipeVersion

Phiên bản công thức theo hiệu lực.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RecipeVersionId | BIGINT | Không | PK |
| VariantId | BIGINT | Không | FK → ProductVariant |
| EffectiveFrom | DATETIME2 | Không | — |
| EffectiveTo | DATETIME2 | Có | — |

UNIQUE: VariantId, EffectiveFrom.

CHECK:

- `[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## RecipeDetail

Định lượng cho một đơn vị biến thể.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RecipeVersionId | BIGINT | Không | PK; FK → RecipeVersion |
| IngredientId | BIGINT | Không | PK; FK → Ingredient |
| QuantityRequired | DECIMAL(18,3) | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[QuantityRequired] > 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## IngredientBatch

Thông tin lô tùy chọn; CHƯA truy xuất tiêu hao theo lô.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| BatchId | BIGINT | Không | PK |
| IngredientId | BIGINT | Không | FK → Ingredient |
| QuantityReceived | DECIMAL(18,3) | Không | — |
| ExpiryDate | DATE | Có | — |

UNIQUE: chỉ PK.

CHECK:

- `[QuantityReceived] > 0`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## StockBalance

Tổng hợp một kho.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| IngredientId | BIGINT | Không | PK; FK → Ingredient |
| QuantityOnHand | DECIMAL(18,3) | Không | — |
| QuantityReserved | DECIMAL(18,3) | Không | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: chỉ PK.

CHECK:

- `[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## StockMovement

Sổ biến động không sửa/xóa.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| MovementId | BIGINT | Không | PK |
| MovementCode | NVARCHAR(40) | Không | — |
| IngredientId | BIGINT | Không | FK → Ingredient |
| MovementType | NVARCHAR(30) | Không | — |
| Quantity | DECIMAL(18,3) | Không | — |
| SourceType | NVARCHAR(30) | Không | — |
| SourceId | BIGINT | Không | — |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: MovementCode.

CHECK:

- `[Quantity] > 0`
- `[MovementType] IN (N'Receipt',N'Consume',N'Waste',N'AdjustmentIn',N'AdjustmentOut',N'Reserve',N'Release')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## StockCount

Biên bản kiểm đếm và người duyệt.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CountId | BIGINT | Không | PK |
| IngredientId | BIGINT | Không | FK → Ingredient |
| BookQty | DECIMAL(18,3) | Không | — |
| CountedQty | DECIMAL(18,3) | Không | — |
| CountedAt | DATETIME2 | Không | — |
| CountedBy | BIGINT | Không | FK → StaffAccount |
| ApprovedBy | BIGINT | Có | FK → StaffAccount |
| Status | NVARCHAR(30) | Không | — |

UNIQUE: chỉ PK.

CHECK:

- `[BookQty] >= 0 AND [CountedQty] >= 0`
- `[Status] IN (N'Pending',N'Approved',N'Rejected')`
- `[ApprovedBy] IS NULL OR [ApprovedBy] <> [CountedBy]`

CHECK bổ sung HistoryProtection: Pending chưa có ApprovedBy; Approved/Rejected có ApprovedBy và khác CountedBy.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## StaffAccount

Hồ sơ nhân viên, không chứa mật khẩu.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| StaffId | BIGINT | Không | PK |
| IdentityUserId | NVARCHAR(450) | Không | FK → AspNetUsers |
| FullName | NVARCHAR(200) | Không | — |
| IsActive | BIT | Không | — |

UNIQUE: IdentityUserId.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Role

Vai trò nghiệp vụ.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RoleId | BIGINT | Không | PK |
| RoleName | NVARCHAR(100) | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Permission

Mã hành động.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| PermissionId | BIGINT | Không | PK |
| PermissionCode | NVARCHAR(100) | Không | — |

UNIQUE: PermissionCode.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## StaffRole

Phân vai nhân viên.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| StaffId | BIGINT | Không | PK; FK → StaffAccount |
| RoleId | BIGINT | Không | PK; FK → Role |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## RolePermission

Cấp quyền cho vai trò.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RoleId | BIGINT | Không | PK; FK → Role |
| PermissionId | BIGINT | Không | PK; FK → Permission |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## AuditEvent

Nhật ký chỉ ghi thêm.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| AuditId | BIGINT | Không | PK |
| StaffId | BIGINT | Không | FK → StaffAccount |
| ActionCode | NVARCHAR(100) | Không | — |
| EntityName | NVARCHAR(100) | Không | — |
| EntityReference | NVARCHAR(100) | Có | — |
| Reason | NVARCHAR(500) | Có | — |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## IdempotencyRecord

Mã yêu cầu, dấu băm và tham chiếu kết quả.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| RequestId | UNIQUEIDENTIFIER | Không | PK |
| RequestHash | NVARCHAR(128) | Không | — |
| ResultReference | NVARCHAR(200) | Có | — |
| CreatedAt | DATETIME2 | Không | — |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## CancellationRequest

Đề nghị và quyết định hủy.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CancellationId | BIGINT | Không | PK |
| OrderId | BIGINT | Không | FK → SalesOrder |
| RequestedBy | BIGINT | Không | FK → StaffAccount |
| ApprovedBy | BIGINT | Có | FK → StaffAccount |
| Reason | NVARCHAR(500) | Không | — |
| Status | NVARCHAR(30) | Không | — |
| CreatedAt | DATETIME2 | Không | — |
| ApprovedAt | DATETIME2 | Có | — |
| RowVersion | ROWVERSION | Không | Concurrency token |

UNIQUE: chỉ PK.

CHECK:

- `[Status] IN (N'Pending',N'Approved',N'Rejected')`
- `([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## CancellationItem

Mỗi dòng là một đơn vị món bị đề nghị hủy.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CancellationId | BIGINT | Không | PK; FK → CancellationRequest |
| UnitId | BIGINT | Không | PK; FK → PreparationUnit |

UNIQUE: chỉ PK.

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## ShiftCashEntry

Thu/chi tiền mặt khác ngoài bán hàng.

| Trường | Kiểu SQL | NULL | Khóa / tham chiếu |
|---|---|---|---|
| CashEntryId | BIGINT | Không | PK |
| CashEntryCode | NVARCHAR(40) | Không | — |
| ShiftId | BIGINT | Không | FK → Shift |
| EntryType | NVARCHAR(30) | Không | — |
| Amount | DECIMAL(18,2) | Không | — |
| Reason | NVARCHAR(500) | Không | — |
| CreatedBy | BIGINT | Không | FK → StaffAccount |
| CreatedAt | DATETIME2 | Không | — |
| Status | NVARCHAR(30) | Không | — |

UNIQUE: CashEntryCode.

CHECK:

- `[Amount] > 0`
- `[EntryType] IN (N'OtherIn',N'OtherOut')`
- `[Status] IN (N'Posted',N'Voided')`

Chỉ mục FK do convention EF hỗ trợ join/kiểm tra tham chiếu. Không thêm chỉ mục tìm kiếm tên tự do khi chưa có bằng chứng truy vấn.

## Identity

`AspNetUsers`: Id NVARCHAR(450) PK; UserName, NormalizedUserName, Email, NormalizedEmail NVARCHAR(256) NULL; EmailConfirmed BIT; PasswordHash/SecurityStamp/ConcurrencyStamp/PhoneNumber NVARCHAR(MAX) NULL; PhoneNumberConfirmed/TwoFactorEnabled/LockoutEnabled BIT; LockoutEnd DATETIMEOFFSET NULL (ngoại lệ kiểu thời gian do Identity); AccessFailedCount INT. UserNameIndex UNIQUE có lọc; EmailIndex không UNIQUE. Model Identity nguyên bản cho phép username NULL ở SQL, UserManager validation kiểm tra khi đăng ký. Không tự thêm NOT NULL làm lệch framework.

`AspNetUserClaims`: Id INT IDENTITY PK, UserId FK, ClaimType/ClaimValue. `AspNetUserLogins`: PK(LoginProvider,ProviderKey), UserId FK, ProviderDisplayName. `AspNetUserTokens`: PK(UserId,LoginProvider,Name), Value. Xem migration cho độ dài và nullability kỹ thuật chính xác; không coi 3 bảng này là quan hệ nghiệp vụ mới.

