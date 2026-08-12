# System Tests — Activity 5

Executed through `AppointmentBooking.Console` — the external interface — not by calling
`AppointmentBookingService` directly. First evidence at the **system** boundary.

> **Action needed from you:** add the Console App project (files provided), run it for each
> scenario below, and fill in Actual Result / Pass-Fail / Evidence. Note: SYS-02's day offset
> is `1`, not `0` — today is now invalid in this codebase (see Activity 3), so every "valid
> date" scenario must use at least `1`.

### SYS-01 — Valid future booking with final available slot
Inputs: `P001` / `Diana William` / *(blank)* / `Dr Mark` / slots `1` / days `1`
Expected: Success message names Diana William (no preferred name) and Dr Mark; `Remaining slots: 0`

### SYS-02 — No availability
Inputs: `P002` / `Sam Lee` / *(blank)* / `Dr Mark` / slots `0` / days `1`
Expected: Failure message explains no available slots; `Remaining slots: 0`

### SYS-03 — Past/today appointment date
Inputs: `P003` / `Kim Tan` / *(blank)* / `Dr Mark` / slots `1` / days `0`
Expected: `Validation error:` — **today is invalid** under this codebase's `<=` rule (not
just genuinely-past dates), so `days = 0` is the sharper boundary test here rather than `-1`

### SYS-04 — Preferred name shown
Inputs: `P004` / `Diana William` / `Aroha` / `Dr Mark` / slots `1` / days `1`
Expected: Success message uses **Aroha**; `Remaining slots: 0`

### SYS-05 — Extra scenario: blank legal name caught gracefully
Inputs: `P005` / *(blank)* / *(blank)* / `Dr Mark` / slots `1` / days `1`
Expected: `Validation error: Legal name is required.` — console's own `try/catch` handles it

*(Fill in Preconditions/Steps/Actual/Pass-Fail/Evidence per the lab's required table format
for each row above when you run it.)*

## Reflection
The difference from the MSTest suite is the **entry point and boundary**, not the mechanism —
the same underlying logic (including the corrected date rule) runs through `Console.ReadLine`/
`WriteLine` here instead of being called directly.
