# Coverage Review — Activity 4

> **Action needed from you:** run Visual Studio's built-in code coverage (Test → Analyze
> Code Coverage for All Tests) on the current suite (24 baseline + 5 from Activity 3 = 29
> tests), note the numbers, add `Activity4_CoverageGapTests.cs`, rerun coverage, and fill in
> the actual before/after figures below. The branch analysis below is from manually reading
> every guard clause in the six production classes and cross-checking it against all 29
> existing tests — it tells you *where* to expect gaps; the tool run is what actually proves
> it, and might turn up things this manual read missed.

## Which branches or paths were initially missing?

Reading `Doctor.cs`, `Patient.cs`, `AppointmentRequest.cs`, `Appointment.cs` and
`AppointmentBookingService.cs` against all 29 tests currently in the suite:

1. **`Doctor` constructor — whitespace-only `fullName`.** The `IsNullOrWhiteSpace(fullName)`
   guard has never been triggered; only the ID guard is tested.
2. **`Patient` constructor — whitespace-only `legalName`.** Same gap, on the `Patient` side.
3. **`AppointmentRequest` constructor — both null-guard branches** (`patient ?? throw ...`
   and `doctor ?? throw ...`). Every test constructs a request with real objects.
4. **`Doctor.ReserveSlot()`'s own two guard clauses.** `AppointmentBookingService` always
   checks `HasAvailableSlot()` and `HasCapacityOnDate()` *before* calling `ReserveSlot()`, so
   the `if (!HasAvailableSlot()) throw ...` and `if (!HasCapacityOnDate(date)) throw ...`
   lines inside `ReserveSlot` itself are **unreachable through the service** — the same
   "duplicated guard" pattern already flagged for the dead patient-ID check in
   `AppointmentBookingService` (Activities 1–2). They can only be exercised by calling
   `doctor.ReserveSlot(...)` directly.
5. **`Doctor.ReleaseSlot()`'s `Math.Max(0, currentCount - 1)` floor.** Every cancellation
   test in the suite releases a slot for a date that was booked first (so `currentCount` is
   always ≥ 1 going in). The defensive "don't go negative" branch — which only matters when
   `currentCount` is already 0 — has never actually executed.
6. **`Appointment`'s own constructor guards** (blank `id`, null `doctor`, null `patient`).
   In normal use `Appointment` is only ever constructed internally by
   `AppointmentBookingService` with already-valid data, so nothing has ever driven these
   guards directly, even though `Appointment`'s constructor is public and could be called
   with bad data from anywhere else in the codebase later.

## Which tests did you add?

All in `Activity4_CoverageGapTests.cs`:

- `Doctor_WhenFullNameIsWhitespace_ThrowsException` (covers #1)
- `Patient_WhenLegalNameIsWhitespace_ThrowsException` (covers #2)
- `Patient_WhenPreferredNameIsWhitespace_DisplayNameUsesLegalName` (bonus — a third
  `Patient` branch, the `DisplayName` whitespace fallback, also flagged back in Activity 2)
- `AppointmentRequest_WhenPatientIsNull_ThrowsArgumentNullException` and
  `AppointmentRequest_WhenDoctorIsNull_ThrowsArgumentNullException` (cover #3)
- `Doctor_ReserveSlot_WhenNoSlotsAvailable_ThrowsInvalidOperationException` and
  `Doctor_ReserveSlot_WhenAtDailyCapacity_ThrowsInvalidOperationException` (cover #4, by
  calling `ReserveSlot()` directly rather than through the service)
- `Doctor_ReleaseSlot_WhenDateWasNeverBooked_DailyCountDoesNotGoNegative` (covers #5)
- `Appointment_WhenIdIsWhitespace_ThrowsException` and
  `Appointment_WhenDoctorIsNull_ThrowsArgumentNullException` (cover #6)

## What code became covered?

- Every constructor guard clause across `Doctor`, `Patient`, `AppointmentRequest` and
  `Appointment` now has at least one direct test, rather than only being covered
  incidentally by tests aimed at something else.
- `Doctor.ReserveSlot()`'s two internal exception branches move from "provably unreachable
  through the service" to "directly exercised," which also documents in the test suite
  itself that these guards are defensive/redundant when called via
  `AppointmentBookingService`, and only meaningful if `ReserveSlot` is ever called from
  somewhere else in the future.
- `Doctor.ReleaseSlot()`'s floor-at-zero branch is exercised for the first time.

*(Once you run the tool: report the actual line/branch % before and after here, and name
which specific lines in the coverage report turned from red/uncovered to green/covered —
I'd expect `Doctor.cs`, `Patient.cs`, `AppointmentRequest.cs` and `Appointment.cs` to show
the biggest visible jump, since those had the most never-exercised guard clauses.)*

## What important quality risks are still not addressed even after coverage increases?

- **Concurrency / thread safety.** `Doctor.ReserveSlot()` and `ReleaseSlot()` both perform
  unsynchronised read-then-write on `AvailableSlots` *and* `_dailyAppointmentCounts`, with
  no locking. Coverage can show every line as "executed," but it says nothing about whether
  those two related pieces of state stay consistent when accessed concurrently — see
  Activity 6.
- **Real integration behaviour.** None of these 31+ tests touch a database, file system, or
  external service — coverage of in-memory code says nothing about real I/O (Activity 7).
- **System-level and business-user validation.** Coverage measures whether lines of
  production code executed — it says nothing about whether the console/UI workflow makes
  sense to an actual receptionist (Activity 5/6).
- **Assertion strength / message content.** A line can be "covered" by a test with a weak or
  missing assertion — e.g. `CancelAppointment`'s success message has no content check at
  all, even though the line that builds it will show as "covered" the moment any
  cancellation test runs.
- **Dead/unreachable code that still counts toward coverage if bypassed.** The patient-ID
  check in `AppointmentBookingService` and the two guards inside `ReserveSlot()` are only
  "coverable" by deliberately routing around the normal call path (as the new Activity 4
  tests do). A high coverage number achieved this way says the *line ran*, not that the
  *feature is reachable or meaningful* in real usage.

## Why would "100% line coverage" still be an insufficient release argument?

Line coverage only proves a statement *executed* at least once — it says nothing about
whether it executed correctly for every meaningful input, whether the assertion checked the
right outcome, whether independent conditions were combined in every important way (this
codebase alone has at least three: total slots, daily capacity, and cancellation state —
decision/condition coverage is a stronger claim than line coverage), whether the code is
safe under concurrent access, or whether real users and real external dependencies behave
correctly with it. This project makes that concrete: several branches (the patient-ID check,
`ReserveSlot`'s internal guards) can only be "covered" by tests that deliberately bypass the
normal call path — meaning a 100%-covered report can include code that is, in practice,
unreachable or dead in real production use.
