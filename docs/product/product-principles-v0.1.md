# Product Principles v0.1

- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner

## 1. Simple on the surface, capable underneath

- SimpleStore phải xử lý nghiệp vụ thực tế đủ sâu nhưng không bắt người dùng hiểu toàn bộ complexity phía sau.
- Một hành động nghiệp vụ tự nhiên của người dùng nên để hệ thống tự duy trì các hệ quả liên quan như tồn kho, giá vốn, tiền, công nợ, lợi nhuận.
- Không được hiểu principle này là cắt bỏ chức năng chỉ để UI trông đơn giản.

## 2. Reliability before intelligence

- Thứ tự ưu tiên: giao dịch đúng → dữ liệu đúng → báo cáo đúng → giải thích đúng → khuyến nghị.
- AI/insight không được che giấu uncertainty hoặc đưa ra kết luận tự tin khi dữ liệu nền chưa đáng tin.
- Principle này bám trực tiếp Product Model DO → TRUST → UNDERSTAND & ACT.

## 3. Design around user outcomes, not software modules

- Module vẫn tồn tại trong domain/architecture nhưng không được mặc định quyết định UX.
- Ưu tiên thiết kế theo câu hỏi và outcome người dùng cần đạt, ví dụ: hôm nay bán được bao nhiêu, cần nhập gì, ai đang nợ, vì sao cuối ngày không khớp.

## 4. Explain instead of exposing complexity

- Không giả định người dùng hiểu các thuật ngữ như doanh thu, giá vốn, công nợ, doanh thu thuần, chênh lệch tồn.
- Khi có thể, giải thích số liệu bằng ngôn ngữ nghiệp vụ đời thường và cho phép drill-down/thuật ngữ chuẩn khi cần.

## 5. Protect the critical path

- Critical path tại quầy bán ưu tiên: tốc độ → độ ổn định → ít thao tác → khả năng phục hồi khi có lỗi.
- Dashboard hoặc trải nghiệm phụ không được đánh đổi critical workflow.

## 6. Information should lead toward action

- Khi cung cấp thông tin, hãy thiết kế để người dùng dễ chuyển từ hiểu → hành động.
- Không đồng nghĩa SimpleStore v1 phải có AI recommendation engine.
- Principle 2 luôn đứng trước Principle 6: không đủ dữ liệu thì không được giả vờ chắc chắn.

## Lưu ý đã được phê duyệt

“Dễ dùng” không phải Product Principle độc lập vì quá chung chung.

## Product Principle Evaluation Checklist

Checklist này ánh xạ một câu kiểm tra với mỗi Product Principle để dùng khi review feature:

1. **Simple on the surface, capable underneath:** Feature có xử lý đủ nghiệp vụ và tự duy trì các hệ quả liên quan mà không buộc người dùng hiểu complexity phía sau không?
2. **Reliability before intelligence:** Giao dịch, dữ liệu và báo cáo đã đủ đáng tin, đồng thời uncertainty đã được thể hiện trung thực trước khi giải thích hoặc khuyến nghị chưa?
3. **Design around user outcomes, not software modules:** Thiết kế có bắt đầu từ câu hỏi hoặc outcome người dùng cần đạt, thay vì để cấu trúc module quyết định trải nghiệm không?
4. **Explain instead of exposing complexity:** Thông tin có được giải thích bằng ngôn ngữ nghiệp vụ đời thường, đồng thời vẫn cho phép xem chi tiết và thuật ngữ chuẩn khi cần không?
5. **Protect the critical path:** Feature có bảo vệ tốc độ, độ ổn định, số thao tác và khả năng phục hồi của critical path tại quầy bán không?
6. **Information should lead toward action:** Thông tin có giúp người dùng tiến tới hành động phù hợp và tránh thể hiện sự chắc chắn khi dữ liệu chưa đủ không?

## Liên quan

- [`docs/product/product-model-do-trust-understand-act.md`](product-model-do-trust-understand-act.md)
- [`docs/product/current-user-journey-v0.1.md`](current-user-journey-v0.1.md)
- [`docs/product/pain-point-map-v0.1.md`](pain-point-map-v0.1.md)
