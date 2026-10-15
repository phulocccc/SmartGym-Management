using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Services;

public sealed class ReportService(ApplicationDbContext db)
{
    public async Task<AdminReportsViewModel> BuildAsync(ReportPeriod period)
    {
        var start = period.From;
        var until = period.Until;
        var today = GymClock.Today;
        var now = GymClock.Now;
        var payments = db.PaymentTransactions.AsNoTracking();
        var bookings = db.Bookings.AsNoTracking().Where(b => b.BookingDate >= start && b.BookingDate < until);
        var attendance = db.Attendances.AsNoTracking().Where(a => a.CheckInTime >= start && a.CheckInTime < until);
        var gross = await payments.Where(p => (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Refunded) &&
            p.PaymentDate >= start && p.PaymentDate < until).Select(p => (decimal?)p.Amount).SumAsync() ?? 0;
        var refunds = await payments.Where(p => p.Status == PaymentStatus.Refunded &&
            (p.RefundedAt ?? p.PaymentDate) >= start && (p.RefundedAt ?? p.PaymentDate) < until)
            .Select(p => (decimal?)p.Amount).SumAsync() ?? 0;
        var pastBookings = db.Bookings.AsNoTracking().Where(b => b.Status != BookingStatus.Cancelled && b.ClassSchedule != null &&
            b.ClassSchedule.Status != ClassStatus.Cancelled && b.ClassSchedule.StartTime >= start && b.ClassSchedule.StartTime < until &&
            (b.Status == BookingStatus.Attended || b.Status == BookingStatus.NoShow || b.ClassSchedule.EndTime < now.AddHours(-2)));
        var completed = await pastBookings.CountAsync();
        var attended = await pastBookings.CountAsync(b => db.Attendances.Any(a => a.BookingId == b.BookingId));
        var model = new AdminReportsViewModel
        {
            From = period.From, To = period.To, GrossRevenue = gross, Refunds = refunds, TotalRevenue = gross - refunds,
            TotalMembers = await db.Members.CountAsync(),
            ActiveMembers = await db.Members.CountAsync(m => m.Status == MemberStatus.Active && m.User != null && m.User.IsActive),
            ActiveMemberships = await db.Memberships.CountAsync(m => m.Status == MembershipStatus.Active && m.StartDate.Date <= today && m.EndDate.Date >= today),
            UpcomingClasses = await db.ClassSchedules.CountAsync(c => c.Status == ClassStatus.Scheduled && c.StartTime > now),
            TotalBookings = await bookings.CountAsync(), TotalAttendances = await attendance.CountAsync(),
            AttendanceRate = completed == 0 ? 0 : Math.Round(attended * 100m / completed, 2)
        };
        var receiptMonths = await payments.Where(p => (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Refunded) && p.PaymentDate >= start && p.PaymentDate < until)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month }).Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.Amount) }).ToListAsync();
        var refundMonths = await payments.Where(p => p.Status == PaymentStatus.Refunded && (p.RefundedAt ?? p.PaymentDate) >= start && (p.RefundedAt ?? p.PaymentDate) < until)
            .GroupBy(p => new { Year = (p.RefundedAt ?? p.PaymentDate).Year, Month = (p.RefundedAt ?? p.PaymentDate).Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.Amount) }).ToListAsync();
        var bookingMonths = await bookings.GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
        var attendanceMonths = await attendance.GroupBy(a => new { a.CheckInTime.Year, a.CheckInTime.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
        for (var month = new DateTime(start.Year, start.Month, 1); month < until; month = month.AddMonths(1))
            model.MonthlyData.Add(new MonthlyReportItemViewModel
            {
                MonthLabel = month.ToString("MM/yyyy"),
                Revenue = (receiptMonths.SingleOrDefault(m => m.Year == month.Year && m.Month == month.Month)?.Amount ?? 0) -
                    (refundMonths.SingleOrDefault(m => m.Year == month.Year && m.Month == month.Month)?.Amount ?? 0),
                Bookings = bookingMonths.SingleOrDefault(m => m.Year == month.Year && m.Month == month.Month)?.Count ?? 0,
                Attendances = attendanceMonths.SingleOrDefault(m => m.Year == month.Year && m.Month == month.Month)?.Count ?? 0
            });
        model.TopClasses = await bookings.Where(b => b.Status != BookingStatus.Cancelled && b.ClassSchedule != null)
            .GroupBy(b => b.ClassSchedule!.ClassName).Select(g => new TopClassReportItemViewModel { ClassName = g.Key, BookingCount = g.Count() })
            .OrderByDescending(c => c.BookingCount).ThenBy(c => c.ClassName).Take(5).ToListAsync();
        model.TopPackages = await db.Memberships.AsNoTracking().Where(m => m.CreatedAt >= start && m.CreatedAt < until &&
                (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Expired))
            .GroupBy(m => m.PurchasedPackageName ?? m.Package!.PackageName)
            .Select(g => new TopPackageReportItemViewModel { PackageName = g.Key, PurchaseCount = g.Count() })
            .OrderByDescending(p => p.PurchaseCount).ThenBy(p => p.PackageName).Take(5).ToListAsync();
        return model;
    }
}
