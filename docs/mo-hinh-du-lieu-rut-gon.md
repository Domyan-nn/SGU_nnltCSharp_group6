# Mô hình dữ liệu rút gọn (lõi và mô-đun mở rộng)

Ngày 16/11/2026. Mục tiêu: trình bày và vận hành mô hình dữ liệu **đơn giản hơn** (từ 40 bảng xuống 30 bảng lõi; bản 16/11 là 25) mà **không đổi cơ sở dữ liệu, không đổi mã, không đổi kiểm thử**, giữ nguyên kết quả chuẩn hóa (29/30 bảng lõi BCNF, một ngoại lệ `PreparationUnit` được xử lý ở mục 5), và các phần được rút khỏi mô hình chính có thể đưa vào lại bất cứ lúc nào vì chúng vẫn nằm nguyên trong cơ sở dữ liệu. Kết quả chuẩn hóa từng bảng xem [phan-tich-3nf.md](phan-tich-3nf.md).

> **Cập nhật 18/11/2026.** Có 5 bảng chuyển từ mô-đun mở rộng vào lõi:
> - `Counter`, `Shift`, `ShiftCashEntry`, `Payment`: dùng cho mở ca, thu tiền mặt, đóng ca và đối soát. Lý do: đã có API ca và thu tiền (A2, PR #11, gộp 17/11) và màn hình Desktop Thu ngân (A4, 18/11).
> - `PreparationAttempt`: dùng khi bắt đầu làm món. Lý do: đã có API pha chế (A3, PR #11) và màn hình Desktop Pha chế (C3, 18/11).
>
> Sau thay đổi, lõi có **30 bảng** và phần mở rộng còn **10 bảng**. Các bảng dưới đây đã sửa theo số mới. Báo cáo (mục 4.2.1, Bảng 4.4) vẫn ghi 25/15 theo bản 16/11, cần sửa theo tài liệu này.

## 1. Nguyên tắc

1. **Rút gọn ở mức mô hình, không xóa gì ở mức vật lý.** 40 bảng và 4 migration giữ nguyên; bộ kiểm thử (105 ca ngày 16/11, 148 ca ngày 18/11) không bị chạm.
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

Chuẩn hóa: **29/30 bảng đạt BCNF**, ngoại lệ `PreparationUnit` (mục 5).

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

## 5. 3NF của mô hình rút gọn và ngoại lệ PreparationUnit

`PreparationUnit` là bảng duy nhất không đạt 2NF và 3NF vì `LineId → TicketId` và `LineId → RecipeVersionId` (xem [phan-tich-3nf.md](phan-tich-3nf.md), mục 4). **Nói thẳng:** không thể đạt 3NF cho 100% bảng mà không đổi `PreparationUnit` ở mức vật lý (bỏ hai cột trên), và đổi đó có ảnh hưởng nhỏ tới mã và kiểm thử. Trong ràng buộc "không đụng mã và CSDL" chỉ có hai lựa chọn:

- **(a) Giữ và ghi rõ ngoại lệ** (báo cáo mục 4.2.5 đã làm): 29/30 bảng lõi đạt BCNF, ngoại lệ có giải thích.
- **(b) Dùng dạng luận lý 3NF đề xuất dưới đây trong sơ đồ rút gọn**, kèm ghi chú rằng bảng vật lý hiện còn hai cột phi chuẩn có kiểm soát. Dạng này **chưa khớp hoàn toàn** với danh mục bảng ở báo cáo mục 4.2.1 (đang ghi `TicketId` trong `PreparationUnit`) cho tới khi làm mức 2 ở mục 6, nên chỉ dùng khi nhóm chấp nhận ghi chú đó.

**Dạng luận lý 3NF đề xuất:**

```
OrderLine(LineId, RoundId, VariantId, Quantity, ..., RecipeVersionId)      -- công thức chọn lúc tiếp nhận, thuộc về dòng
PreparationUnit(UnitId, LineId, UnitNumber, Status, DeliveredAt, DeliveredBy)
PreparationTicket(TicketId, RoundId, Status, CreatedAt)                    -- phiếu của một đơn vị = phiếu có RoundId = OrderLine.RoundId
```

Ánh xạ sang vật lý hiện tại: `OrderLine.RecipeVersionId` tương ứng `PreparationUnit.RecipeVersionId` (mọi đơn vị cùng dòng có cùng giá trị) và `TicketId` suy ra từ `LineId` qua lượt. Hai cột vật lý còn lại là **phi chuẩn có chủ ý**: giữ để truy vấn đơn vị theo phiếu không cần nối và để giữ vết công thức từng đơn vị; được bảo vệ bằng ba trigger (`TR_PreparationUnit_Round`, `TR_PreparationTicket_Round`, `TR_OrderLine_Units`).

**Nếu muốn đưa 3NF xuống mức vật lý:** đã làm thử trên nhánh `feat/db-3nf-preparation-unit` (PR #9) nhưng chưa gộp. Muốn gộp phải sửa thêm 5 chỗ đọc `PreparationUnit.TicketId` trong `StaffPreparationService`, xem ghi chú 18/11 ở [phan-tich-3nf.md](phan-tich-3nf.md). Các bước của nhánh thử:

1. Migration mới: thêm `OrderLine.RecipeVersionId` (cho phép NULL, khóa ngoại tới `RecipeVersion`), điền từ `PreparationUnit` (một giá trị cho mỗi dòng).
2. Viết lại ba trigger bỏ tham chiếu `u.TicketId` và `u.RecipeVersionId`: kiểm tra `UnitNumber ≤ Quantity`, `RecipeVersion.VariantId = OrderLine.VariantId` và việc dòng thuộc lượt đã tiếp nhận có phiếu.
3. Bỏ khóa ngoại, chỉ mục rồi cột `PreparationUnit.TicketId`, `PreparationUnit.RecipeVersionId`; viết `Down` làm ngược lại (thêm cột, điền từ `OrderLine` và phiếu của lượt).
4. Mã: `CafeDataCommands.AcceptRoundAsync` gán `line.RecipeVersionId` thay vì hai cột của đơn vị; cập nhật `Entities.cs` và `CafeDbContext`.
5. Kiểm thử: sửa các ca trong `DatabaseTests` đếm đơn vị theo `TicketId` và các ca kiểm tra trigger của đơn vị; chạy lại 105 ca (nay là toàn bộ 148 ca).

Thay đổi này gồm một migration, một chỗ ghi và 5 chỗ đọc trong mã, cùng vài ca kiểm thử, nên đưa vào lại rồi gỡ ra đều gọn. Cần quyết định riêng vì **có ảnh hưởng tới mã và kiểm thử**.

## 6. Các mức rút gọn

| Mức | Việc | Ảnh hưởng tới dự án | Kết quả |
|---|---|---|---|
| **0** | Trình bày lõi 30 bảng và 4 mô-đun như tài liệu này; ghi rõ ngoại lệ `PreparationUnit` (lựa chọn a hoặc b ở mục 5) | Không (chỉ tài liệu và báo cáo) | 30 bảng lõi, 29 bảng BCNF và một ngoại lệ có giải thích |
| **1** | Bỏ bảng không ai dùng bằng một migration mới; nay chỉ còn `TableTransferHistory` | Không đổi mã và kiểm thử; đổi lược đồ và cần sửa tài liệu 04. Bỏ thêm `StockCount`, `IngredientBatch` thì phải sửa kiểm tra xóa nguyên liệu (A1) | 39 bảng; khôi phục bằng `Down` của migration |
| **2** | Chuẩn hóa vật lý `PreparationUnit` (mục 5) | Có: một chỗ ghi và 5 chỗ đọc trong mã, ba trigger, vài ca kiểm thử. Đã làm thử ở PR #9 | 3NF vật lý cho mọi bảng |

Khuyến nghị (cập nhật 18/11):
- **Mức 0:** đã làm. Báo cáo còn phải sửa số 25/15 thành 30/10.
- **Mức 2:** đã có nhánh thử ở PR #9; nhóm quyết trước giờ đóng băng mã.
- **Mức 1:** nay chỉ bỏ được 1 bảng nên ít giá trị.
