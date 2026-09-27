# Pilot Validation Execution Plan v0.1

This is a fieldwork plan, not pilot findings. Run it with a real, consented pilot Store after the required readiness gates. Keep operational task outcomes separate from C14 hypothesis evidence. Use the [observation](templates/pilot-observation-template.md) and [incident](templates/pilot-incident-template.md) records. Minimize personal data; use safe Store/User IDs and redacted evidence. No pilot interviews or willingness-to-pay results have been collected here.

## Core MVP operational validation

For each task, note actor role, start/end, whether it completed without help, any help or recovery, observed confusion, relevant SHA/version/traceId and a concrete evidence reference. Observe actual work; a click or automated test alone is not task success.

| Workflow/task to observe | Success/recovery prompt |
|---|---|
| Owner onboarding and Cashier login | Can Owner initialize Store and Cashier change temporary password? Can support recover a locked-out user safely? |
| Product CSV import | Can data owner prepare, validate, preview, correct and confirm the intended catalog? |
| Purchase | Can operator record receipt and explain resulting stock/cost? |
| Sale and 80 mm print/reprint | Can Cashier complete a Sale, print/retry, and retrieve the stored Sale independently of printer outcome? |
| Inventory, Stock Adjustment and Stocktake | Can Owner explain before/after quantity, cost and reconciliation, including negative-stock setting? |
| Customer debt and supplier debt | Can operator understand original vs current outstanding and record the correct payment? |
| Return and Void | Can Owner choose the correct correction, understand retained original facts and recover from a rejected action? |
| End-of-day and Today screen | Can Owner finish day-end review and use Today evidence without confusing it with completed actions? |

After each session, summarize actual task completions/failures and repeatable confusion, with severity and recovery; link incidents. Product Owner reviews Core MVP operational findings separately from C14. Do not mark a workflow validated from scripted E2E alone.

## C14 hypothesis validation (D-084)

Existing `TodayOpened`, `SignalShown`, `WhyOpened`, and `PurchaseDraftStarted` events may support counts/exposure analysis. Record the queried Store/time window, SHA/version, event definitions, deduplication and missing-data caveats. **Do not infer** click = value, `WhyOpened` = trust, `PurchaseDraftStarted` = a successful recommendation, or event count = willingness-to-pay.

Ask the Owner in context, then compare answers with observed behavior and actual business decisions:

1. Did you notice this specific signal? When?
2. Was it new information or already known? What was your previous plan?
3. Can you explain why the signal appeared, in your own words?
4. Which source facts did you inspect? Did you trust them, and why or why not?
5. Did the signal influence a real decision? Which decision and what other factors mattered?
6. Did you do something different because of it? Was the action completed and useful?
7. Would you continue using this view in normal operations? What would you stop using?
8. What concrete value, if any, would justify payment? Ask willingness-to-pay separately from usage and avoid leading price cues.

Capture contradictory cases, non-use and confounding factors. A reviewer writes a separate C14 finding with evidence and limits only after real observation/interviews. **C14 value / willingness-to-pay — NOT VALIDATED** until that review; Pilot remains NOT STARTED at creation of this plan.
