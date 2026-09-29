# Technical Breakdown UI-A v0.1 — Design Foundation + Application Shell

## Status and objective

`APPROVED FOR IMPLEMENTATION — D-103`; UI-A implementation **APPROVED / COMPLETED — D-104**.

D-103 approved this **frontend-only** breakdown at reviewed baseline `88a29b4f8a86b8cc5b988b0af9edff90bb314954`. Product Owner approved the implementation on 2026-09-29 in [D-104](../../DECISIONS.md#d-104--ui-a-design-foundation--application-shell-implementation-approval), at head `eb487be1c4290dad3e1ddd6fa039d2470dcaf495`. CI #74 is `SUCCESS`: frontend production build/test suite and backend restore/build/tests pass. Local targeted `tests/e2e/specs/ui-a-shell.spec.ts` finished **5 passed / 0 failed**, with the full spec repeated 10 times **50/50 passed**. The initial transient tablet failure did not reproduce; the individual rerun passed, no implementation bug was reproduced and it is not a known UI-A blocker. Verification ended with a clean working tree and no additional code change. No backend/domain/database/migration changes were introduced.

UI-B is **NOT STARTED**; the next step is **Technical Breakdown UI-B — Sales redesign**, requiring separate Product Owner approval before implementation. UI-B–UI-F implementation is not approved by D-104 and the program is not completed. Follow the [staged UI Redesign Program](../design/ui-redesign-program-v0.1.md); D-095–D-103 semantics/boundaries and existing domain/auth contracts remain unchanged.

UI-A establishes the reusable foundation for later stages: a D-095 application shell, semantic tokens, navigation and focused primitives. Existing business workflows remain usable; most screen internals may remain legacy. No new business capability or financial/domain meaning is introduced.

Allowed work: Vue shell, global/shared styling, semantic tokens, necessary reusable primitives, navigation presentation configuration, minimal router **presentation** metadata/refactor, frontend tests and narrowly scoped E2E/smoke checks. Do not change backend API, database, migrations, domain, authorization semantics or business routes merely for the shell. If a real backend dependency appears, stop that portion for separate Product Owner review.

## Inspected frontend baseline and responsibility boundary

Current `src/frontend/simplestore-web/src/App.vue` has a horizontal header/navigation and a shared `max-w-6xl` main workspace. `styles.css` defines legacy `.btn-primary`, `.btn-secondary`, `.card`, `.field`, `.input`, `.error`, `.nav-link` classes and 80mm receipt print rules. Router guards enforce login, Store setup, forced password change and Owner-only routes; the backend remains the security authority. Existing Sales, Product, Purchase, Today, Debt, EOD and Settings views predate the approved visual direction. Preserve their behavior while replacing the application shell. Likely areas are `App.vue`, `styles.css`, `router/index.ts`, current App/router tests and focused new `components/app/`, `components/ui/`, `navigation/`, `styles/` files; these are responsibility suggestions, not required filenames.

Do not create an npm design-system package, Storybook requirement, enterprise UI framework or unnecessary abstraction layer. Native HTML remains preferable when a generic component gives no actual reuse. No broad rewrite of business views and no generic `AppTable` requirement.

## Semantic foundation and shared controls

Start from **Emerald + Light + Standard**. Establish stable semantic roles with refinable variable names and values:

| Group | Minimum conceptual tokens |
| --- | --- |
| Brand | `brand-primary`, `brand-primary-hover` |
| Surface | `surface-page`, `surface`, `surface-muted`, `surface-elevated` |
| Border | `border`, `border-strong` |
| Text | `text`, `text-muted`, `text-subtle`, `text-on-brand` |
| State | `positive`, `warning`, `danger`, `info` |
| Interaction | `focus-ring` |
| Shape | `radius-sm`, `radius-md`, `radius-lg` |
| Density-ready spacing | `density-control-y`, `density-gap`, `density-section-gap` |

UI-A does not persist Appearance, offer Dark/System, density selection or alternate presets. Tokens and shell structure must be **appearance-ready** for a separately reviewed UI-F without a whole-app rewrite. Legacy classes may temporarily remain and map to semantic tokens where suitable; the compatibility layer must not become another design system. Migrate views gradually in UI-B–UI-E without changing workflow semantics.

Create shared controls only as immediate shell/use-case reuse needs them: Button, Field/input presentation, Alert/Callout, Badge, Skeleton, Empty state, Error state, Page/Section header, and dialog/icon button base **when needed**. Button roles are Primary for the context's main action, Secondary for supporting non-destructive action, Danger only for genuinely destructive action, Text/link for low-emphasis navigation or explanation. Do not pick colors by screen preference. Form presentation keeps visible labels, supporting text, inline validation, disabled/read-only/required semantics, visible focus and accessible error association; essential guidance does not live only in a placeholder. UI-A need not migrate every legacy form. Feedback distinguishes Info, Success, Warning and Error with text/icon meaning; warning is not an error and state is not color-only.

## Application shell and navigation

Desktop moves primary navigation to a persistent left sidebar with product/Store identity, authorized routes and account/settings area; workspace contains `RouterView`. Do not impose one universal max width: POS, Settings, lists, forms and reports need different usable widths. No resizable sidebar or persisted collapsed preference is required.

Centralize **presentation** metadata such as label, existing route, grouping, role visibility, active matching and optional icon outside a large hard-coded `App.vue` block. The model must not become a new permission framework. Render only existing supported routes and capabilities; router/backend authorization remains authoritative. Owner may see currently supported Hôm nay, Bán hàng, Đơn bán, Sản phẩm, Nhập hàng, Nhà cung cấp, Công nợ khách/NCC, Cuối ngày and current Settings entries. Cashier sees only permitted entries; Owner-only entries are absent, not disabled. CSV Import need not remain top-level if safely reachable from the Product workflow. Grouping and wording may be refined without changing route meaning.

Active matching must intentionally map nested `/products`, `/products/new`, `/products/:id`, `/products/:id/edit` to Sản phẩm; `/purchases/*` to Nhập hàng; and Sale creation/history/detail to their intended navigation context. Do not rely blindly on default `router-link-active`. Current Store data may show canonical Store name; when context is unresolved, SimpleStore identity remains usable. UI-A adds no logo upload, Store rename or branding persistence. Account area may show actual email, role, logout and supported Change Password entry, with no speculative security controls.

Tablet uses compact/adaptive navigation or a drawer at usable breakpoints; no persisted collapse state. Mobile uses a compact header with navigation drawer/sheet, not a desktop sidebar column; bottom navigation is optional, not required. When opened, the drawer is keyboard operable, Escape closes it, the current route is visible, route selection closes it, and background interaction/scroll is handled appropriately. Authorization filtering must be correct, and active state must not rely on color alone.

## Session, authorization and compatibility

Preserve four states: **anonymous → login**; **authenticated without Store → setup gate**, without normal Store navigation; **`mustChangePassword` → hard Change Password gate**, possibly with reduced shell and no sidebar bypass; **normal authenticated Store session → role-appropriate shell**. Keep existing safe redirect, setup redirect, Owner-only route rejection and logout/session behavior. During session resolution use an intentional lightweight loading state; do not flash stale/anonymous shell, wrong-role or unauthorized navigation. Navigation visibility is UX, never the security boundary.

Current business views may retain legacy internals after UI-A, but must stay readable, responsive enough, unoccluded by sidebar and fully actionable. Do not opportunistically redesign Sale, Product, Purchase, Today, Debt, EOD or Settings internals. Legacy `btn-primary`, `btn-secondary`, `card`, `field`, `input`, `error`, `nav-link` can coexist temporarily with token-backed styling. Preserve current workflow, forms, state and transaction behavior.

**Receipt printing is a regression blocker.** Shell/sidebar/account/navigation must not appear on receipt. Preserve existing width, 80mm page behavior, print visibility and transaction behavior. Any necessary print CSS change must be narrowly scoped and regression-tested/reviewed.

## Accessibility, icons and motion

UI-A baseline includes semantic navigation landmarks, visible focus, accessible current-navigation state, meaningful button names, adequate contrast/touch targets, correct headings and keyboard-operable mobile navigation. Do not rely on color alone. Formal WCAG certification is not required for UI-A. If icons are useful, use one lightweight Vue-compatible source, keep text labels and avoid emoji as production primary-nav icons; text-only navigation is acceptable. No animation system is required. Use only subtle helpful transitions and respect reduced-motion when motion is meaningful.

## Implementation sequence and gates

| Step | Work | Check before advancing |
| --- | --- | --- |
| UI-A.1 | Add semantic tokens/global baseline; retain legacy compatibility. | Frontend build and existing tests. |
| UI-A.2 | Build centralized navigation and shell components. | Owner/Cashier visibility and nested active-state tests. |
| UI-A.3 | Integrate into `App.vue` with current router/auth. | Anonymous login/safe redirect, setup, forced password, Owner/Cashier and logout regressions. |
| UI-A.4 | Add tablet/mobile navigation. | Drawer open/close, Escape, keyboard, selection, background handling and role visibility. |
| UI-A.5 | Add only shared primitives needed immediately. | Semantic usage, labels/focus/error association and reuse. |
| UI-A.6 | Harden compatibility, responsive behavior, print and accessibility. | Representative desktop/tablet/mobile checks; 80mm receipt print review. |
| UI-A.7 | Run full relevant validation. | All relevant frontend tests, production build and repository CI before Product Owner implementation review. |

Component/unit coverage must include Owner/Cashier navigation, unauthorized items absent, nested-route active state, mobile drawer/selection, logout and account identity. Router/auth regression includes anonymous redirect, safe redirect, Owner/Cashier, Store setup and redirect, forced password, Owner-only rejection and session/logout flow. Check desktop, tablet and mobile behavior without requiring new pixel-perfect infrastructure. Verify shell/nav is absent from receipt printing. If current Playwright setup supports it cleanly, a narrow smoke may cover Owner login → shell → Today → Product → logout and Cashier login → only allowed navigation → allowed screen; do not add backend semantics merely to ease E2E.

## Explicit UI-A exclusion and Product Owner gate

UI-A must not redesign business views, implement Settings Overview or Appearance, persist theme/density, add Dark/System or presets, upload logo, rename Store, add global Inventory, Held Sale, discounts, Category/Unit master data or printer configuration. It must not change domain/business rules, add backend APIs/migrations, broaden authorization or create unsupported routes.

Product Owner may approve **UI-A implementation** only after semantic foundation and desktop left shell exist; tablet/mobile work is usable; Owner/Cashier navigation, login/setup/forced-password and current screens have no regression; receipt printing is unaffected; accessibility baseline is met; no new business/backend/domain semantics appear; frontend tests/build and CI pass. These gates are accepted in **D-104**; UI-A is **APPROVED / COMPLETED**. The program is not complete. UI-B Sales remains **NOT STARTED** pending its own technical breakdown and separate Product Owner approval before implementation.
