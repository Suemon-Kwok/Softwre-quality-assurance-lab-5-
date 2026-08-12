# Week 5 Testing Map — Activities 1 & 2

## Activity 1 — Baseline and diagnosis

### Step 1-2 — Build and run

Confirmed via `dotnet test` from the solution root:

```
Test summary: total: 24, failed: 0, succeeded: 24, skipped: 0, duration: 7.3s
Build succeeded in 11.5s
```

Test parallelisation is enabled at method level (28 workers) — worth remembering for
Activity 7, where shared test-data files need isolating for exactly this reason.

### Step 3 — Recorded numbers

| | Count |
|---|---|
| Tests discovered | 24 |
| Tests passed | 24 |
| Tests failed | 0 |
| Tests skipped | 0 |

### Step 4 — Production classes exercised

`Doctor`, `Patient`, `AppointmentRequest`, `AppointmentBookingService`, `Appointment`,
`BookingResult` — split across two test classes: `AppointmentBookingServiceTests` (18 tests)
and `AppointmentCancellationTests` (6 tests).

### Step 5 — At least four behaviours/risks with little or no test evidence

1. **`AppointmentBookingService.BookAppointment(null)`.** The method still has an explicit
   `if (request == null)` branch returning a failure result, but none of the 24 tests call
   it with a null request. Zero evidence for this path.

2. **The patient-ID validation branch is unreachable — and one test's name is misleading.**
   `BookAppointment` contains `if (string.IsNullOrWhiteSpace(request.Patient.Id)) return ...`,
   but `Patient`'s own constructor already throws on a blank ID, so no `Patient` object with
   an invalid ID can ever exist to be passed into a request. This branch is dead code that no
   test — and no real caller — can reach through the public API.
   `BookAppointment_WhenPatientIdInvalid_ReturnsFailure` is *named* as if it tests this, but
   its own comment admits it only re-checks that a normal valid booking succeeds. This is a
   good example of a test that looks like coverage but isn't.

3. **Concurrent booking and cancellation.** `Doctor` now holds two pieces of shared mutable
   state — `AvailableSlots` and the `_dailyAppointmentCounts` dictionary — both updated with
   plain, unsynchronised read-then-write operations in `ReserveSlot()` and `ReleaseSlot()`.
   There is no lock anywhere. If two bookings raced for a doctor's last slot, or a booking
   and a cancellation happened at the same instant, both fields could end up inconsistent
   with each other. No test gives any evidence about this.

4. **Whitespace-only (as opposed to empty-string) invalid input.** `Doctor_WhenIdIsEmpty_
   ThrowsException` and `Patient_WhenIdIsEmpty_ThrowsException` only pass `""`. The actual
   guard clauses use `string.IsNullOrWhiteSpace`, which also rejects `"   "` — that specific
   partition is untested for `Doctor`'s ID, `Doctor`'s name, and `Patient`'s legal name.

5. **`CancelAppointment`'s success message content.** Every successful/failed *booking*
   message has a `StringAssert.Contains` test checking its wording. The cancellation success
   message (`"...has been cancelled."`) has no equivalent content check — only that
   `IsCancelled` becomes `true` and the slot is released, not that the message itself says
   anything sensible.

### Reflection — what confidence is still missing?

A green run of all 24 tests only tells us that the specific input/output pairs someone
thought to write down are still true today. It says nothing about the null-request path,
the dead patient-ID branch (which looks covered by name but isn't), whitespace-only inputs,
or what happens when two operations touch a doctor's slot state at the same moment — a risk
that's arguably gotten *more* dangerous since the daily-capacity feature was added, because
now there are two related counters that both need to stay in sync, not just one. It also
says nothing about behaviour through a real interface (Activity 5) or under real
persistence (Activity 7 extension). Passing tests are evidence for the scenarios they
cover — not a general certificate that the system is correct or ready to ship.

---

## Activity 2 — Testing map of existing tests

| Existing test/evidence | Level | Type/focus | Technique/perspective | What it provides evidence for | Important gap |
|---|---|---|---|---|---|
| **Doctor: negative slots**<br>`Doctor_WhenAvailableSlotsIsNegative_ThrowsException` | Unit | Negative / validation test on the `Doctor` constructor | Boundary value analysis (boundary between -1 and 0) | The invariant "available slots cannot be negative" is enforced at construction time | Doesn't prove the invariant holds *after* construction — `ReserveSlot`/`ReleaseSlot` mutate `AvailableSlots` with no re-validation, and with no locking (see Activity 1, gap 3) |
| **Booking: no slots**<br>`BookAppointment_WhenDoctorHasNoAvailableSlots_ReturnsFailure` | Unit / small component (`AppointmentBookingService` + `Doctor` + `Patient` together) | Functional negative-path test | Equivalence partitioning (the "0 total slots" partition) | The service refuses to book when `Doctor.HasAvailableSlot()` is false | There are now **two distinct** reasons a booking can fail — no total slots left, or the daily cap reached (`HasCapacityOnDate`) — and this test only proves one of them. Nothing confirms the two failure paths stay distinguishable by message if the code changes |
| **Patient: preferred display name**<br>`Patient_WhenPreferredNameExists_DisplayNameUsesPreferredName` | Unit | Functional positive-path test | Equivalence partitioning ("preferred name present" partition) | `DisplayName` correctly prefers `PreferredName` when one is supplied | Doesn't test the whitespace-only partition (`"   "`), which `IsNullOrWhiteSpace` should also treat as "absent" — untested for `DisplayName` specifically |
| **Request: past appointment date**<br>`AppointmentRequest_WhenRequestedDateIsInPast_ThrowsException` | Unit | Negative validation test | Boundary value analysis (one day before "today") | Yesterday is rejected | On its own this test only proves *yesterday* throws — it does **not** locate the real boundary, because the rule is now `<= DateTime.Today` (today also throws — the PDF's Rule set B, "today is valid," is out of date for this codebase). The actual boundary is covered *elsewhere*, by two other tests: `AppointmentRequest_WhenRequestedDateIsToday_ThrowsException` and `..._Tomorrow_Succeeds`. As a single row this test is incomplete; as a suite the boundary is well covered |
| **Booking: helpful success message**<br>`BookAppointment_WhenSuccessful_ReturnsHelpfulMessage` | Unit / small component | Functional test with output-content assertion | Specification-based, substring/content checking | The success message contains an expected phrase and the patient's preferred name | Uses `StringAssert.Contains` (weak assertion) and doesn't check the doctor's name or the appointment date are present in *this* test — those are checked in a separate test (`..._MessageIncludesDoctorName`), and the date's presence in the message has no dedicated check at all |
| **Other test of choice:**<br>`CancelAppointment_AlreadyCancelledAppointment_ThrowsException` | Unit / small component (`AppointmentBookingService` + `Appointment` + `Doctor` together) | Negative validation / business-rule guard | State-transition testing (already-cancelled state) | An appointment cannot be cancelled twice, which prevents the same doctor slot from being released more than once for one appointment | Only checks that the second `CancelAppointment` call throws — it doesn't confirm the doctor's `AvailableSlots`/daily count stayed unchanged after the *rejected* second attempt, so a bug that both throws *and* corrupts state would still pass this test |

### Level/focus not currently well-evidenced by this project

| Level/focus | Does the project currently provide convincing evidence? |
|---|---|
| **Integration** | **No.** Every test constructs `Doctor`, `Patient`, `AppointmentRequest`, `Appointment` and `AppointmentBookingService` directly in memory in the same process. No test crosses a real integration boundary (file, database, external service) — that's what the Activity 7 extension adds. |
| **System** | **No.** Nothing drives the application through an external interface such as a console or UI. Activity 5 is what introduces the first system-level evidence. |
| **Acceptance** | **No.** No test is traceable to a business/acceptance criterion signed off by a stakeholder, and none was executed by anyone other than a developer. |
| **Non-functional** | **No.** No performance or load testing, and — more pressingly given this codebase — no concurrency/thread-safety evidence, despite `Doctor` now managing *two* pieces of shared mutable state (`AvailableSlots` and `_dailyAppointmentCounts`) with unsynchronised updates. |

**Note on the unit vs. small-component boundary:** several tests above are classified as
"unit / small component" because they exercise `Doctor`, `Patient`, `Appointment` and
`AppointmentBookingService` together, in memory, with no test doubles. This is a defensible
unit-test boundary because there's no external dependency (no I/O) — the "collaborators" are
plain in-memory objects, not faked external systems. Under a stricter definition (one class
under test, everything else mocked) the same tests would be small component tests instead;
what matters is stating and justifying the boundary chosen, which this table now does.
