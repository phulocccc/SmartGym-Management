using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Services;

// SQL Server application locks serialize all writes that affect reservations.
// Locks are owned by the caller's transaction and released on commit/rollback.
public sealed class BookingService(ApplicationDbContext db)
{
    public async Task LockAsync()
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Reservation writes require a transaction.");
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'SmartGym.Reservations',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Cannot acquire reservation lock.', 1;
            """);
    }

    public async Task<string?> ValidateAsync(int memberId, int classId, int? excludingBookingId = null)
    {
        var member = await db.Members.AsNoTracking().Include(m => m.User).SingleOrDefaultAsync(m => m.MemberId == memberId);
        if (member?.User == null || !member.User.IsActive || member.Status != MemberStatus.Active)
            return "Tài khoản hội viên không hoạt động.";
        var schedule = await db.ClassSchedules.Include(c => c.Room).Include(c => c.Trainer)
            .SingleOrDefaultAsync(c => c.ClassId == classId);
        if (schedule == null || schedule.Status != ClassStatus.Scheduled || schedule.StartTime <= GymClock.Now)
            return "Lớp không còn nhận đặt chỗ.";
        if (schedule.Room?.IsActive != true || schedule.Trainer?.Status != TrainerStatus.Active)
            return "Phòng hoặc huấn luyện viên không hoạt động.";
        if (!await db.Memberships.AnyAsync(m => m.MemberId == memberId && m.Status == MembershipStatus.Active &&
            m.StartDate.Date <= schedule.StartTime.Date && m.EndDate.Date >= schedule.StartTime.Date))
            return "Gói tập không có hiệu lực vào ngày diễn ra lớp.";
        var reservations = db.Bookings.Where(b => b.BookingId != excludingBookingId &&
            (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended));
        if (await reservations.AnyAsync(b => b.MemberId == memberId && b.ClassId == classId))
            return "Hội viên đã đặt lớp này.";
        if (await reservations.CountAsync(b => b.ClassId == classId) >= schedule.Capacity)
            return "Lớp đã đủ số lượng hội viên.";
        if (await reservations.OverlappingReservations(memberId, classId, schedule.StartTime, schedule.EndTime).AnyAsync())
            return "Hội viên có lớp khác trùng giờ.";
        return null;
    }

    public async Task<string?> ValidateTransitionAsync(Booking booking, BookingStatus next)
    {
        var hasAttendance = await db.Attendances.AnyAsync(a => a.BookingId == booking.BookingId);
        var error = BookingTransitionRules.Validate(booking.Status, next, hasAttendance, booking.ClassSchedule?.EndTime, GymClock.Now);
        if (error != null) return error;
        if (next != booking.Status && next == BookingStatus.Booked)
            return await ValidateAsync(booking.MemberId, booking.ClassId, booking.BookingId);
        return null;
    }
}
