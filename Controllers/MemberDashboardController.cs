using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (member == null)
            {
                return NotFound("Member profile was not found for this account.");
            }

            DateTime today = GymClock.Today;
            DateTime now = GymClock.Now;

            var activeMembership = await _context.Memberships
                .Include(m => m.Package)
                .Where(m =>
                    m.MemberId == member.MemberId &&
                    m.Status == MembershipStatus.Active &&
                    m.StartDate.Date <= today &&
                    m.EndDate.Date >= today
                )
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            int remainingDays = 0;

            if (activeMembership != null)
            {
                remainingDays = Math.Max(
                    0,
                    (activeMembership.EndDate.Date - today).Days + 1
                );
            }

            int totalBookings = await _context.Bookings
                .CountAsync(b => b.MemberId == member.MemberId);

            int upcomingBookingsCount = await _context.Bookings
                .Include(b => b.ClassSchedule)
                .CountAsync(b =>
                    b.MemberId == member.MemberId &&
                    b.Status == BookingStatus.Booked &&
                    b.ClassSchedule != null &&
                    b.ClassSchedule.StartTime > now && b.ClassSchedule.Status == ClassStatus.Scheduled
                );

            int attendedCount = await _context.Bookings
                .CountAsync(b =>
                    b.MemberId == member.MemberId &&
                    b.Status == BookingStatus.Attended
                );

            int cancelledCount = await _context.Bookings
                .CountAsync(b =>
                    b.MemberId == member.MemberId &&
                    b.Status == BookingStatus.Cancelled
                );

            int noShowCount = await _context.Bookings
                .CountAsync(b =>
                    b.MemberId == member.MemberId &&
                    b.Status == BookingStatus.NoShow
                );

            var upcomingBookingEntities = await _context.Bookings
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(b =>
                    b.MemberId == member.MemberId &&
                    b.Status == BookingStatus.Booked &&
                    b.ClassSchedule != null &&
                    b.ClassSchedule.StartTime > now && b.ClassSchedule.Status == ClassStatus.Scheduled
                )
                .OrderBy(b => b.ClassSchedule!.StartTime)
                .Take(5)
                .ToListAsync();

            var upcomingBookings = upcomingBookingEntities
                .Select(b => new MemberDashboardBookingItemViewModel
                {
                    BookingId = b.BookingId,
                    ClassName = b.ClassSchedule?.ClassName ?? string.Empty,
                    TrainerName = b.ClassSchedule?.Trainer?.FullName,
                    RoomName = b.ClassSchedule?.Room?.RoomName,
                    StartTime = b.ClassSchedule?.StartTime ?? DateTime.MinValue,
                    EndTime = b.ClassSchedule?.EndTime ?? DateTime.MinValue,
                    Status = b.Status.ToString()
                })
                .ToList();

            var attendanceEntities = await _context.Attendances
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(a => a.MemberId == member.MemberId)
                .OrderByDescending(a => a.CheckInTime)
                .Take(5)
                .ToListAsync();

            var recentAttendances = attendanceEntities
                .Select(a => new MemberDashboardAttendanceItemViewModel
                {
                    AttendanceId = a.AttendanceId,
                    ClassName = a.ClassSchedule?.ClassName ?? string.Empty,
                    TrainerName = a.ClassSchedule?.Trainer?.FullName,
                    RoomName = a.ClassSchedule?.Room?.RoomName,
                    CheckInTime = a.CheckInTime,
                    Method = a.Method.ToString()
                })
                .ToList();

            var model = new MemberDashboardViewModel
            {
                FullName = member.User?.FullName ?? string.Empty,
                Email = member.User?.Email ?? string.Empty,
                PhoneNumber = member.User?.PhoneNumber,
                MemberStatus = member.Status.ToString(),
                JoinDate = member.JoinDate,

                HasActiveMembership = activeMembership != null,
                PackageName = activeMembership?.PurchasedPackageName ?? activeMembership?.Package?.PackageName,
                MembershipStartDate = activeMembership?.StartDate,
                MembershipEndDate = activeMembership?.EndDate,
                RemainingDays = remainingDays,

                TotalBookings = totalBookings,
                UpcomingBookingsCount = upcomingBookingsCount,
                AttendedCount = attendedCount,
                CancelledCount = cancelledCount,
                NoShowCount = noShowCount,

                UpcomingBookings = upcomingBookings,
                RecentAttendances = recentAttendances
            };

            return View(model);
        }
    }
}