# Settings / Cài đặt v0.1

**Status:** Product Owner-approved design direction — D-102, 2026-09-29. **Implementation:** NOT STARTED. This specification organizes approved capabilities; it does not approve new domain, API, authentication or printer behavior.

## Purpose and source of truth

Settings uses the **Modern Retail Utility + Friendly Local Commerce** direction of [D-095 Design System v0.1](../simple-store-design-system-v0.1.md): utility-first, calm, trustworthy and understandable for a small Store. It is an administration workspace, not an enterprise console. Explain the business consequence of a setting where it matters. Existing domain/backend and authorization contracts, D-095 appearance behavior and [D-096 Explainability Pattern](explainability-pattern-v0.1.md) remain authoritative; written behavior wins over a visual example. The current `OperationalSettingsView.vue` and `UserSettingsView.vue` predate this design and are not evidence of a completed redesign.

Use these conceptual sections in this order: **Tổng quan**, **Cửa hàng**, **Người dùng**, **Vận hành**, **Giao diện**, **In & biên lai**, then **Tài khoản & bảo mật** only when it has useful current-user information or supported self-service action. Exact routes belong to a later Technical Breakdown, but each detail must be deep-linkable and refresh-safe. Do not add generic Advanced, System or Developer Settings without a concrete requirement.

## Overview and navigation

**Tổng quan** is status plus navigation, not a KPI dashboard or quick-control panel. Group readable rows for actual Store name, IANA timezone, `allowNegativeStock`, saved Appearance summary, supported user summary and receipt/printing entry where capability exists. A row opens the relevant detail. No operational configuration changes directly on Overview; no invented health state, counts, printer connection or placeholder capability. If a summary cannot be obtained reliably, omit it or show a clear unavailable state rather than inventing a value.

Desktop uses main app navigation, secondary Settings navigation and a narrow readable content column with section headings, setting rows, controls, dividers and contextual callouts; avoid a card per setting. Tablet may compact the secondary navigation. Mobile uses **Settings Overview → Detail**, a back affordance, one-column forms and stacked User rows, without the desktop sidebar. Keep warnings and explanations visible; optional sticky actions respect safe areas. Desktop, tablet and mobile preserve the same permissions and business consequences.

## Cửa hàng

Show only Store fields established by the approved domain/backend. Current `Store` has canonical `Name`, `TimeZoneId` and `AllowNegativeStock`; it does not establish address, tax code, phone, email or website. `Store.Name` is the canonical Store identity and can be reused for app and receipt branding. Do not invent a separate `ReceiptStoreName` or conflicting Store-name editor. The current API reads Store name and sets it during Store initialization; it does **not** expose a post-setup rename operation. A future editable name control therefore requires a reviewed mutation contract, and any multiple entry points must update the same canonical field. Receipt/logo Appearance options are governed by D-095 and their separate implementation review.

Timezone is a **Store-level** setting. Persist a canonical IANA ID such as `Asia/Ho_Chi_Minh`; a UTC offset is presentation only. Use a searchable, keyboard-accessible selector that displays the IANA ID and may show its current offset. The Store timezone determines local/business-date grouping, including daily reporting and End-of-day (D-054/D-101); browser and server timezones are not the authority. Before saving a changed timezone, explain in plain Vietnamese that daily grouping can change. Do not promise migration, historic snapshot or rebucketing rules beyond the approved backend semantics. Current Owner-only `PUT /api/store/timezone` is the mutation authority.

## Vận hành

The approved boolean `allowNegativeStock` has the user-facing label **Cho phép bán khi tồn kho không đủ**. Use a toggle with accessible name/state, but **do not auto-save**. The explicit form flow is `edit → dirty → Save → persisted`; OFF → ON needs no confirmation modal. When ON, show a visible caution/information callout that some Products may reach negative stock and cost reliability can be affected. This is not a validation error. The Settings UI only mutates configuration through the Owner-only backend operation; stock validation, inventory valuation and Sale behavior remain the approved domain/backend rules. It must not implement a parallel frontend business rule. Do not add a negative-stock threshold or other operational setting by inference.

## Người dùng and permissions

This is **Store-level administration**, distinct from current-user Account & Security. Roles are **Owner** and **Cashier**; no custom roles, permission matrix or arbitrary editor. Desktop uses a table; mobile uses stacked rows/cards. A real display name is the primary identity **when the authentication model has one**. The current account contract identifies Cashiers by email and does not provide a profile name, so use the actual email as the primary label now; do not manufacture a name. Role badges are neutral, with text rather than a color hierarchy.

Render only actions supported by the actual auth/authorization contract. The current Owner-only Cashier API supports list, create by email with one-time temporary credential, disable and credential reset. Preserve their existing security/consequence behavior and Store isolation; these supported actions are action-based and persist individually. Do not add edit-user, invite, delete, arbitrary role change, force logout, session revoke or MFA management without an approved capability. The backend decides authorization; frontend role names alone do not create new rights. A Store invariant requiring an Owner must not be turned into a speculative empty-list onboarding flow.

Differentiate **no read permission** (hide navigation or explain denial as the real auth semantics require), **read-only** (show data, no mutation, explain limitation), and **mutation allowed** (show applicable controls). Avoid flashing a denied state while authorization is loading. A disabled control alone is insufficient explanation. Current Owner-only Store mutations/User Management remain Owner-only; a shared Settings shell does not grant Cashier access.

## Giao diện, In & biên lai, Tài khoản & bảo mật

**Cài đặt → Giao diện** delegates directly to [D-095 Appearance](appearance-settings-v0.1.md): Store branding, bounded presets, Light/Dark/System, density, previews and its own reset/save behavior. Do not build a second theme/settings store or add another Settings-shell Save layer. Keep Store name canonical if an Appearance surface eventually exposes it.

Receipt presentation is **Store-level** and reuses canonical Store branding/Appearance. Printer connection is **device/terminal-level**; label it **Máy in trên thiết bị này**, never implying one Store-global printer. The existing Sale receipt/browser print flow is not a managed printer-connection architecture. D-102 defines no USB, LAN/IP, Bluetooth, Windows queue, browser-print configuration, local agent, ESC/POS, cutter, cash drawer or paper-profile rules. If no managed connection capability exists, show an explanatory empty state instead of a fake or disabled connection form. Printer architecture needs a separate decision.

**Tài khoản & bảo mật** concerns the current user, not Store User Management. Current session data includes email and role, and the app supports authenticated Change Password; these may provide a useful page or link. Render MFA, session management, trusted devices and further self-service only if actually supported; do not create disabled “coming soon” controls. If there is no useful information/action, this section need not appear in navigation.

## Persistence, feedback and safety

Per-page persistence wins over a global Save convention. **Store** and **Vận hành** use explicit forms where editable capability exists; **Người dùng** actions persist separately; **Giao diện** keeps its approved own Save semantics. No global Save footer. For explicit forms, Save is disabled while clean; Cancel restores persisted values; saving prevents duplicate submission without blocking the entire page; success is a non-blocking acknowledgement. A save error retains every unsaved value and explicitly tells the user their input remains available for retry. Put the Save area in a stable location.

When an editable form is dirty, internal navigation warns before discarding. Browser refresh/close may use native `beforeunload`. Background refresh must not overwrite unsaved editable values; no sophisticated merge is required in v0.1. Loading retains app/Settings shell with content skeletons rather than an unnecessary full-page spinner. Load failure explains the problem and offers Retry, never fallback invented data. Distinguish save error, permitted empty state and permission denial. Use contextual D-096 explanation for business effect, unsaved-on-error behavior and access limitations; explanation must not invent new semantics.

## Controls, accessibility and representative review

Use a toggle only for a true boolean such as `allowNegativeStock`, not timezone, role, Appearance preset, printer selection or other multi-option choices. Provide visible focus, associated labels and errors, accessible toggle state and icon-only action names, non-color-only status, adequate touch targets and keyboard operation for searchable selectors. `Esc` must not discard an entire form.

Future implementation/review should cover: desktop Overview and Store; Operations OFF and ON with consequence callout; desktop Users; Receipt/Printer; permission state; save error retaining input; mobile Overview, Operations and Users; and Appearance integration. Deep links and refresh must preserve the intended detail page without bypassing authorization.

## Boundary

Settings v0.1 is **design-complete within the current scope**. It organizes or exposes approved capabilities and does not make frontend code the source of new business semantics. Outside this approval: custom roles/permissions, tax or payment configuration, costing-method selector, negative-stock threshold, automatic End-of-day, multi-Store/advanced multi-warehouse settings, notification preferences, integrations, API keys, backup/audit configuration, Advanced/Developer Settings, printer protocol configuration and speculative MFA/session administration. Printer architecture, detailed authentication/user lifecycle beyond current capabilities and additional operational settings require separate decisions. This is documentation only; no Vue, backend, database or migration implementation is approved as complete.
