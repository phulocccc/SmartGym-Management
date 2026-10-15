using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Services;

public static class BookingQueries
{
    public static IQueryable<Booking> OverlappingReservations(this IQueryable<Booking> bookings,
        int memberId, int excludingClassId, DateTime start, DateTime end) =>
        bookings.Where(b => b.MemberId == memberId && b.ClassId != excludingClassId &&
            (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended) &&
            b.ClassSchedule != null && b.ClassSchedule.Status != ClassStatus.Cancelled &&
            start < b.ClassSchedule.EndTime && end > b.ClassSchedule.StartTime);
}
