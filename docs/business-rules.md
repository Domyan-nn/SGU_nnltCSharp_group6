# Quy tắc, transaction và giới hạn triển khai

Đối chiếu SRS BR01–BR12, dữ liệu 04 §5.7/6. Phân biệt cơ chế đã thực hiện với use case chưa xây. Quyền SQL của chủ CSDL có thể vô hiệu hóa trigger; cấu hình production cần tài khoản ứng dụng quyền tối thiểu, không có quyền DDL.

## Ma trận BR

| BR | SQL bảo vệ | Dịch vụ dữ liệu đã có | Còn cần use case/phân quyền |
|---|---|---|---|
| BR01 | CHECK số lượng bán dương, tiền không âm, lượng nguyên liệu dương; FK biến thể/công thức | Tiếp nhận đòi công thức hiệu lực có chi tiết, món còn bán | CRUD thực đơn/công thức; kiểm tra mọi biến thể được công bố có giá và công thức |
| BR02 | Snapshot tách khỏi danh mục; FK NO ACTION, DECIMAL | Tiếp nhận không sửa tên/giá cũ | UC02/05 tính giá tại máy chủ, kiểm tra giá khách xác nhận, tách dòng khi khác tùy chọn; chưa có lệnh tạo đơn công khai |
| BR03 | Ticket chỉ cho round Accepted, UNIQUE RoundId; trigger Unit cùng round/recipe/quantity | AcceptRound: chốt nguyên liệu cả món và addon, giữ tồn, tạo ticket/unit/snapshot/movement, đổi trạng thái trong Serializable transaction; cùng round nhận lại ticket cũ | Kiểm tra quyền tiếp nhận, lý do từ chối, quy trình tạo nháp. SQL không tự ngăn mọi can thiệp trực tiếp vào OrderRound hoặc bảng giữ tồn |
| BR04 | FK hồ sơ/đơn; hủy có trạng thái, người và thời gian; CHECK duyệt khác người đề nghị | Chưa có lệnh tự hủy hoặc duyệt hủy | UC04 kiểm tra chủ sở hữu + Pending; sau nhận phải UC10/11 có quyền và trạng thái hiện hành |
| BR05 | CancellationItem là một Unit; trigger không cho hủy Unit của đơn khác; quyết định đã xử lý không sửa/xóa | StartUnit chuyển lượng giữ → tiêu hao và ghi Attempt/movement một lần | Duyệt hủy, giải phóng giữ, hao hụt/làm lại, quyết định tính tiền và điều chỉnh đồng bộ; chưa xây |
| BR06 | Successful Payment/RefundTransaction không UPDATE/DELETE; FK thu gốc; trigger hoàn Approved, nguồn Succeeded, tổng Pending+Succeeded không vượt khoản thu hoặc số đề nghị | RecordCashRefund khóa Payment nguồn, kiểm tra số còn hoàn; không thực hiện lại một đề nghị đang pending/đã hoàn; Request đã có giao dịch active không sửa/xóa | Chính sách duyệt, kiểm tra Adjustment cùng đơn, tạo hạn mức theo nghĩa vụ sau bán; tích hợp ngân hàng/chuyển khoản |
| BR07 | PK RequestId toàn cục, Payment/Refund FK + UNIQUE; trigger chống dùng cùng RequestId ở cả hai bảng | sp_getapplock theo RequestId, SHA-256 nội dung chuẩn hóa, ghi kết quả cùng transaction; cùng nội dung trả ID cũ, khác nội dung từ chối | Chưa áp dụng vào tạo đơn/chuyển bàn/các UC khác; tra cứu API, chính sách lưu trữ idempotency và kết quả ngân hàng chưa rõ |
| BR08 | CHECK 0 ≤ Reserved ≤ OnHand; RowVersion | Cập nhật giữ bằng UPDATE có điều kiện tồn khả dụng, thứ tự IngredientId, cùng transaction tiếp nhận; StartUnit trừ OnHand/Reserved, ghi Consume | Nhập/điều chỉnh/kiểm kê, release/waste/làm lại có kiểm soát; đối chiếu tổng hợp với ledger tự động |
| BR09 | FK RecipeVersion/Detail, UNIQUE bắt đầu; trigger khoảng hiệu lực không chồng | AcceptRound đọc phiên bản hiệu lực, lưu RecipeVersionId chính và định lượng gộp từng Unit; StartUnit dùng snapshot | Version của từng addon không lưu thành FK riêng, nhưng định lượng addon nằm trong snapshot bất biến theo dịch vụ. Chưa có UC sửa công thức hoặc bảo vệ mọi SQL trực tiếp sửa snapshot |
| BR10 | UNIQUE có lọc CurrentTableId WHERE ClosedAt IS NULL; SessionId trên đơn UNIQUE có lọc | Chưa có lệnh mở/chuyển/dọn bàn | Bàn đích Ready, đổi hai trạng thái + history trong transaction, NeedsCleaning không được mở; UNIQUE đơn lẻ không bảo vệ các bước này |
| BR11 | UNIQUE ca mở trên quầy; CHECK đóng đủ CountedCash/ClosedBy/ClosedAt; thu/hoàn có Staff, Method, Shift FK; trigger từ chối ghi vào ca đã đóng | Thu/hoàn khóa hàng Shift UPDLOCK/HOLDLOCK; ReconcileShift đọc nhất quán, chỉ cộng tiền mặt thành công và OtherIn/Out Posted | UC14 đóng ca phải lấy cùng khóa Shift, xử lý pending, kiểm đếm, quyền xác nhận và bàn giao đơn; chưa có close handler. ShiftCashEntry chưa có cơ chế chứng minh không trùng Payment/Refund theo nguồn |
| BR12 | NO ACTION toàn bộ FK; chứng từ thành công, kho, audit append-only, quyết định hủy đã xử lý bất biến; SalesAdjustment tách riêng | Các thao tác dữ liệu không tự đóng đơn hoặc sửa lịch sử menu | Chưa khóa nội dung đơn đã đóng bằng handler, chưa có quy trình điều chỉnh sau bán, audit tự động cho mọi UC, kiểm tra giao đủ + cân bằng nghĩa vụ trước khi đóng |

## Ranh giới của các ràng buộc

- CHECK: chỉ điều kiện trong một dòng; không chứng minh tổng hoàn hay khoảng thời gian không chồng.
- FK: hiện thực đủ 64 tham chiếu, không cascade. SourceType/SourceId của StockMovement là tham chiếu đa hình **không có FK đến nguồn**; dịch vụ hiện có ghi PreparationUnit, seed ghi DemoSeed. Các nguồn bổ sung phải được xác minh bởi handler.
- UNIQUE có lọc: bảo vệ ca/phiên đang mở và SessionId không NULL. UNIQUE thời điểm bắt đầu chỉ là khóa thay thế; không đủ chống chồng khoảng.
- Trigger có UPDLOCK/HOLDLOCK: bảo vệ khoảng PriceHistory/RecipeVersion, hạn mức hoàn và ca khi ghi thu/hoàn. Trigger khác bảo vệ đồng lượt/giới hạn UnitNumber, lịch sử, hủy cùng đơn. Các trigger nằm trong migration, được kiểm thử SQL Server, có thể rollback cả lệnh nhiều dòng.
- Transaction Serializable và khóa: AcceptRound giữ tồn/tạo phiếu nguyên khối; StartUnit xuất từ snapshot; CashPayment/CashRefund chống trùng với applock và khóa ca/thu gốc. Deadlock/timeout là thất bại cần tra cứu hoặc thử lại toàn command với cùng RequestId khi có; không báo thành công. Không bật retry tự động mù cho giao dịch tiền.
- RowVersion: 8 bảng ServiceSession, SalesOrder, OrderRound, PreparationUnit, Shift, StockBalance, RefundRequest, CancellationRequest. ExecuteUpdate kho dùng điều kiện atomic thay cho concurrency token cũ; EF tracked update dùng RowVersion.
- Sau lỗi command phải hủy scope/DbContext: transaction rollback không tự xóa các entity đã thay đổi khỏi ChangeTracker. Dịch vụ dành cho scope mới mỗi command; không dùng context song song.

## Quyết định hiện thực và điểm nhóm cần xác nhận

| Nội dung | Quyết định trong bộ mã này | Trạng thái |
|---|---|---|
| Khóa | BIGINT nghiệp vụ; GUID RequestId; string Identity Id NVARCHAR(450) | Phù hợp tùy chọn thiết kế |
| Tên bảng/cột | Giữ nguyên danh mục, PK/FK từ tài liệu | Không đổi tên; xem mapping |
| Identity | IdentityUserContext, 4 bảng auth, không role store Identity | Chỉ StaffRole/RolePermission cấp quyền nghiệp vụ |
| Một người vừa khách vừa nhân viên | Cho phép cùng Identity Id có mỗi loại tối đa một hồ sơ; không tự có quyền nhân viên | Chính sách cần nhóm xác nhận |
| RequestId Payment | Bắt buộc NOT NULL, UNIQUE, FK IdempotencyRecord; Refund cũng FK cùng bảng | Chốt kỹ thuật điểm tài liệu để ngỏ; cần nhóm xác nhận |
| Hash | SHA-256 trên JSON trường định danh/loại lệnh/actor; tiền chuẩn F2 invariant; GUID toàn cục cho thao tác | Cùng hash trả ID kết quả; khác hash lỗi; chưa có TTL/cleanup |
| Tiền | DECIMAL(18,2); primitive thu từ chối độ chính xác quá 2 số lẻ | Chưa có chính sách làm tròn toàn bill/giảm trừ |
| Kho | Một kho; thêm MovementType Reserve/Release để đối soát lượng giữ | Chưa có handler mọi loại biến động |
| IngredientBatch | Bảng + FK Ingredient, QuantityReceived/ExpiryDate; không FK Movement/Balance/Preparation đến Batch | **Chỉ metadata lô, không FIFO/FEFO hoặc truy xuất lô** |
| RecipeVersionId trên PreparationUnit | Thêm NOT NULL FK cho công thức chính; snapshot nguyên liệu gộp addon | Mở rộng từ điển để truy vết; dư thừa cần rà FD |
| Tiền và duyệt | Thêm CreatedBy vào Payment/Refund; CreatedBy/CreatedAt vào Adjustment; RequestedBy/ApprovedBy/ApprovedAt vào RefundRequest | Cần để lưu actor, thời gian và duyệt độc lập |
| Audit | Thêm EntityReference/Reason tùy chọn, chỉ ghi thêm | Handler sau này phải ghi đủ sự kiện, không đưa bí mật vào lý do |
| Ngừng sử dụng | Thêm IsActive cho Category, ProductVariant, Ingredient; giữ Product/Staff/Counter.IsActive đã có | Handler CRUD kiểm tra trạng thái; không xóa bản ghi được tham chiếu |
| Trạng thái | Dùng mã tiếng Anh trong CHECK, theo vòng đời báo cáo; phương thức Cash/BankTransfer | Nhóm xác nhận trước khi xây UI; đổi tập giá trị cần migration |
| Nghiệp vụ tài chính hiện có | Primitive ghi **tiền mặt đã xác minh**, không tự tính bill, không đóng đơn, không gọi ngân hàng | Chưa thể gọi là UC08/UC12 hoàn chỉnh |
| Thu chi khác | OtherIn/OtherOut, Posted/Voided, không tự nhân đôi số từ thu/hoàn trong đối soát | Chưa có kiểm tra nguồn chống người dùng nhập trùng thu/hoàn |
| Nguyên tắc bất biến | SQL bảo vệ payment/refund thành công, stock/audit, quyết định hủy và request hoàn đã active | Dữ liệu khác còn cần handler kiểm tra trạng thái/quyền |
| Chuẩn hóa | Có dictionary và FD từng quan hệ; PreparationUnit có dư thừa theo thiết kế | Không kết luận toàn CSDL 3NF; xem phân tích riêng |

Ngày giờ nghiệp vụ lưu UTC DATETIME2; Identity.LockoutEnd dùng DATETIMEOFFSET theo framework. DTO/UI sau này hiển thị múi giờ Việt Nam. Mã chứng từ do primitive sinh GUID dạng N (32 ký tự) trong cột NVARCHAR(40); nhóm có thể bổ sung bộ sinh mã dễ đọc ở lớp UC.

## Chưa kiểm chứng

Chưa nghiệm thu UI Web/WinForms, cookie/antiforgery, các policy endpoints, quyền sở hữu đơn, mọi chuyển trạng thái, toàn bộ luồng hủy/hoàn/điều chỉnh, nhập/kiểm kê kho, đóng ca cạnh tranh thu tiền từ handler, hiệu năng NFR04, RPO/RTO, TLS máy chủ thật, in ấn hay ngân hàng. Kiểm thử cấu trúc Identity không phải kiểm thử toàn bộ đăng ký/đăng nhập Web/Desktop. Các dữ liệu test không phải dữ liệu vận hành được phê duyệt.

