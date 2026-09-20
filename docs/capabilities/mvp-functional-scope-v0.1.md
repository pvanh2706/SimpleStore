# MVP Functional Scope v0.1

- **Step:** 7 — Capability Decomposition / Functional Scope v0.1
- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Bước tiếp theo:** Step 8 — MVP User Flows

## Mục tiêu và ranh giới

Bóc phạm vi MVP thành functional scope cụ thể. Chưa thiết kế database schema, API contract, UI chi tiết hoặc architecture implementation.

Giữ nguyên Step 1–6 đã APPROVED và boundary MVP: 1 cửa hàng, 1 kho chính, 1 chủ cửa hàng và một số nhân viên bán hàng. Không tự thêm feature hoặc mở rộng MVP.

Mỗi chức năng phải trace được: **Feature → Capability → JTBD / Pain Point → MVP Outcome**. Feature không trace được mặc định không đưa vào MVP cho tới khi chứng minh được lý do. Traceability chỉ giải thích phạm vi đã duyệt, không tự chứng minh hypothesis đã validated.

## Quy ước traceability

- `F-Cn-xx`: mã tham chiếu chức năng/yêu cầu trong tài liệu này; capability được ghi trong mã và tiêu đề bảng.
- `J1–J7`: ký hiệu tham chiếu tới đúng thứ tự 1–7 trong [JTBD v0.1](../product/jobs-to-be-done-v0.1.md), không tạo hoặc sửa JTBD.
- `P1–P7`: mã đã có trong [Pain Point Map v0.1](../product/pain-point-map-v0.1.md).
- `O1–O9`: nhãn tham chiếu cho các outcome có sẵn trong [MVP Scope v0.1](../product/mvp-scope-v0.1.md), không phải outcome mới.
- MUST và SHOULD giữ nguyên mức ưu tiên đã duyệt. “MVP tối thiểu” ghi phạm vi tối thiểu ở C6/C12/C16; “EXPERIMENT” là phạm vi thử nghiệm C14; “BOUNDARY” là ràng buộc C7/C19.
- Một yêu cầu được nhắc ở nhiều capability là cùng hệ quả nghiệp vụ cần bảo đảm, không phải yêu cầu xây nhiều module/subsystem riêng.

| Tham chiếu | JTBD đã duyệt |
| --- | --- |
| J1 | Bán hàng nhanh và chính xác, không để khách phải chờ. |
| J2 | Biết tồn kho có đáng tin không và hàng nào thiếu/dư/lệch. |
| J3 | Biết nên nhập hàng gì, bao nhiêu và khi nào. |
| J4 | Biết tiền và công nợ đang ở đâu, cuối ngày có khớp không. |
| J5 | Xử lý các ngoại lệ như trả hàng, hủy nhầm, lệch kho mà không làm sai dữ liệu. |
| J6 | Hiểu cửa hàng thực sự đang lời/lỗ ở đâu. |
| J7 | Đáp ứng HĐĐT/thuế cho nhóm cửa hàng cần áp dụng mà không làm gián đoạn việc bán. |

| Mã | Pain Point đã duyệt |
| --- | --- |
| P1 | Khó bắt đầu đúng |
| P2 | Bán hàng không được phép chậm |
| P3 | Một thao tác sai làm sai nhiều thứ |
| P4 | Không chắc tồn kho có đáng tin hay không |
| P5 | Ngoại lệ khó hơn luồng bình thường |
| P6 | Có số liệu nhưng chưa chắc hiểu tình hình |
| P7 | Compliance chen vào workflow bán hàng |

| Mã | MVP Outcome |
| --- | --- |
| O1 | Khởi tạo cửa hàng |
| O2 | Tạo/import hàng |
| O3 | Nhập hàng |
| O4 | Bán + thanh toán + in |
| O5 | Tồn / tiền / giá vốn tự cập nhật |
| O6 | Xử lý trả hoặc sửa một giao dịch |
| O7 | Đối soát cuối ngày |
| O8 | Chủ cửa hàng trả lời được “Hôm nay cửa hàng thế nào?” |
| O9 | Biết ít nhất một việc đáng chú ý |

O6 được cụ thể hóa bằng trả/void và dấu vết nghiệp vụ; không sửa trực tiếp transaction đã hoàn tất, không xây correction framework tổng quát. J6 được đáp ứng ở mức lãi gộp/lãi gộp ước tính trong MVP, chưa phải lợi nhuận sau toàn bộ chi phí.

## Functional scope đã APPROVED

### C1 — Sales & Checkout

**Mức capability theo MVP:** CORE.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C1-01 | MUST | Tìm sản phẩm theo tên/mã. | J1, J4; P2, P3 | O4, O5 |
| F-C1-02 | MUST | Quét barcode. | J1, J4; P2, P3 | O4, O5 |
| F-C1-03 | MUST | Thêm/xóa sản phẩm khỏi giỏ. | J1, J4; P2, P3 | O4, O5 |
| F-C1-04 | MUST | Thay đổi số lượng. | J1, J4; P2, P3 | O4, O5 |
| F-C1-05 | MUST | Áp dụng giá bán hiện hành. | J1, J4; P2, P3 | O4, O5 |
| F-C1-06 | MUST | Ghi nhận thanh toán tiền mặt. | J1, J4; P2, P3 | O4, O5 |
| F-C1-07 | MUST | Ghi nhận thanh toán chuyển khoản/QR ở mức phương thức thanh toán. | J1, J4; P2, P3 | O4, O5 |
| F-C1-08 | MUST | Hoàn tất giao dịch. | J1, J4; P2, P3 | O4, O5 |
| F-C1-09 | MUST | In phiếu bán hàng. | J1, J4; P2, P3 | O4, O5 |
| F-C1-10 | MUST | Tự giảm tồn. | J1, J4; P2, P3 | O4, O5 |
| F-C1-11 | MUST | Tự ghi nhận doanh thu/tiền thu. | J1, J4; P2, P3 | O4, O5 |
| F-C1-12 | MUST | Chống double-submit/double-sale. | J1, J4; P2, P3 | O4, O5 |
| F-C1-13 | SHOULD | Giảm giá đơn giản. | J1, J4; P2, P3 | O4, O5 |
| F-C1-14 | SHOULD | Ghi chú giao dịch. | J1, J4; P2, P3 | O4, O5 |
| F-C1-15 | SHOULD | Gắn khách hàng khi cần công nợ. | J1, J4; P2, P3 | O4, O5 |

### C2 — Product & Pricing Management

**Mức capability theo MVP:** CORE.

Không làm pricing engine phức tạp, nhiều bảng giá hoặc promotion engine lớn trong MVP.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C2-01 | MUST | Tạo/sửa/ngừng sử dụng sản phẩm. | J1, J2; P1, P4 | O2, O5 |
| F-C2-02 | MUST | Tên, mã/SKU, barcode. | J1, J2; P1, P4 | O2, O5 |
| F-C2-03 | MUST | Đơn vị tính cơ bản. | J1, J2; P1, P4 | O2, O5 |
| F-C2-04 | MUST | Giá mua tham chiếu. | J1, J2; P1, P4 | O2, O5 |
| F-C2-05 | MUST | Giá bán. | J1, J2; P1, P4 | O2, O5 |
| F-C2-06 | MUST | Tồn đầu kỳ. | J1, J2; P1, P4 | O2, O5 |
| F-C2-07 | MUST | Import sản phẩm từ Excel/CSV theo template chuẩn. | J1, J2; P1, P4 | O2, O5 |
| F-C2-08 | SHOULD | Nhóm hàng. | J1, J2; P1, P4 | O2, O5 |
| F-C2-09 | SHOULD | Nhiều barcode cho một sản phẩm nếu pilot thực tế cần. | J1, J2; P1, P4 | O2, O5 |
| F-C2-10 | SHOULD | Đơn vị quy đổi đơn giản. | J1, J2; P1, P4 | O2, O5 |

### C3 — Purchasing & Supplier Management

**Mức capability theo MVP:** CORE.

Không cần xây workflow trả hàng NCC phức tạp trong pilot đầu. Phương pháp giá vốn chưa được chọn ở bước này.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C3-01 | MUST | Tạo/quản lý nhà cung cấp. | J3, J4; P3, P4 | O3, O5 |
| F-C3-02 | MUST | Tạo phiếu nhập hàng. | J3, J4; P3, P4 | O3, O5 |
| F-C3-03 | MUST | Chọn sản phẩm + số lượng. | J3, J4; P3, P4 | O3, O5 |
| F-C3-04 | MUST | Nhập giá mua. | J3, J4; P3, P4 | O3, O5 |
| F-C3-05 | MUST | Ghi nhận đã thanh toán hoặc còn nợ NCC. | J3, J4; P3, P4 | O3, O5 |
| F-C3-06 | MUST | Hoàn tất phiếu nhập. | J3, J4; P3, P4 | O3, O5 |
| F-C3-07 | MUST | Tự tăng tồn. | J3, J4; P3, P4 | O3, O5 |
| F-C3-08 | MUST | Tự cập nhật giá vốn theo phương pháp được chọn sau này. | J3, J4; P3, P4 | O3, O5 |
| F-C3-09 | SHOULD | Chiết khấu/phụ phí đơn giản. | J3, J4; P3, P4 | O3, O5 |
| F-C3-10 | SHOULD | Xem lịch sử nhập theo NCC/sản phẩm. | J3, J4; P3, P4 | O3, O5 |

### C4 — Inventory Operations

**Mức capability theo MVP:** CORE.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C4-01 | MUST | Xem tồn hiện tại. | J2; P3, P4 | O5, O7 |
| F-C4-02 | MUST | Xem lịch sử tăng/giảm tồn. | J2; P3, P4 | O5, O7 |
| F-C4-03 | MUST | Tồn tăng từ nhập hàng. | J2; P3, P4 | O5, O7 |
| F-C4-04 | MUST | Tồn giảm từ bán hàng. | J2; P3, P4 | O5, O7 |
| F-C4-05 | MUST | Điều chỉnh tồn có lý do. | J2; P3, P4 | O5, O7 |
| F-C4-06 | MUST | Kiểm kho. | J2; P3, P4 | O5, O7 |
| F-C4-07 | MUST | Ghi nhận chênh lệch sau kiểm kho. | J2; P3, P4 | O5, O7 |
| F-C4-08 | MUST | Truy được lý do vì sao tồn thay đổi. | J2; P3, P4 | O5, O7 |
| F-C4-09 | SHOULD | Lọc hàng sắp hết. | J2; P3, P4 | O5, O7 |
| F-C4-10 | SHOULD | Một số tín hiệu tồn đơn giản. | J2; P3, P4 | O5, O7 |

### C5 — Returns & Exception Handling

**Mức capability theo MVP:** CORE-LITE.

Scope đã cắt cho pilot. Không xây correction framework tổng quát trong MVP.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C5-01 | MUST | Tìm giao dịch gốc. | J5; P3, P5 | O6, O5 |
| F-C5-02 | MUST | Trả toàn bộ hoặc một phần. | J5; P3, P5 | O6, O5 |
| F-C5-03 | MUST | Chọn số lượng hàng trả. | J5; P3, P5 | O6, O5 |
| F-C5-04 | MUST | Ghi nhận số tiền hoàn. | J5; P3, P5 | O6, O5 |
| F-C5-05 | MUST | Xác định hàng có quay lại kho hay không. | J5; P3, P5 | O6, O5 |
| F-C5-06 | MUST | Tự cập nhật tồn/tiền/công nợ tương ứng. | J5; P3, P5 | O6, O5 |
| F-C5-07 | MUST | Không sửa trực tiếp transaction đã hoàn tất. | J5; P3, P5 | O6, O5 |
| F-C5-08 | MUST | Void/hủy giao dịch phải tạo dấu vết audit. | J5; P3, P5 | O6, O5 |

### C6 — Money & Debt Management

**Mức capability theo MVP:** CORE-LITE.

Scope đã cắt mạnh; các dòng dưới là phạm vi MVP chỉ cần. Không làm accounting đầy đủ, sổ cái, kế toán kép, hệ thống quỹ phức tạp hoặc category thu/chi phong phú.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C6-01 | MVP tối thiểu | Sale: ghi nhận đã thu bao nhiêu. | J4; P3, P6 | O5, O7 |
| F-C6-02 | MVP tối thiểu | Sale: ghi nhận còn khách nợ bao nhiêu. | J4; P3, P6 | O5, O7 |
| F-C6-03 | MVP tối thiểu | Purchase: ghi nhận đã trả NCC bao nhiêu. | J4; P3, P6 | O5, O7 |
| F-C6-04 | MVP tối thiểu | Purchase: ghi nhận còn nợ NCC bao nhiêu. | J4; P3, P6 | O5, O7 |
| F-C6-05 | MVP tối thiểu | Cuối ngày: doanh thu. | J4; P3, P6 | O5, O7 |
| F-C6-06 | MVP tối thiểu | Cuối ngày: tiền đã thu. | J4; P3, P6 | O5, O7 |
| F-C6-07 | MVP tối thiểu | Cuối ngày: khách còn nợ. | J4; P3, P6 | O5, O7 |
| F-C6-08 | MVP tối thiểu | Cuối ngày: chi nhập hàng / số đã trả NCC. | J4; P3, P6 | O5, O7 |

### C8 — Transaction Integrity

**Mức capability theo MVP:** CORE.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C8-01 | MUST | Một business action phải tạo ra một kết quả nhất quán. | J1, J4, J5; P3, P5 | O4, O5, O6 |
| F-C8-02 | MUST | Không có sale thành công nhưng tồn chưa giảm. | J1, J4, J5; P3, P5 | O4, O5, O6 |
| F-C8-03 | MUST | Không ghi thanh toán hai lần. | J1, J4, J5; P3, P5 | O4, O5, O6 |
| F-C8-04 | MUST | Retry không tạo transaction mới ngoài ý muốn. | J1, J4, J5; P3, P5 | O4, O5, O6 |
| F-C8-05 | MUST | Critical operation cần idempotency. | J1, J4, J5; P3, P5 | O4, O5, O6 |
| F-C8-06 | MUST | Transaction không hoàn tất phải có trạng thái rõ. | J1, J4, J5; P3, P5 | O4, O5, O6 |

### C9 — Inventory Confidence

**Mức capability theo MVP:** CORE.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C9-01 | MUST | Mọi biến động tồn có nguồn. | J2; P4 | O5, O7 |
| F-C9-02 | MUST | Adjustment có lý do. | J2; P4 | O5, O7 |
| F-C9-03 | MUST | Có thể drill-down lịch sử tăng/giảm tồn. | J2; P4 | O5, O7 |
| F-C9-04 | MUST | Người dùng hiểu được vì sao tồn hiện tại có giá trị đó. | J2; P4 | O5, O7 |

### C10 — Financial & Profit Integrity

**Mức capability theo MVP:** CORE.

MVP chỉ gọi là **lãi gộp / lãi gộp ước tính** khi chưa quản lý đầy đủ toàn bộ chi phí; không diễn đạt thành lợi nhuận ròng.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C10-01 | MUST | Phân biệt rõ doanh thu. | J4, J6; P3, P6 | O5, O6, O7, O8 |
| F-C10-02 | MUST | Phân biệt rõ tiền đã thu. | J4, J6; P3, P6 | O5, O6, O7, O8 |
| F-C10-03 | MUST | Phân biệt rõ công nợ. | J4, J6; P3, P6 | O5, O6, O7, O8 |
| F-C10-04 | MUST | Phân biệt rõ giá vốn. | J4, J6; P3, P6 | O5, O6, O7, O8 |
| F-C10-05 | MUST | Phân biệt rõ lãi gộp. | J4, J6; P3, P6 | O5, O6, O7, O8 |
| F-C10-06 | MUST | Trả/hủy phải phản ánh đúng. | J4, J6; P3, P6 | O5, O6, O7, O8 |

### C11 — Auditability & Safe Recovery

**Mức capability theo MVP:** CORE-LITE.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C11-01 | MUST | Ghi nhận ai thực hiện. | J5; P3, P5 | O6, O5 |
| F-C11-02 | MUST | Ghi nhận khi nào. | J5; P3, P5 | O6, O5 |
| F-C11-03 | MUST | Ghi nhận loại thao tác. | J5; P3, P5 | O6, O5 |
| F-C11-04 | MUST | Ghi nhận đối tượng thay đổi. | J5; P3, P5 | O6, O5 |
| F-C11-05 | MUST | Ghi nhận lý do với thao tác nhạy cảm. | J5; P3, P5 | O6, O5 |
| F-C11-06 | MUST | Không hard-delete transaction quan trọng. | J5; P3, P5 | O6, O5 |
| F-C11-07 | SHOULD | Before/after cho thay đổi quan trọng. | J5; P3, P5 | O6, O5 |
| F-C11-08 | SHOULD | Phục hồi thông qua reverse/adjustment nghiệp vụ. | J5; P3, P5 | O6, O5 |

### C12 — Business Situation Understanding

**Mức capability theo MVP:** DIFFERENTIATOR-LITE.

Các dòng dưới là mức hiển thị tối thiểu. Không cần dashboard nhiều biểu đồ.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C12-01 | MVP tối thiểu | Entry point: “Hôm nay cửa hàng thế nào?” | J2, J4, J6; P6 | O8 |
| F-C12-02 | MVP tối thiểu | Hiển thị doanh thu hôm nay. | J2, J4, J6; P6 | O8 |
| F-C12-03 | MVP tối thiểu | Hiển thị tiền đã thu. | J2, J4, J6; P6 | O8 |
| F-C12-04 | MVP tối thiểu | Hiển thị công nợ phát sinh. | J2, J4, J6; P6 | O8 |
| F-C12-05 | MVP tối thiểu | Hiển thị lãi gộp ước tính. | J2, J4, J6; P6 | O8 |
| F-C12-06 | MVP tối thiểu | Hiển thị số đơn. | J2, J4, J6; P6 | O8 |
| F-C12-07 | MVP tối thiểu | Hiển thị tín hiệu tồn quan trọng. | J2, J4, J6; P6 | O8 |

### C13 — Explainable Business Insights

**Mức capability theo MVP:** DIFFERENTIATOR-LITE.

Không cần AI.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C13-01 | SHOULD | Người dùng có thể hiểu “vì sao” một số quan trọng có giá trị đó. | J4, J6; P6 | O8 |
| F-C13-02 | SHOULD | Giải thích dựa trên dữ liệu nguồn. | J4, J6; P6 | O8 |

### C14 — Attention & Decision Support

**Mức capability theo MVP:** EXPERIMENT.

EXPERIMENT rất mỏng, ưu tiên rule-based/deterministic. Không đưa recommendation mạnh nếu dữ liệu chưa đủ tin cậy. Không xây subsystem alert lớn hoặc AI recommendation engine. Nhu cầu, value và willingness-to-pay chưa validated; trace tới J2/J3 không biến Differentiator Hypothesis thành JTBD đã được phê duyệt.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C14-01 | EXPERIMENT | Chỉ một hypothesis: nguy cơ sắp hết hàng, dựa trên tồn hiện tại và tốc độ bán gần đây. | J2, J3; P4, P6 | O9 |
| F-C14-02 | EXPERIMENT | Alert cho thấy điều gì xảy ra. | J2, J3; P4, P6 | O9 |
| F-C14-03 | EXPERIMENT | Alert cho thấy dữ liệu nào dẫn đến kết luận. | J2, J3; P4, P6 | O9 |
| F-C14-04 | EXPERIMENT | Người dùng có thể xem chi tiết. | J2, J3; P4, P6 | O9 |

### C15 — Store Setup & Data Onboarding

**Mức capability theo MVP:** CORE.

Import chỉ dùng template cố định, ví dụ: `Barcode | Tên hàng | Đơn vị | Giá nhập | Giá bán | Tồn đầu`. Không xây AI mapping cột, tự đoán cấu trúc Excel, importer tổng quát hoặc merge dữ liệu phức tạp. Trong pilot đầu, Product Owner có thể hỗ trợ migration dữ liệu thủ công khi cần.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C15-01 | MUST | Tạo thông tin cửa hàng. | J1, J2; P1 | O1, O2 |
| F-C15-02 | MUST | Khởi tạo kho chính. | J1, J2; P1 | O1, O2 |
| F-C15-03 | MUST | Import danh mục sản phẩm. | J1, J2; P1 | O1, O2 |
| F-C15-04 | MUST | Import tồn đầu kỳ. | J1, J2; P1 | O1, O2 |
| F-C15-05 | MUST | Validate dữ liệu bắt buộc. | J1, J2; P1 | O1, O2 |
| F-C15-06 | MUST | Phát hiện barcode trùng. | J1, J2; P1 | O1, O2 |
| F-C15-07 | MUST | Validate kiểu số. | J1, J2; P1 | O1, O2 |
| F-C15-08 | MUST | Báo rõ dòng lỗi và lý do. | J1, J2; P1 | O1, O2 |

### C16 — User & Permission Management

**Mức capability theo MVP:** CORE-LITE.

MVP chỉ có 2 role: Owner và Cashier. Không xây permission matrix enterprise.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C16-01 | MVP tối thiểu | Owner: toàn quyền nghiệp vụ MVP. | J1, J5; P2, P3 | O1, O4, O6 |
| F-C16-02 | MVP tối thiểu | Cashier: bán hàng. | J1, J5; P2, P3 | O1, O4, O6 |
| F-C16-03 | MVP tối thiểu | Cashier: xem thông tin cần cho bán hàng. | J1, J5; P2, P3 | O1, O4, O6 |
| F-C16-04 | MVP tối thiểu | Cashier: hạn chế quyền sửa/hủy/điều chỉnh. | J1, J5; P2, P3 | O1, O4, O6 |

### C17 — Device & Peripheral Integration

**Mức capability theo MVP:** CORE.

Reprint xuất hiện ở mức SHOULD tại C17 nhưng khả năng reprint khi xử lý lỗi là MUST tại C18; không được bỏ yêu cầu phục hồi này.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C17-01 | MUST | Barcode scanner kiểu keyboard scanner phổ biến. | J1; P2 | O4 |
| F-C17-02 | MUST | In bill. | J1; P2 | O4 |
| F-C17-03 | MUST | Hỗ trợ khổ giấy mục tiêu. | J1; P2 | O4 |
| F-C17-04 | SHOULD | Test printer. | J1; P2 | O4 |
| F-C17-05 | SHOULD | Preview/reprint bill. | J1; P2 | O4 |

### C18 — Operational Resilience

**Mức capability theo MVP:** CORE-LITE.

Không làm full offline trong MVP.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C18-01 | MUST | Double click thanh toán không tạo 2 sale. | J1, J5; P2, P3, P5 | O4, O5, O6 |
| F-C18-02 | MUST | Timeout phải cho biết trạng thái giao dịch. | J1, J5; P2, P3, P5 | O4, O5, O6 |
| F-C18-03 | MUST | Retry an toàn. | J1, J5; P2, P3, P5 | O4, O5, O6 |
| F-C18-04 | MUST | External service lỗi không làm transaction nội bộ nửa vời. | J1, J5; P2, P3, P5 | O4, O5, O6 |
| F-C18-05 | MUST | Printer lỗi không làm mất sale đã hoàn tất. | J1, J5; P2, P3, P5 | O4, O5, O6 |
| F-C18-06 | MUST | Có thể reprint. | J1, J5; P2, P3, P5 | O4, O5, O6 |

### C7 — E-Invoice & Tax Compliance

**Mức capability theo MVP:** INTEGRATION BOUNDARY.

C7 không build trong MVP. Dòng dưới là ràng buộc duy nhất hiện tại để chừa integration point, không phải feature phát hành HĐĐT hay API contract. Không yêu cầu kết nối service ngay trong MVP.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C7-01 | BOUNDARY | Domain của sale phải đủ sạch để sau này ánh xạ sang request của service API HĐĐT hiện có mà không phá cấu trúc giao dịch. | J7; P7 | O4 |

### C19 — External Integration

**Mức capability theo MVP:** LIMITED.

Đây là ranh giới phạm vi, không phải danh sách integration mới được phê duyệt. Mỗi integration cụ thể vẫn phải trace theo outcome thực sự phục vụ; chưa chọn thêm integration ở bước này.

| Feature / yêu cầu | Mức | Nội dung | JTBD / Pain Point | MVP Outcome |
| --- | --- | --- | --- | --- |
| F-C19-01 | BOUNDARY | Chỉ xây integration thực sự cần cho vòng MVP. | J1; P2, P3 | O4, O5 |

## Vertical slices đã APPROVED

Triển khai theo 6 vertical slice; ưu tiên end-to-end outcome thay vì module completeness. Capability hỗ trợ có thể xuyên nhiều slice; bảng này không chốt architecture hoặc tách module.

| Thứ tự | Vertical slice | Capability liên quan | Outcome |
| --- | --- | --- | --- |
| 1 | Setup + Product | C15, C2, C16; C8–C11 cho dữ liệu liên quan | O1, O2, O5 |
| 2 | Purchase → Inventory | C3, C4, C6, C8–C11, C16, C18 | O3, O5 |
| 3 | Sale → Payment → Print | C1, C2, C4, C6, C8–C11, C16–C18; ranh giới C7/C19 | O4, O5 |
| 4 | Return / Void → dữ liệu vẫn đúng | C5, C4, C6, C8–C11, C16, C18 | O6, O5 |
| 5 | End-of-day → tiền + tồn + lãi gộp | C4, C6, C9–C11 | O7 |
| 6 | “Hôm nay cửa hàng thế nào?” + 1 attention experiment | C12, C13, C14; dữ liệu từ C4, C6, C9, C10 | O8, O9 |

DO và TRUST phải đủ chắc trước khi đẩy mạnh UNDERSTAND & ACT. C8–C11 là nền tảng TRUST, không phải backend nice-to-have.

## Các ranh giới cần giữ khi làm bước tiếp theo

- Chuyển khoản/QR chỉ ở mức ghi nhận phương thức thanh toán; không tự thêm tích hợp ngân hàng/đối soát tự động.
- Giá vốn theo phương pháp sẽ chọn sau; chưa chốt phương pháp hoặc công thức ở Step 7.
- Import dùng template cố định; không suy diễn ví dụ template thành database schema hoặc importer tổng quát.
- Owner/Cashier là hai role MVP; quyền sửa/hủy/điều chỉnh cụ thể cần được làm rõ trong user flows, không tạo permission matrix enterprise.
- C14 chỉ thử nghiệm nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây. Chưa chốt cửa sổ dữ liệu/ngưỡng; chưa validated nhu cầu, value hoặc willingness-to-pay.
- C7 chỉ chừa integration point qua domain sale đủ sạch, không xây capability HĐĐT nội bộ.
- Các nội dung ngoài MVP ở Step 6 tiếp tục giữ nguyên.
- Step 8 — MVP User Flows là bước tiếp theo; tài liệu này không thực hiện Step 8.

## Tài liệu liên quan

- [Capability Map v0.1](capability-map-v0.1.md)
- [MVP Scope v0.1](../product/mvp-scope-v0.1.md)
- [JTBD v0.1](../product/jobs-to-be-done-v0.1.md)
- [Pain Point Map v0.1](../product/pain-point-map-v0.1.md)
- [Decision Log](../../DECISIONS.md)
- [Project State](../../PROJECT_STATE.md)
- [Open Questions](../../OPEN_QUESTIONS.md)
