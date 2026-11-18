# Mô hình dữ liệu rút gọn (lõi và mô-đun mở rộng)

Ngày 16/11/2026. Mục tiêu: trình bày và vận hành mô hình dữ liệu **đơn giản hơn** (từ 40 bảng xuống 30 bảng lõi; bản 16/11 là 25) mà **không đổi cơ sở dữ liệu, không đổi mã, không đổi kiểm thử**, giữ nguyên kết quả chuẩn hóa (bản 16/11 là 24/25 bảng lõi BCNF với ngoại lệ `PreparationUnit`; sau mức 2 ngày 18/11 là 30/30, xem mục 5), và các phần được rút khỏi mô hình chính có thể đưa vào lại bất cứ lúc nào vì chúng vẫn nằm nguyên trong cơ sở dữ liệu. Kết quả chuẩn hóa từng bảng xem [phan-tich-3nf.md](phan-tich-3nf.md).

> **Cập nhật 18/11/2026.** Có 5 bảng chuyển từ mô-đun mở rộng vào lõi:
> - `Counter`, `Shift`, `ShiftCashEntry`, `Payment`: dùng cho mở ca, thu tiền mặt, đóng ca và đối soát. Lý do: đã có API ca và thu tiền (A2, PR #11, gộp 17/11) và màn hình Desktop Thu ngân (A4, 18/11).
> - `PreparationAttempt`: dùng khi bắt đầu làm món. Lý do: đã có API pha chế (A3, PR #11) và màn hình Desktop Pha chế (C3, 18/11).
>
> Sau thay đổi, lõi có **30 bảng** và phần mở rộng còn **10 bảng**. Các bảng dưới đây đã sửa theo số mới. Báo cáo (mục 4.2.1, Bảng 4.4) vẫn ghi 25/15 theo bản 16/11, cần sửa theo tài liệu này.

## 1. Nguyên tắc

1. **Rút gọn ở mức mô hình, không xóa gì ở mức vật lý.** Vẫn đủ 40 bảng; migration thứ 5 (`PreparationUnit3NF`, mức 2 ở mục 6) chỉ chuyển một cột, không bỏ bảng nào; bộ kiểm thử (105 ca ngày 16/11, 148 ca ngày 18/11) không bị chạm.
2. **Lõi là phần đang chạy thật:** bảng được Web, Desktop hoặc API dùng qua các dịch vụ hiện có.
3. **Mô-đun mở rộng khép kín:** mỗi mô-đun chỉ trỏ vào lõi qua vài khóa ngoại và lõi không phụ thuộc ngược lại (một ngoại lệ nêu ở mục 4). Vì vậy rút ra hay đưa vào lại chỉ là thêm hoặc bớt một cụm bảng trên sơ đồ, không phải sửa lõi.

## 2. Kiểm kê mức độ sử dụng của 40 bảng

Căn cứ: tìm mọi chỗ dùng bảng trong `Services/`, `CafeDataCommands.cs`, `StaffPermissionReader.cs` (mã chạy thật) và trong `tests/`.

| Nhóm | Số bảng | Mức sử dụng |
|---|---|---|
| **Lõi (A)** | 30 | Dùng bởi Web, Desktop, API (UC01–UC04, UC07, UC08, UC14, UC17–UC19) kể cả chuỗi tiếp nhận đơn, pha chế, thu tiền và ca |
| **Mô-đun B** | 3 | Hoàn tiền: có thao tác dữ liệu trong `CafeDataCommands` (hoặc là khóa ngoại của các bảng đó) và có kiểm thử CSDL, chưa có API hay giao diện |
| **Mô-đun C** | 7 | Chỉ có lược đồ, hoặc chỉ được đọc để chặn xóa nguyên liệu còn tham chiếu |

## 3. Mô hình lõi (30 bảng, 6 cụm)

| Cụm | Bảng | Dùng cho |
|---|---|---|
| Danh mục | Category, Product, ProductVariant, PriceHistory, AllowedAddon | Thực đơn, biến thể, giá theo thời gian, tùy chọn thêm (UC01, UC17) |
| Công thức và tồn | Ingredient, RecipeVersion, RecipeDetail, StockBalance, StockMovement | Định lượng, giữ nguyên liệu khi tiếp nhận (UC03, UC17) |
| Đơn hàng | SalesOrder, OrderRound, OrderLine, OrderLineAddon, IdempotencyRecord | Đặt, xem, hủy đơn chờ; chống gửi trùng (UC02, UC04) |
| Pha chế | PreparationTicket, PreparationUnit, PreparationIngredient, PreparationAttempt | Tạo phiếu và đơn vị món khi tiếp nhận (UC03); hàng đợi ly, bắt đầu làm, sẵn sàng, đã giao (UC07: API A3, Desktop Pha chế C3) |
| Thu tiền và ca | Counter, Shift, ShiftCashEntry, Payment | Mở ca, thu tiền mặt, đóng ca và đối soát (UC08, UC14: API A2, Desktop Thu ngân A4); Web đọc Payment để hiện đơn đã thanh toán (B2) |
| Tài khoản và quyền | CustomerProfile, StaffAccount, Role, Permission, StaffRole, RolePermission, AuditEvent | Đăng ký, đăng nhập, phân quyền, nhật ký (UC18, UC19, UC03) |

Chuẩn hóa: **30/30 bảng lõi đạt BCNF** sau migration `PreparationUnit3NF` (mục 5); trước đó là 29/30, ngoại lệ `PreparationUnit`.

## 4. Mô-đun mở rộng (10 bảng, 4 mô-đun)

| Mô-đun | Bảng | Trỏ vào lõi qua | Cách đưa vào lại |
|---|---|---|---|
| **B. Hoàn tiền** (UC12) | SalesAdjustment, RefundRequest, RefundTransaction | Payment, Shift, SalesOrder, StaffAccount | Đã có trong CSDL và `CafeDataCommands` (hoàn tiền mặt); thêm API và màn hình |
| **C1. Bàn và phục vụ tại quán** (UC05, UC06, UC09) | CafeTable, ServiceSession, TableTransferHistory | SalesOrder.SessionId (khóa ngoại cho phép NULL, đơn Web luôn NULL) | Đã có trong CSDL; thêm dịch vụ và màn hình D01 |
| **C2. Hủy món** (UC10, UC11) | CancellationRequest, CancellationItem | SalesOrder, PreparationUnit, StaffAccount | Đã có trong CSDL; thêm dịch vụ và màn hình D05 |
| **C3. Kho mở rộng** (UC13) | StockCount, IngredientBatch | Ingredient, StaffAccount | Đã có trong CSDL. API danh mục (A1) chỉ đọc hai bảng này để chặn xóa nguyên liệu còn tham chiếu. Cần thêm dịch vụ và màn hình kho |

- **Chiều phụ thuộc:** mọi khóa ngoại của mô-đun đều trỏ **vào lõi**. Chiều ngược lại chỉ có `SalesOrder.SessionId → ServiceSession` (mô-đun C1). Trên sơ đồ lõi, cột này và cột dẫn xuất `OrderType` được ẩn đi; đơn Web luôn có `SessionId = NULL`.
- **Chuẩn hóa:** cả 10 bảng mở rộng đều BCNF, nên thêm hay bớt mô-đun không làm đổi kết luận 3NF của lõi.
- **Bảng không có mã hay kiểm thử nào dùng:** nay chỉ còn `TableTransferHistory`.
  - `StockCount` và `IngredientBatch` được đọc khi xóa nguyên liệu.
  - `PreparationAttempt` đã được API pha chế dùng (xem mục 6).

## 5. 3NF của mô hình rút gọn: ngoại lệ PreparationUnit đã xử lý

Bản 16/11 có một ngoại lệ: `PreparationUnit` không đạt 2NF và 3NF vì `LineId → TicketId` và `LineId → RecipeVersionId` (xem [phan-tich-3nf.md](phan-tich-3nf.md), mục 4).

Ngày 18/11, nhóm chuẩn hóa ở mức vật lý bằng migration `PreparationUnit3NF`. Nhánh `feat/a17-chuan-hoa-preparation-unit` làm mới từ `main`, thay cho nhánh thử PR #9. Lược đồ sau migration:

```
OrderLine(LineId, RoundId, VariantId, Quantity, ..., RecipeVersionId)      -- công thức chốt lúc tiếp nhận, thuộc về dòng
PreparationUnit(UnitId, LineId, UnitNumber, Status, DeliveredAt, DeliveredBy)
PreparationTicket(TicketId, RoundId, Status, CreatedAt)                    -- phiếu của một ly = phiếu có RoundId = OrderLine.RoundId
```

Kết quả: **30/30 bảng lõi và cả 40 bảng đạt BCNF**.

Các việc đã làm:
1. **Migration mới:** thêm `OrderLine.RecipeVersionId` (cho phép NULL, khóa ngoại tới `RecipeVersion`) và điền từ các ly hiện có. Sau đó mới bỏ khóa ngoại, chỉ mục và hai cột của `PreparationUnit`. `Down` làm ngược lại.
2. **Trigger:** viết lại ba trigger (giữ tên và mã lỗi 51002) để thay vai trò của hai khóa ngoại đã bỏ. Thêm luật mới: không đổi công thức của dòng khi dòng đã có ly.
3. **Mã:** `CafeDataCommands.AcceptRoundAsync` gán `line.RecipeVersionId` trước khi tạo ly. `StaffPreparationService` lấy phiếu qua `OrderLine.RoundId` (5 chỗ).
4. **Kiểm thử:** sửa ca đếm ly theo phiếu; thêm một ca kiểm các trigger mới. Kết quả 149/149 đạt. Thử nâng cấp rồi hạ cấp trên bản sao CSDL Dev có 63 ly đều đúng dữ liệu.

## 6. Các mức rút gọn

| Mức | Việc | Ảnh hưởng tới dự án | Kết quả |
|---|---|---|---|
| **0** | Trình bày lõi 30 bảng và 4 mô-đun như tài liệu này | Không (chỉ tài liệu và báo cáo) | 30 bảng lõi, nay cả 30 đạt BCNF sau mức 2 |
| **1** | Bỏ bảng không ai dùng bằng một migration mới; nay chỉ còn `TableTransferHistory` | Không đổi mã và kiểm thử; đổi lược đồ và cần sửa tài liệu 04. Bỏ thêm `StockCount`, `IngredientBatch` thì phải sửa kiểm tra xóa nguyên liệu (A1) | 39 bảng; khôi phục bằng `Down` của migration |
| **2** | Chuẩn hóa vật lý `PreparationUnit` (mục 5) | Có: một chỗ ghi và 5 chỗ đọc trong mã, ba trigger, vài ca kiểm thử. **Đã làm 18/11** (migration `PreparationUnit3NF`) | 3NF vật lý cho mọi bảng |

Khuyến nghị (cập nhật 18/11):
- **Mức 0:** đã làm. Báo cáo còn phải sửa số 25/15 thành 30/10.
- **Mức 2:** đã làm ngày 18/11. Báo cáo 4.2.5 còn phải sửa 39/40 thành 40/40.
- **Mức 1:** nay chỉ bỏ được 1 bảng nên ít giá trị.
