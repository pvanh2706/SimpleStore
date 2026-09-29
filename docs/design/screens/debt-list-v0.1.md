# Debt List and Filter v0.1

**Status:** Approved visual/product UX direction — D-100. **Redesign implementation:** NOT STARTED.

![Approved Debt overview and filter reference](../references/debt-management-v0.1.png)

The unified **Công nợ** screen uses an unmistakable Customer/Supplier mode switch. Keep the current money direction visible: customers owe the Store; the Store owes suppliers. Headline facts may include the number of parties with current outstanding, total current Customer debt or total current Supplier debt in the selected mode, and a backend `asOf`/cutoff. These aggregates need an authoritative API and [D-096 explanation](debt-explainability-v0.1.md); do not fabricate them from a single page of results.

Search Customer by name/phone and Supplier by name/phone where present, using the existing debt endpoints with stable pagination and Store isolation. Preferred scan-friendly columns are party name, phone, current outstanding, `asOf` where useful, last relevant activity **only if supported**, and an authorized **Thu tiền** or **Trả tiền** action. Use tabular numerals and keep long Vietnamese names readable. The current `DebtBalance` result guarantees party, phone, outstanding and `asOf`; it does **not** guarantee mockup **Tổng phát sinh**, **Đã thanh toán**, overdue days or last-transaction columns.

A focused filter panel may visually present party type, outstanding range, transaction date range and debt/payment state, but expose only implemented, approved filters. Current debt list APIs support `search`, `page` and `pageSize`; additional filters need API/technical review. **Quá hạn**, **Sắp đến hạn**, “> 7 ngày”, “≤ 7 ngày”, due-date and aging filters are `FUTURE PRODUCT/DESIGN DIRECTION — REQUIRES SEPARATE DOMAIN REVIEW`, **NOT APPROVED FOR IMPLEMENTATION** by D-100. Current domain has no DueDate, PaymentTermDays, DaysOverdue or AgingBucket; transaction age must not be used as an invented due date.

Mockup **Tổng phát sinh** and **Đã thanh toán** are visual/product direction only. A later review must define time scope, original Sale/Purchase payments, standalone DebtPayments, Return/Void/refunds, cross-day corrections and the difference between historical and current totals. Do not assume `outstanding = illustrated total incurred − illustrated total paid` without those definitions. Distinguish no debt from no filtered results, loading, API failure and unauthorized mode. Cashier must not see the Owner-only Supplier mode as a usable action merely because both modes share one design.
