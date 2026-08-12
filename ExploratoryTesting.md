# Exploratory Testing — Activity 6

**Charter:** duplicated bookings, incorrect slot counts, misleading feedback.

## Observations

| Observation | Expected? | Evidence | Requirement clear? | Risk / next question |
|---|---|---|---|---|
| Two concurrent `BookAppointment` calls for a doctor's **last** slot could both succeed — `ReserveSlot()` checks `HasAvailableSlot()`/`HasCapacityOnDate()` then does a plain `AvailableSlots--` and dictionary update, with no lock | No | `Doctor.ReserveSlot`/`ReleaseSlot`, no synchronization anywhere | Unclear — spec never addresses concurrency | Could corrupt **two** related fields (`AvailableSlots` and `_dailyAppointmentCounts`) out of sync with each other, not just one |
| The same `AppointmentRequest` object can be submitted twice (double-click/retry); nothing detects it's a duplicate | No | No request ID/idempotency check anywhere in `BookAppointment` | Unclear | Same patient could be double-booked by a UI double-click |
| `CancelAppointment`'s guard against double-cancellation (`Appointment.Cancel()` throws if already cancelled) is solid, but if a cancel and a re-book race for the same freed slot, the interleaving is untested | No | `ReleaseSlot`/`ReserveSlot` share the same unsynchronized state | Unclear | Same underlying race as row 1, triggered from a different direction |
| `CancelAppointment`'s success message content is never asserted, unlike booking messages | Not a functional break | No `StringAssert` on cancellation messages anywhere | Clear this isn't tested, unclear if it matters | Minor — low priority |

## Risk assessment

| Risk | Likelihood | Impact | Priority | Testing response |
|---|---|---|---|---|
| Concurrent booking/cancellation corrupts `AvailableSlots` and/or `_dailyAppointmentCounts` | Medium | High — breaks the "cannot be negative" invariant, real overbooking | **High** | Concurrency-focused integration test (see Optional Challenge below); add locking |
| Duplicate submission of the same request books twice | Medium | Medium | **Medium** | Add a regression test documenting current behaviour; raise as a requirement question |
| Cancellation message content unverified | Low | Low | **Low** | Add a `StringAssert` test; low priority |

**If only one more test before release:** the concurrency test — it's the only risk that can
break a stated invariant, and it's gotten more dangerous now that two related counters need
to stay in sync instead of one.

**Confirmed defect vs. requirement gap:** the concurrency issue is closer to a confirmed
defect (the code claims an invariant it doesn't enforce under concurrent access); the
duplicate-submission behaviour is a requirement gap — idempotency was never specified.

**Why isn't unexpected behaviour automatically a defect?** It's relative to the tester's
assumptions, not necessarily to a documented requirement — some findings are legitimate
design trade-offs and need stakeholder clarification, not an automatic "bug" label.
