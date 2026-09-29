# Future Close Day Visual Concept v0.1

**Status:** D-101 records `FUTURE PRODUCT/DESIGN DIRECTION — NOT APPROVED FOR IMPLEMENTATION / REQUIRES SEPARATE DOMAIN REVIEW`.

![Illustrative future Close Day flow in the approved image](../references/end-of-day-v0.1.png)

The pictured **Xem số liệu → Kiểm tra → Xác nhận đóng ngày** flow, **Đóng ngày** button, **Đã đóng/Chưa đóng** badges, close time/actor and promise that figures are locked are illustrative. D-048 remains authoritative: current End-of-day is a Store-local date **query/report**, not a business-state transition, accounting close, day/period lock, cash closing balance, immutable snapshot or Close Day/Reopen Day workflow. A report date is not a close record. Current UI must not claim **Số liệu đã được khóa**, **Không thể sửa sau khi đóng** or **Ngày đã chốt**. Later corrections may legitimately affect historical reports under approved event-date/cutoff semantics.

Before any real closing workflow, a separate Product Owner domain decision must answer:

1. What does “closed” mean, and does it block Sale, Purchase, DebtPayment, Return or Void afterward? Are backdated transactions allowed?
2. Can a day be reopened? Who may close or reopen, and what authorization and audit evidence are required?
3. Is close an immutable event or a snapshot? If the report stays a live query, what does a close status guarantee?
4. How do corrections posted later, cross-day Returns and Voids affect closed-date and current-date figures?
5. What idempotency, concurrency and recovery rules protect close and reopen?
6. Is cash count/reconciliation required? Does close create a carry-forward balance or accounting-period lock?

D-101 answers none of these and does not authorize schema, API, Vue, backend or migration work for closing. Keep any future close action out of the current report until those semantics and permissions are separately approved.
