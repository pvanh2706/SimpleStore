# Pilot 80 mm Browser Print Certification v0.1

## Gate and current result

Primary target: **80 mm thermal receipt, browser `window.print()`**. The printed Sale is a read-only view of a committed transaction. A canceled, unavailable, or failed print must leave the Sale Completed, reloadable, and reprintable. An additional print agent/service needs a later Product Owner decision if browser printing fails certification.

**PHYSICAL 80 MM PRINTER CERTIFICATION PENDING — HARDWARE NOT AVAILABLE.** PR-BLOCKER-06 remains OPEN. DOM/unit tests and browser PDF/preview inspection, if performed, do not certify paper output. A secondary configuration is needed only if the pilot actually uses it; 58 mm and A4 are not the baseline.

## Safe discovery on this Windows workstation (2026-09-27)

Commands: `Get-Printer`, `Get-CimInstance Win32_Printer`, `Get-PrinterDriver -Name 'HP LaserJet P2035'`, `Get-PrintConfiguration -PrinterName 'HP LaserJet P2035'`, and OS/browser file-version inspection. No print job was sent.

| Installed printer | Interface/port | Driver and visible capability | 80 mm certification use |
|---|---|---|---|
| HP LaserJet P2035 (default; manufacturer HP) | USB001 | HP LaserJet P2035, driver v3.0.1.52444; current paper Letter, monochrome, one-sided | No: this is a sheet printer, not an 80 mm thermal device |
| Microsoft Print to PDF | PORTPROMPT: | Microsoft Print To PDF | No: virtual |
| Microsoft XPS Document Writer | PORTPROMPT: | Microsoft XPS Document Writer v4 | No: virtual |
| Send To OneNote 2010 | nul: | Send To Microsoft OneNote 2010 Driver | No: virtual |
| Fax | SHRFAX: | Microsoft Shared Fax Driver | No: fax |

Host: Windows 11 Pro, build 26200. Microsoft Edge executable version 154.0.4258.37 is installed; it has **not** been exercised with 80 mm hardware. The installed browser version is discovery only, not a certified pilot browser/version.

## Candidate layout and pilot settings to verify

`SaleReceipt.vue` presents each Product name on its own row with SKU/unit and a quantity × unit-price / line-amount row. Print CSS removes desktop widths, navigation and buttons; names use normal word wrapping, while an unbroken SKU may wrap. `@page` requests **80 mm × 200 mm** with **2 mm** page margins and a 76 mm content box. The 200 mm page length is a candidate, not a certified continuous-roll/cut setting. A real printer test must check long receipts, pagination, cut position, clipping, and how its driver handles this requested size.

At certification, select the actual 80 mm roll/media in the driver, browser scale **100%**, browser headers/footers **off**, and margins that preserve the CSS 2 mm inset. Record any necessary different setting and repeat the full matrix; do not silently use “fit to page”, desktop/A4 media, or 58 mm. Verify Vietnamese glyphs and physical edge clearance.

## Actual certification record — complete on pilot hardware

| Field | Actual value/evidence |
|---|---|
| Store/environment, operator, reviewer, UTC timestamp | PENDING — HARDWARE NOT AVAILABLE |
| Candidate exact commit SHA, application version, artifact SHA-256 | PENDING — exact candidate required |
| Printer manufacturer/model/serial-safe identifier | PENDING — HARDWARE NOT AVAILABLE |
| USB/network/actual interface and port | PENDING — HARDWARE NOT AVAILABLE |
| Driver name/version and Windows version | PENDING — HARDWARE NOT AVAILABLE |
| Browser name/version | PENDING — HARDWARE NOT AVAILABLE |
| Actual paper/driver width and roll length/cut behavior | PENDING — HARDWARE NOT AVAILABLE |
| Browser scale, margins, headers/footers and print-dialog settings | PENDING — HARDWARE NOT AVAILABLE |
| Sample IDs/photo or redacted scan references, without customer/payment sensitive data | PENDING — HARDWARE NOT AVAILABLE |
| Final physical result and Product Owner review | FAIL / NOT CERTIFIED — PENDING |

## Physical test matrix

For each row record sample Sale ID, settings, PASS/FAIL, observed clipping/wrapping/alignment, redacted evidence reference and operator/time. Use a disposable or approved pilot test Sale; printing itself must not create or edit transactions.

| Case | Required observation | Result |
|---|---|---|
| Short Product, multiple lines | All lines and receipt boundaries visible; no desktop shrink-to-left | PENDING — HARDWARE NOT AVAILABLE |
| Long Vietnamese Product, diacritics | Natural word wrapping; no split of ordinary words such as “bằm” or “tỏi”; legible accents | PENDING — HARDWARE NOT AVAILABLE |
| Long SKU and unit | Complete, distinguishable from Product name; no overlap | PENDING — HARDWARE NOT AVAILABLE |
| Integer and decimal quantity | Quantity × unit price and line amount readable | PENDING — HARDWARE NOT AVAILABLE |
| Unit price, each line total, receipt total | Values aligned and complete | PENDING — HARDWARE NOT AVAILABLE |
| Cash and Transfer | Method and amount readable | PENDING — HARDWARE NOT AVAILABLE |
| Customer and original debt | Facts readable and distinguishable from current corrections | PENDING — HARDWARE NOT AVAILABLE |
| Initial print after Completed Sale | Committed Sale remains visible/reloadable | PENDING — HARDWARE NOT AVAILABLE |
| Reprint from stored Sale detail | Same original Sale facts; no new CompleteSale | PENDING — HARDWARE NOT AVAILABLE |
| Voided Sale | “ĐÃ HỦY” is unmistakable; original lines/payments remain for comparison | PENDING — HARDWARE NOT AVAILABLE |
| Sale with Return | Original facts plus current correction note; detail retains Return history | PENDING — HARDWARE NOT AVAILABLE |
| Cancel browser dialog | Sale stays Completed and retryable | PENDING — HARDWARE NOT AVAILABLE |
| Offline/unavailable printer | Failure does not mutate Sale; retry after recovery | PENDING — HARDWARE NOT AVAILABLE |
| Driver/printer error where practical | Record exact symptom; Sale stays Completed/reprintable | PENDING — HARDWARE NOT AVAILABLE |
| Long receipt/page boundary/cut | No lost lines, partial words or unwanted blank/cut pages | PENDING — HARDWARE NOT AVAILABLE |

Any actual failure must be recorded with browser/driver/media settings and redacted sample. Fix browser CSS and rerun certification, or seek a separate Product Owner decision before pursuing a print agent. Do not close PR-BLOCKER-06 until sufficient physical evidence and Product Owner approval.
