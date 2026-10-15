using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Services;

// Invoked by an explicit admin POST, never during startup or a read-only report.
public sealed class MembershipMaintenanceService(ApplicationDbContext db, BookingService reservations)
{
    public async Task UpdateStatusesAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await reservations.LockAsync();
        var today = GymClock.Today;
        var deadline = GymClock.Now.AddHours(-2);
        await db.Memberships.Where(m => m.Status == MembershipStatus.Active && m.EndDate < today)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MembershipStatus.Expired));
        await db.Bookings.Where(b => b.Status == BookingStatus.Booked && b.ClassSchedule != null &&
            b.ClassSchedule.Status != ClassStatus.Cancelled && b.ClassSchedule.EndTime < deadline &&
            !db.Attendances.Any(a => a.BookingId == b.BookingId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.NoShow));
        await transaction.CommitAsync();
    }
}
