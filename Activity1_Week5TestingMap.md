# Week 5 Testing Map — Activity 1

*(Activity 2's classification table will be appended to this same file once we do it —
they both live in `docs/Week5TestingMap.md` per the lab sheet.)*

## Step 1-2 — Build and run

Confirmed via `dotnet test` from the solution root:

```
Test summary: total: 24, failed: 0, succeeded: 24, skipped: 0, duration: 7.3s
Build succeeded in 11.5s
```

Test parallelisation is enabled at method level (28 workers) — worth remembering for
Activity 7, where shared test-data files need isolating for exactly this reason.

## Step 3 — Recorded numbers

| | Count |
|---|---|
| Tests discovered | 24 |
| Tests passed | 24 |
| Tests failed | 0 |
| Tests skipped | 0 |

## Step 4 — Production classes exercised

`Doctor`, `Patient`, `AppointmentRequest`, `AppointmentBookingService`, `Appointment`,
`BookingResult` — split across two test classes: `AppointmentBookingServiceTests` (18 tests)
and `AppointmentCancellationTests` (6 tests).

## Step 5 — At least four behaviours/risks with little or no test evidence

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

## Reflection — what confidence is still missing?

A green run of all 24 tests only tells us that the specific input/output pairs someone
thought to write down are still true today. It says nothing about the null-request path,
the dead patient-ID branch (which looks covered by name but isn't), whitespace-only inputs,
or what happens when two operations touch a doctor's slot state at the same moment — a risk
that's arguably gotten *more* dangerous since the daily-capacity feature was added, because
now there are two related counters that both need to stay in sync, not just one. It also
says nothing about behaviour through a real interface (Activity 5) or under real
persistence (Activity 7 extension). Passing tests are evidence for the scenarios they
cover — not a general certificate that the system is correct or ready to ship.
