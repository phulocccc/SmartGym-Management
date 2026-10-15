using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using QRCoder;
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
    public class MemberBookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;
        private readonly BookingQrTokenService _qrTokens;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberBookingsController(
            ApplicationDbContext context,
            BookingService reservations, BookingQrTokenService qrTokens,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _reservations = reservations;
            _qrTokens = qrTokens;
            _userManager = userManager;
        }

        public async Task<IActionResult> AvailableClasses()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound("Member profile was not found for this account.");
            }

            var classes = await _context.ClassSchedules
                .Include(c => c.Trainer)
                .Include(c => c.Room)
                .Where(c =>
                    c.Status == ClassStatus.Scheduled &&
                    c.StartTime > GymClock.Now
                )
                .OrderBy(c => c.StartTime)
                .ToPagedListAsync(this);

            var result = new List<AvailableClassViewModel>();

            var classIds = classes.Select(c => c.ClassId).ToList();
            var counts = await _context.Bookings.Where(b => classIds.Contains(b.ClassId) &&
                (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended))
                .GroupBy(b => b.ClassId).Select(g => new { ClassId = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.ClassId, g => g.Count);
            var ownBookings = await _context.Bookings.AsNoTracking().Include(b => b.ClassSchedule)
                .Where(b => b.MemberId == member.MemberId && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended) && b.ClassSchedule != null &&
                    b.ClassSchedule.Status == ClassStatus.Scheduled && b.ClassSchedule.EndTime > GymClock.Now).ToListAsync();
            var memberships = await _context.Memberships.AsNoTracking().Where(m => m.MemberId == member.MemberId && m.Status == MembershipStatus.Active).ToListAsync();
            foreach (var classSchedule in classes)
            {
                int bookedCount = counts.GetValueOrDefault(classSchedule.ClassId);
                bool alreadyBooked = ownBookings.Any(b => b.ClassId == classSchedule.ClassId);
                bool hasActiveMembership = memberships.Any(m => m.StartDate.Date <= classSchedule.StartTime.Date && m.EndDate.Date >= classSchedule.StartTime.Date);
                bool hasOverlappingBooking = ownBookings.Any(b => b.ClassId != classSchedule.ClassId &&
                    classSchedule.StartTime < b.ClassSchedule!.EndTime && classSchedule.EndTime > b.ClassSchedule.StartTime);

                int remainingSlots = classSchedule.Capacity - bookedCount;
                bool isFull = remainingSlots <= 0;

                string cannotBookReason = string.Empty;

                if (member.Status != MemberStatus.Active || member.User == null || !member.User.IsActive)
                {
                    cannotBookReason = "Your account is not active.";
                }
                else if (classSchedule.Room?.IsActive != true || classSchedule.Trainer?.Status != TrainerStatus.Active)
                {
                    cannotBookReason = "Phòng hoặc huấn luyện viên không hoạt động.";
                }
                else if (!hasActiveMembership)
                {
                    cannotBookReason = "You do not have an active membership on this class date.";
                }
                else if (alreadyBooked)
                {
                    cannotBookReason = "You already booked this class.";
                }
                else if (isFull)
                {
                    cannotBookReason = "This class is full.";
                }
                else if (hasOverlappingBooking)
                {
                    cannotBookReason = "You already have another booking during this time.";
                }

                bool canBook = string.IsNullOrEmpty(cannotBookReason);

                result.Add(new AvailableClassViewModel
                {
                    ClassId = classSchedule.ClassId,
                    ClassName = classSchedule.ClassName,
                    TrainerName = classSchedule.Trainer?.FullName,
                    RoomName = classSchedule.Room?.RoomName,
                    StartTime = classSchedule.StartTime,
                    EndTime = classSchedule.EndTime,
                    Capacity = classSchedule.Capacity,
                    BookedCount = bookedCount,
                    RemainingSlots = remainingSlots,
                    IsFull = isFull,
                    AlreadyBooked = alreadyBooked,
                    HasActiveMembership = hasActiveMembership,
                    CanBook = canBook,
                    CannotBookReason = cannotBookReason
                });
            }

            return View(result);
        }

        public async Task<IActionResult> MyBookings()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound("Member profile was not found for this account.");
            }

            var bookings = await _context.Bookings
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BookingDate)
                .ToPagedListAsync(this);

            return View(bookings);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Qr(int bookingId)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();
            var booking = await _context.Bookings.AsNoTracking().Include(b => b.ClassSchedule).SingleOrDefaultAsync(b =>
                b.BookingId == bookingId && b.MemberId == member.MemberId);
            if (booking?.ClassSchedule == null || booking.Status != BookingStatus.Booked || !AttendanceWindowRules.AllowsAttendance(booking.ClassSchedule.Status) ||
                booking.ClassSchedule.EndTime.AddHours(2) < GymClock.Now) return NotFound();
            var token = _qrTokens.Issue(booking.BookingId, member.MemberId);
            var png = PngByteQRCodeHelper.GetQRCode(token, QRCodeGenerator.ECCLevel.Q, 8);
            return View(new BookingQrViewModel { ClassName = booking.ClassSchedule.ClassName, Token = token, ImageBase64 = Convert.ToBase64String(png) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int classId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound("Member profile was not found for this account.");
            }

            await ValidateMemberSelfBookingAsync(member.MemberId, classId);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = GetFirstModelStateError();
                return RedirectToAction(nameof(AvailableClasses));
            }

            var booking = new Booking
            {
                MemberId = member.MemberId,
                ClassId = classId,
                BookingDate = GymClock.Now,
                Status = BookingStatus.Booked,
                Note = "Booked by member"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
                await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Class booked successfully.";

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound("Member profile was not found for this account.");
            }

            var booking = await _context.Bookings
                .Include(b => b.ClassSchedule)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.MemberId == member.MemberId
                );

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Booked)
            {
                TempData["ErrorMessage"] = "Only booked classes can be cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.ClassSchedule != null && booking.ClassSchedule.StartTime <= GymClock.Now)
            {
                TempData["ErrorMessage"] = "You cannot cancel a class that has already started or passed.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = BookingStatus.Cancelled;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
                await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Booking cancelled successfully.";

            return RedirectToAction(nameof(MyBookings));
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            return await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);
        }

        private async Task ValidateMemberSelfBookingAsync(int memberId, int classId)
        {
            var error = await _reservations.ValidateAsync(memberId, classId);
            if (error != null) ModelState.AddModelError(string.Empty, error);
        }

        private string GetFirstModelStateError()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Booking failed.";
        }
    }
}
