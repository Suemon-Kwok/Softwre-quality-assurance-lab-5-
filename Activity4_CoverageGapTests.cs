using ENSE707_AppointmentBooking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ENSE707_AppointmentBooking.Tests
{
    // Activity 4 — targeted tests for branches identified as uncovered/weak
    // by manually reading the production source against the current 29 tests
    // (24 baseline + 5 from Activity 3). See docs/CoverageReview.md.
    // Run coverage before AND after adding these to see the change for yourself.
    [TestClass]
    public class Activity4_CoverageGapTests
    {
        // --- Doctor constructor: whitespace-only name guard, never tested ---
        [TestMethod]
        public void Doctor_WhenFullNameIsWhitespace_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Doctor("D001", "   ", 2));
        }

        // --- Patient constructor: whitespace-only legal name guard, never tested ---
        [TestMethod]
        public void Patient_WhenLegalNameIsWhitespace_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Patient("P001", "   "));
        }

        [TestMethod]
        public void Patient_WhenPreferredNameIsWhitespace_DisplayNameUsesLegalName()
        {
            var patient = new Patient("P001", "Diana William", "   ");

            Assert.AreEqual("Diana William", patient.DisplayName);
        }

        // --- AppointmentRequest constructor: null-guard branches, never tested ---
        [TestMethod]
        public void AppointmentRequest_WhenPatientIsNull_ThrowsArgumentNullException()
        {
            var doctor = new Doctor("D001", "Dr Mark", 2);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new AppointmentRequest(null!, doctor, DateTime.Today.AddDays(1)));
        }

        [TestMethod]
        public void AppointmentRequest_WhenDoctorIsNull_ThrowsArgumentNullException()
        {
            var patient = new Patient("P001", "Diana William");

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new AppointmentRequest(patient, null!, DateTime.Today.AddDays(1)));
        }

        // --- Doctor.ReserveSlot's OWN guards. AppointmentBookingService always
        // checks HasAvailableSlot()/HasCapacityOnDate() before calling ReserveSlot,
        // so these two branches are unreachable through the service - only a
        // direct call to ReserveSlot() can exercise them. Same "duplicated guard"
        // pattern as the dead patient-ID check flagged in Activity 1/2. ---
        [TestMethod]
        public void Doctor_ReserveSlot_WhenNoSlotsAvailable_ThrowsInvalidOperationException()
        {
            var doctor = new Doctor("D001", "Dr Mark", 0);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                doctor.ReserveSlot(DateTime.Today.AddDays(1)));
        }

        [TestMethod]
        public void Doctor_ReserveSlot_WhenAtDailyCapacity_ThrowsInvalidOperationException()
        {
            var doctor = new Doctor("D001", "Dr Mark", 5, maxDailyAppointments: 1);
            var date = DateTime.Today.AddDays(1);
            doctor.ReserveSlot(date); // fills the day's only slot directly

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                doctor.ReserveSlot(date));
        }

        // --- Doctor.ReleaseSlot's Math.Max(0, ...) floor. Every existing
        // cancellation test releases a slot for a date that WAS booked first,
        // so the "count would go negative" branch has never actually run. ---
        [TestMethod]
        public void Doctor_ReleaseSlot_WhenDateWasNeverBooked_DailyCountDoesNotGoNegative()
        {
            var doctor = new Doctor("D001", "Dr Mark", 2);
            var neverBookedDate = DateTime.Today.AddDays(5);

            doctor.ReleaseSlot(neverBookedDate);

            Assert.AreEqual(0, doctor.GetAppointmentCountForDate(neverBookedDate));
            Assert.AreEqual(3, doctor.AvailableSlots); // total slots still increments
        }

        // --- Appointment's OWN constructor guards. In normal use, Appointment
        // is only ever constructed internally by AppointmentBookingService with
        // already-valid data, so these guards have never been directly exercised. ---
        [TestMethod]
        public void Appointment_WhenIdIsWhitespace_ThrowsException()
        {
            var doctor = new Doctor("D001", "Dr Mark", 2);
            var patient = new Patient("P001", "Diana William");

            Assert.ThrowsExactly<ArgumentException>(() =>
                new Appointment("   ", doctor, patient, DateTime.Today.AddDays(1)));
        }

        [TestMethod]
        public void Appointment_WhenDoctorIsNull_ThrowsArgumentNullException()
        {
            var patient = new Patient("P001", "Diana William");

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new Appointment("A001", null!, patient, DateTime.Today.AddDays(1)));
        }
    }
}
