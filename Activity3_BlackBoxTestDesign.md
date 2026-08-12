# Black-Box Test Design — Activity 3

Designed from the actual business rules your code enforces — read from the constructors and
guard clauses in `Doctor.cs`, `AppointmentRequest.cs` and `AppointmentBookingService.cs`,
**not** from the lab sheet's Rule set B, which is out of date for this project (see the note
below). This still counts as "black-box": the partitions come from the stated rules/behaviour,
not from tracing through the implementation logic.

> **Correction to the lab sheet:** Rule set B says "Today is valid in the current starter."
> That is **wrong** for your code. `AppointmentRequest`'s constructor now uses
> `requestedDate.Date <= DateTime.Today`, so **today throws**, same as any past date. This is
> almost certainly the error the TA flagged. Every boundary below uses your actual `<=` rule.

## Equivalence partitions — doctor total available slots

| Partition | Description | Representative value |
|---|---|---|
| Invalid | Negative slot count | -1 |
| Valid — zero | Zero slots (cannot accept a booking) | 0 |
| Valid — available | One or more slots (can accept a booking) | 1 (and 2, as a second representative) |

## Boundary values — total slot counts

| Boundary | Value | Expected |
|---|---|---|
| Just invalid | -1 | Constructor throws `ArgumentException` |
| Just valid, but cannot book | 0 | Constructs successfully; `HasAvailableSlot()` is `false` |
| Just valid and can book | 1 | Constructs successfully; `HasAvailableSlot()` is `true` |

## Equivalence partitions — doctor daily capacity (`MaxDailyAppointments`)

This is a second, independent constraint that didn't exist in the original starter —
`Doctor` now caps how many appointments can be booked on a single date, separately from the
total `AvailableSlots`.

| Partition | Description | Representative value |
|---|---|---|
| Invalid (constructor) | `maxDailyAppointments <= 0` | 0 |
| Valid (constructor) | `maxDailyAppointments >= 1` | 1 |
| Below the daily cap (booking) | Existing bookings on that date < cap | 0 existing bookings, cap 1 |
| At the daily cap (booking) | Existing bookings on that date == cap | 1 existing booking, cap 1 |

## Boundary values — daily capacity

| Boundary | Value | Expected |
|---|---|---|
| Just invalid (constructor) | `maxDailyAppointments = 0` | Constructor throws `ArgumentException` |
| Just valid (constructor) | `maxDailyAppointments = 1` | Constructs successfully |
| One below the cap (booking) | count = 0, cap = 1 | `HasCapacityOnDate` is `true` — booking succeeds |
| Exactly at the cap (booking) | count = 1, cap = 1 | `HasCapacityOnDate` is `false` — booking fails |

## Equivalence partitions — appointment dates

| Partition | Description | Representative value |
|---|---|---|
| Invalid | Any date at or before today | Yesterday, and today itself (both invalid under `<=`) |
| Valid | Any date after today | Tomorrow (and further future dates as a second representative) |

## Boundary values — the real past/future transition

The rule change means the boundary is **between today and tomorrow**, not between yesterday
and today as the (incorrect) lab sheet implies.

| Boundary | Value | Expected |
|---|---|---|
| Comfortably invalid (sanity check) | Yesterday (today − 1) | Constructor throws `ArgumentException` |
| Just invalid | **Today** (today + 0) | Constructor throws `ArgumentException` — this is the actual boundary, and it's the opposite of what the lab sheet describes |
| Just valid | Tomorrow (today + 1) | Constructs successfully — no exception |

## Decision table — `BookAppointment`

Four conditions now drive the outcome (up from the two in the original Rule set C), reflecting
the patient-ID check, the total-slots check, and the new daily-capacity check.

| Rule | C1: Request null? | C2: Patient ID invalid? | C3: Doctor has a total available slot? | C4: Doctor has capacity on this date? | `Success` | Message | `Appointment` created? |
|---|---|---|---|---|---|---|---|
| R1 | Yes | — | — | — | `false` | "request is missing" | No |
| R2 | No | Yes | — | — | `false` | "patient ID is invalid" | No |
| R3 | No | No | No (0 total slots) | — | `false` | "no available slots" | No |
| R4 | No | No | Yes | No (cap reached) | `false` | "maximum number of appointments on [date]" | No |
| R5 | No | No | Yes | Yes | `true` | "Appointment booked successfully..." | Yes |

**Note on R2:** as flagged in Activity 1/2, this branch is **structurally unreachable**
through the public API — `Patient`'s own constructor already blocks an invalid ID, so no
`Patient` object can exist to trigger it. It's included in the decision table because the
*code* implements this rule, but no black-box test can actually drive C2 = "Yes" without
bypassing `Patient`'s constructor (e.g. reflection), which is out of scope here.

## Which designed cases already exist vs. are missing from the current 24-test suite

| Designed case | In current suite? |
|---|---|
| R3 — 0 total slots → failure | ✅ `BookAppointment_WhenDoctorHasNoAvailableSlots_ReturnsFailure` |
| R5 — available slots (2) → success | ✅ `BookAppointment_WhenDoctorHasAvailableSlots_ReturnsSuccess` |
| R5 — success decrements total slot count | ✅ `BookAppointment_WhenSuccessful_DecreasesAvailableSlots` |
| R3 — failure leaves total slot count unchanged | ✅ `BookAppointment_WhenFailed_DoesNotDecreaseAvailableSlots` / `..._SlotCountRemainsUnchanged` |
| R4 — daily cap reached → failure | ✅ `BookAppointment_WhenDoctorAtMaxDailyAppointments_ReturnsFailure` |
| Total-slot boundary: -1 → constructor throws | ✅ `Doctor_WhenAvailableSlotsIsNegative_ThrowsException` |
| Date boundary: yesterday → throws | ✅ `AppointmentRequest_WhenRequestedDateIsInPast_ThrowsException` |
| Date boundary: **today → throws** (the corrected boundary) | ✅ `AppointmentRequest_WhenRequestedDateIsToday_ThrowsException` |
| Date boundary: tomorrow → succeeds | ✅ `AppointmentRequest_WhenRequestedDateIsTomorrow_Succeeds` |
| R1 — null request → failure, no throw | ❌ **Missing** — added below |
| Daily-cap constructor boundary: `maxDailyAppointments = 0` → throws | ❌ **Missing** — added below |
| Daily-cap constructor boundary: `maxDailyAppointments = 1` → succeeds | ❌ **Missing** (only ever constructed with the default 5, or explicitly with 1 combined with a booking scenario — never checked as a standalone constructor boundary) — added below |
| Total-slot boundary: exactly 1 slot → succeeds and leaves 0 | ❌ **Missing** (existing tests only use 0 or 2 slots, skipping the 1-slot boundary) — added below |
| `HasAvailableSlot()` boundary as its own isolated, parameterised check (0/1/2) | ❌ **Missing** as a direct unit test — only exercised indirectly through `BookAppointment` — added below |

## Data-driven tests added

See `AppointmentBooking.Tests/Activity3_BlackBoxTests.cs`:

1. `HasAvailableSlot_BoundaryCases` — the lab sheet's example pattern, `DataRow(0, false)`,
   `DataRow(1, true)`, `DataRow(2, true)`.
2. `AppointmentRequest_DateBoundaryCases` — the additional data-driven test, covering the
   corrected yesterday/today/tomorrow boundary in one parameterised test.
3. `Doctor_MaxDailyAppointmentsBoundaryCases` — a second data-driven test for the previously
   untested constructor boundary on `maxDailyAppointments`.

Plus two scalar tests for the missing scenarios that don't need parameterising:
`BookAppointment_WhenRequestIsNull_ReturnsFailureWithoutThrowing` (R1) and
`BookAppointment_WhenExactlyOneSlotAvailable_SucceedsAndLeavesZero` (the 1-slot boundary).
