# MVP User Flows v0.1

- **Step:** 8 — MVP User Flows v0.1
- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Bước tiếp theo:** Step 9 — Domain Model

## Mục tiêu và ranh giới

Mô tả 6 MVP user flows end-to-end theo **User Action → System Behavior → Business Outcome → Failure Path → Recovery Path**.

Chưa thiết kế database schema, API contract, UI/wireframe chi tiết hoặc architecture implementation. Các tên trạng thái và operation bên dưới là yêu cầu hành vi nghiệp vụ, không phải enum, endpoint hoặc mô hình lưu trữ đã chốt.

Giữ nguyên Step 1–7 đã APPROVED. Không thêm feature, không mở rộng MVP và không thực hiện Step 9 trong tài liệu này. C7 tiếp tục chỉ chừa integration point, không build capability HĐĐT nội bộ. C14 chỉ là experiment nguy cơ sắp hết hàng; nhu cầu, value và willingness-to-pay chưa validated.

## Traceability tới functional scope

Các mã F-C, J, P và O tham chiếu [MVP Functional Scope v0.1](../capabilities/mvp-functional-scope-v0.1.md). Dải mã bao gồm hai đầu. Bảng này chỉ nối các flow với phạm vi đã duyệt; không đổi mức MUST/SHOULD của Step 7.

| Flow | Feature / yêu cầu liên quan | Capability | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| 1 — Setup + Product | F-C15-01–08, F-C2-06–07, F-C17-01–04 | C15, C2, C17 | J1, J2; P1 | O1, O2 |
| 2 — Purchase → Inventory | F-C3-01–08, F-C3-10, F-C6-03–04, F-C8-01, F-C8-04–06 | C3, C4, C6, C8–C11, C18 | J2, J3, J4; P3, P4 | O3, O5 |
| 3 — Sale → Payment → Print | F-C1-01–12, F-C6-01–02, F-C17-01–03, F-C18-01–06 | C1, C4, C6, C8–C11, C17, C18 | J1, J4; P2, P3 | O4, O5 |
| 4 — Return / Void | F-C5-01–08, F-C10-06, F-C11-01–08, F-C16-01, F-C16-04 | C5, C4, C6, C8–C11, C16, C18 | J5; P3, P5 | O6, O5 |
| 5 — End-of-day Reconciliation | F-C6-03–08, F-C10-01–06, F-C12-06, F-C13-01–02 | C6, C9, C10, C12, C13 | J4, J6; P6 | O7 |
| 6 — Hôm nay cửa hàng thế nào? | F-C12-01–07, F-C13-01–02, F-C14-01–04 | C12, C13, C14; dữ liệu từ C4, C6, C9, C10 | J2, J3, J4, J6; P4, P6 | O8, O9 |

C8, C11, C16 và C18 hỗ trợ xuyên suốt khi có thao tác tương ứng. Mỗi critical flow có Happy Path, Failure Path và Recovery Path; các bảng lỗi bên dưới áp dụng cùng nguyên tắc phục hồi chung ở cuối tài liệu.

## Flow 1 — Setup + Product

**Actor:** Owner.

### Happy Path

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Owner tạo cửa hàng. | Tạo kho chính. | Có cửa hàng và kho chính để bắt đầu. |
| Tải template sản phẩm, import sản phẩm + tồn đầu. | Validate dữ liệu bắt buộc, kiểu số và barcode trùng; báo rõ từng dòng lỗi và lý do. | Owner biết dữ liệu cần sửa trước khi xác nhận. |
| Owner xem kết quả validation và xác nhận. | Ghi nhận sản phẩm và tồn đầu, thể hiện rõ kết quả import. | Danh mục và tồn đầu được ghi nhận; không có import nửa vời mà người dùng không biết. |
| Test printer/scanner. | Cho phép kiểm tra in và quét trong phạm vi thiết bị đã duyệt. | Cửa hàng sẵn sàng bán. |

Import tiếp tục dùng template cố định của Step 7; không có mapping cột bằng AI hoặc importer tổng quát.

### Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Thiếu dữ liệu bắt buộc, sai kiểu số hoặc barcode trùng. | Báo dòng lỗi/lý do; Owner sửa dữ liệu, validate lại rồi xác nhận. |
| Import lỗi hoặc kết quả ghi nhận chưa rõ. | Làm rõ trạng thái và dữ liệu đã được ghi nhận trước khi thực hiện lại; không để Owner tưởng import đã hoàn tất khi kết quả chưa rõ. |
| Test printer/scanner không thành công. | Xử lý lỗi thiết bị rồi test lại; không coi lỗi thiết bị là lý do tạo lại dữ liệu sản phẩm/tồn đầu. |

Chính sách nhận toàn bộ file hay từng phần chưa được chốt ở bước này. Dù chọn cách nào, kết quả import phải rõ và không để người dùng không biết dữ liệu đã ghi nhận đến đâu.

## Flow 2 — Purchase → Inventory

**Actor:** Owner.

### Happy Path

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Chọn/tạo NCC, tạo phiếu nhập. | Ghi nhận NCC của phiếu nhập. | Có cơ sở ghi nhận hàng nhập và nghĩa vụ với NCC. |
| Thêm sản phẩm + số lượng + giá mua; nhập số đã trả. | Tính nợ NCC. | Owner thấy số đã trả và còn nợ trước khi hoàn tất. |
| Xác nhận hoàn tất. | Tăng tồn, cập nhật giá vốn và công nợ NCC nhất quán; lưu lịch sử nhập. | Purchase được Completed với đầy đủ hệ quả nghiệp vụ. |

**Business rule:** Purchase chỉ được coi là `Completed` khi inventory, cost và supplier balance đã phản ánh nhất quán. Không được báo operation thành công khi chỉ một phần đã cập nhật. Phương pháp giá vốn vẫn là câu hỏi chưa chốt.

### Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Một hệ quả tồn kho/giá vốn/công nợ chưa nhất quán. | Không báo Completed; xác định trạng thái operation và phục hồi nhất quán trước khi công nhận kết quả. |
| Timeout sau khi xác nhận hoặc gửi lại thao tác hoàn tất. | Kiểm tra identity của Complete Purchase; Completed thì trả kết quả cũ, Failed thì retry an toàn, Processing/Unknown thì không tùy tiện tạo operation mới. |

Retry cùng operation không tạo thêm phiếu nhập hoặc nhân đôi tác động tồn/giá vốn/công nợ.

## Flow 3 — Sale → Payment → Print

**Actor:** Cashier.

### Happy Path

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Quét/tìm hàng, thêm vào giỏ và thay đổi số lượng nếu cần. | Áp dụng giá bán hiện hành và tính tổng. | Có giao dịch bán để xác nhận thanh toán. |
| Chọn phương thức thanh toán, nhập số tiền đã nhận và xác nhận. | Tạo một sale duy nhất; giảm tồn; cập nhật doanh thu/tiền/công nợ nhất quán. | Sale Completed. |
| Tiếp tục in bill sau khi bán thành công. | In bill như hậu xử lý của sale đã Completed. | Có phiếu bán hàng; trạng thái in tách biệt với kết quả bán hàng. |

Double click không tạo 2 sale. Retry không tạo sale mới ngoài ý muốn. Chuyển khoản/QR vẫn chỉ là ghi nhận phương thức thanh toán theo Step 7.

### Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Double click hoặc gửi lại yêu cầu thanh toán. | Nhận diện cùng Complete Sale operation; chỉ một sale và một lần tác động nghiệp vụ. |
| Timeout, Cashier chưa biết sale đã thành công chưa. | Kiểm tra trạng thái operation theo quy tắc chung; không tạo sale mới tùy tiện. |
| Sale Completed nhưng Print Failed. | Hiển thị rõ bán hàng đã thành công, in thất bại; cho phép **Reprint** từ sale đã hoàn tất. |
| Lỗi trong critical operation hoặc external service làm gián đoạn xử lý. | Làm rõ trạng thái và phục hồi/retry an toàn; không báo thành công khi dữ liệu nội bộ nửa vời. |

**Print failure không rollback sale.** Reprint không phải Complete Sale lần nữa và không tạo sale mới.

## Flow 4 — Return / Void

### Return — Happy Path

**Actor:** User có quyền.

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Tìm sale gốc, chọn sản phẩm + số lượng trả. | Kiểm tra số lượng còn được trả dựa trên sale và các lần trả trước. | Không trả vượt số lượng thực tế còn có thể trả. |
| Chọn hàng có quay lại kho hay không; xác nhận tiền hoàn. | Ghi nhận lựa chọn restock và tiền hoàn, không tự đoán. | Hệ quả nghiệp vụ phản ánh lựa chọn đã xác nhận. |
| Xác nhận trả hàng. | Tạo Return transaction; cập nhật tồn, tiền/công nợ, doanh thu/lãi gộp; giữ nguyên sale gốc. | Giao dịch trả được ghi nhận và dữ liệu liên quan vẫn đúng. |

**Validation bắt buộc:**

Số lượng trả ≤ Số lượng đã bán − Số lượng đã trả trước đó.

| Lựa chọn | Hệ quả tồn kho |
| --- | --- |
| Return + Restock | Tồn tăng. |
| Return + No Restock | Tồn không tăng. |

### Return — Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Số lượng trả vượt số còn có thể trả. | Không hoàn tất Return sai; người dùng điều chỉnh về số lượng hợp lệ rồi xác nhận. |
| Chưa xác định restock hay no restock. | Yêu cầu người dùng chọn rõ; hệ thống không tự đoán. |
| User không có quyền. | Thao tác do người có quyền thực hiện; không bỏ qua giới hạn quyền. |
| Timeout hoặc retry Create Return. | Kiểm tra cùng operation; trả kết quả cũ nếu Completed, retry an toàn nếu Failed, không tạo operation mới khi Processing/Unknown. |

### Void — Happy Path

Void khác Return:

| Nghiệp vụ | Ý nghĩa |
| --- | --- |
| Return | Giao dịch ban đầu đúng, sau đó khách trả hàng. |
| Void | Giao dịch ban đầu không nên tồn tại về nghiệp vụ, ví dụ nhập nhầm và phát hiện sau khi hoàn tất. |

User có quyền tìm giao dịch gốc → ghi nhận lý do → xác nhận Void → hệ thống tạo Void/reversal transaction và phản ánh hệ quả nghiệp vụ nhất quán → giữ original transaction và audit reason.

Không hard-delete sale. Phải giữ đủ **Original transaction + Void/reversal transaction + Audit reason**.

MVP **có thể giới hạn chỉ Owner được Void transaction đã Completed**. Đây là giới hạn được cho phép trong nội dung duyệt, chưa tự diễn giải thành permission matrix hoặc quyền Void cho Cashier.

### Void — Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| User không có quyền hoặc thiếu lý do audit. | Chỉ tiếp tục với người có quyền và lý do được ghi nhận; không xóa/sửa trực tiếp sale gốc. |
| Timeout hoặc retry Void Transaction. | Kiểm tra cùng operation; không tạo nhiều Void/reversal cho một lần thao tác. |
| Hệ quả Void chưa nhất quán. | Không công nhận operation hoàn tất; phục hồi theo trạng thái operation, giữ nguyên dấu vết giao dịch gốc. |

Return / Void / Reverse ở đây là nghiệp vụ correction phù hợp với phạm vi đã duyệt, không mở thêm correction framework tổng quát.

## Flow 5 — End-of-day Reconciliation

**Actor:** Owner.

### Happy Path

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Owner mở cuối ngày. | Tổng hợp số đơn, doanh thu, tiền đã thu, khách còn nợ, đã trả NCC, nợ NCC phát sinh và lãi gộp ước tính. | Owner có số liệu để đối soát cuối ngày. |
| Đối chiếu với thực tế. | Giữ rõ ý nghĩa từng số liệu, đặc biệt doanh thu và tiền đã thu. | Owner biết cuối ngày có khớp không. |
| Nếu lệch, xem các giao dịch nguồn liên quan. | Cho phép truy về dữ liệu nguồn. | Có căn cứ tìm nguyên nhân chênh lệch. |

**Doanh thu ≠ tiền đã thu.** Ví dụ minh họa:

| Chỉ tiêu | Giá trị |
| --- | --- |
| Doanh thu | 8,5 triệu |
| Đã thu | 7,9 triệu |
| Khách còn nợ | 0,6 triệu |

Ví dụ không thay thế quy tắc tổng hợp đầy đủ cho các trường hợp trả/hủy hoặc các kỳ khác. Chỉ gọi là lãi gộp/lãi gộp ước tính khi chưa quản lý đầy đủ chi phí.

### Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Số liệu không khớp thực tế. | Xem giao dịch nguồn liên quan; nếu cần correction, dùng nghiệp vụ Return / Void / Reverse phù hợp có audit, không sửa trực tiếp transaction Completed. |
| Giao dịch liên quan chưa rõ trạng thái sau timeout. | Làm rõ trạng thái operation theo nguyên tắc chung trước khi kết luận kết quả đối soát. |

Không tự thêm quy trình khóa sổ, sổ cái hoặc hệ thống thu/chi ngoài scope đã duyệt.

## Flow 6 — “Hôm nay cửa hàng thế nào?”

**Actor:** Owner.

### Happy Path

| User Action | System Behavior | Business Outcome |
| --- | --- | --- |
| Owner mở SimpleStore. | Hiển thị doanh thu hôm nay, tiền đã thu, công nợ phát sinh, lãi gộp ước tính, số đơn và attention signal. | Owner hiểu tình hình cửa hàng hôm nay. |
| Xem signal “Nguy cơ sắp hết hàng”, chọn “Xem vì sao”. | Hiển thị evidence từ tồn hiện tại và tốc độ bán gần đây, cho phép xem chi tiết. | Owner hiểu dữ liệu dẫn đến kết luận và việc đáng chú ý. |

**C14 experiment duy nhất:** Nguy cơ sắp hết hàng.

Ví dụ minh họa: **“Coca có nguy cơ sắp hết” → Xem vì sao**.

| Evidence | Giá trị ví dụ |
| --- | --- |
| Tồn hiện tại | 6 |
| Bán trung bình gần đây | 4/ngày |

Các con số chỉ minh họa evidence; không chốt cửa sổ thời gian, công thức hoặc ngưỡng alert.

### Failure Path và Recovery Path

| Failure Path | Recovery Path |
| --- | --- |
| Dữ liệu chưa đủ tin cậy để đưa kết luận mạnh. | Không đưa recommendation mạnh; thể hiện giới hạn dữ liệu, xem evidence/chi tiết để hiểu căn cứ hiện có. |
| Số liệu khiến Owner chưa hiểu “vì sao”. | Truy về dữ liệu nguồn/giải thích trong phạm vi C13 và C14 đã duyệt. |

Không xây subsystem alert lớn, AI recommendation engine hoặc thêm hypothesis khác. Experiment chưa validated về nhu cầu, value hoặc willingness-to-pay.

## Cross-cutting Flow Principles đã APPROVED

### 1. Completed transaction is immutable

Transaction đã `Completed` không sửa trực tiếp, không hard-delete. Correction phải thông qua Return / Void / Reverse phù hợp và có audit.

### 2. Critical operation phải idempotent

Ít nhất gồm **Complete Sale, Complete Purchase, Create Return, Void Transaction**. Retry cùng một operation không tạo business transaction mới.

### 3. Critical operation phải có identity riêng

Mỗi critical business operation cần identity để nhận biết request cũ, kiểm tra trạng thái, retry an toàn và chống duplicate. Đây là business behavior requirement; chưa chốt implementation/API/database.

### 4. Timeout phải recover được

Client timeout không đồng nghĩa transaction thất bại.

Request timeout → kiểm tra trạng thái operation:

| Kết quả kiểm tra | Recovery Path |
| --- | --- |
| Completed | Trả lại kết quả cũ. |
| Failed | Cho phép retry an toàn cùng operation. |
| Vẫn Processing/Unknown từ góc nhìn client | Không tùy tiện tạo operation mới; tiếp tục làm rõ trạng thái operation hiện có. |

Unknown mô tả điều client chưa biết, không khẳng định nghiệp vụ đã thất bại. Quy tắc này chưa quyết định cơ chế kiểm tra trạng thái hoặc lưu identity.

### 5. Mọi critical flow có 3 lớp

Happy Path + Failure Path + Recovery Path là nguyên tắc xuyên toàn MVP.

### 6. Purchase consistency

Purchase chỉ `Completed` khi inventory, cost và supplier balance đã phản ánh nhất quán; không báo thành công với dữ liệu cập nhật một phần.

### 7. Return validation

Không được trả vượt quá số lượng còn có thể trả thực tế: số lượng đã bán trừ số lượng đã trả trước đó.

### 8. Printing không quyết định transaction success

Sale có thể `Completed` dù print thất bại. Hiển thị rõ sale thành công, print thất bại và cho phép Reprint. Print failure không rollback sale.

## Tài liệu liên quan

- [MVP Functional Scope v0.1](../capabilities/mvp-functional-scope-v0.1.md)
- [MVP Scope v0.1](../product/mvp-scope-v0.1.md)
- [Decision Log](../../DECISIONS.md)
- [Project State](../../PROJECT_STATE.md)
- [Open Questions](../../OPEN_QUESTIONS.md)
