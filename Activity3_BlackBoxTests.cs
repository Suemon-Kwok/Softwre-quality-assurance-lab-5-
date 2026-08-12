using ENSE707_AppointmentBooking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ENSE707_AppointmentBooking.Tests
{
    // Activity 3 — black-box tests designed from the actual rules enforced by
    // Doctor, AppointmentRequest and AppointmentBookingService (see
    // docs/BlackBoxTestDesign.md), not from the lab sheet's Rule set B, which
    // is out of date for this project (today is now INVALID, not valid).
    [TestClass]
    public class Activity3_BlackBoxTests
    {
        // Slot boundary: 0 -> false, 1 -> true, 2 -> true (matches the lab's example pattern)
        [TestMethod]
        [DataRow(0, false)]
        [DataRow(1, true)]
        [DataRow(2, true)]
        public void HasAvailableSlot_BoundaryCases(int availableSlots, bool expected)
        {
            var doctor = new Doctor("D001", "Dr Mark", availableSlots);
            Assert.AreEqual(expected, doctor.HasAvailableSlot());
        }

        // Appointment-date boundary, corrected for the real <= DateTime.Today rule:
        // yesterday throws, TODAY throws (the lab sheet is wrong about this), tomorrow does not.
        [TestMethod]
        [DataRow(-1, true)]   // yesterday -> should throw
        [DataRow(0, true)]    // today -> should throw (corrected boundary)
        [DataRow(1, false)]   // tomorrow -> should NOT throw
        public void AppointmentRequest_DateBoundaryCases(int daysFromToday, bool expectedToThrow)
        {
            var doctor = new Doctor("D001", "Dr Mark", 1);
            var patient = new Patient("P001", "Diana William");
            DateTime requestedDate = DateTime.Today.AddDays(daysFromToday);

            if (expectedToThrow)
            {
                Assert.ThrowsExactly<ArgumentException>(() =>
                    new AppointmentRequest(patient, doctor, requestedDate));
            }
            else
            {
                var request = new AppointmentRequest(patient, doctor, requestedDate);
                Assert.AreEqual(requestedDate, request.RequestedDate);
            }
        }

        // Previously-untested constructor boundary: maxDailyAppointments <= 0 must throw,
        // and 1 (the smallest valid value) must succeed.
        [TestMethod]
        [DataRow(-1, true)]
        [DataRow(0, true)]
        [DataRow(1, false)]
        public void Doctor_MaxDailyAppointmentsBoundaryCases(int maxDailyAppointments, bool expectedToThrow)
        {
            if (expectedToThrow)
            {
                Assert.ThrowsExactly<ArgumentException>(() =>
                    new Doctor("D001", "Dr Mark", 2, maxDailyAppointments));
            }
            else
            {
                var doctor = new Doctor("D001", "Dr Mark", 2, maxDailyAppointments);
                Assert.AreEqual(maxDailyAppointments, doctor.MaxDailyAppointments);
            }
        }

        // R1 in the decision table: a null request must fail gracefully, not throw.
        [TestMethod]
        public void BookAppointment_WhenRequestIsNull_ReturnsFailureWithoutThrowing()
        {
            var service = new AppointmentBookingService();

            BookingResult result = service.BookAppointment(null!);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "missing");
        }

        // Missing boundary case identified in BlackBoxTestDesign.md: exactly one slot.
        [TestMethod]
        public void BookAppointment_WhenExactlyOneSlotAvailable_SucceedsAndLeavesZero()
        {
            var doctor = new Doctor("D001", "Dr Mark", 1);
            var patient = new Patient("P001", "Diana William");
            var request = new AppointmentRequest(patient, doctor, DateTime.Today.AddDays(1));
            var service = new AppointmentBookingService();

            BookingResult result = service.BookAppointment(request);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, doctor.AvailableSlots);
        }
    }
}
