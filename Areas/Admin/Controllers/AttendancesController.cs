using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;
        private readonly BookingQrTokenService _qrTokens;

        public AttendancesController(ApplicationDbContext context, BookingService reservations, BookingQrTokenService qrTokens)
        {
            _context = context;
            _reservations = reservations;
            _qrTokens = qrTokens;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            var attendances = await _context.Attendances
                .Include(a => a.Member)
                    .ThenInclude(m => m!.User)
                .Include(a => a.Booking)
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(a => search == null || ((a.Member != null && a.Member.User != null && a.Member.User.FullName.Contains(search)) || (a.ClassSchedule != null && a.ClassSchedule.ClassName.Contains(search))))
                .OrderByDescending(a => a.CheckInTime)
                .ToPagedListAsync(this);

            return View(attendances);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Member)
                    .ThenInclude(m => m!.User)
                .Include(a => a.Booking)
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(a => a.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        public async Task<IActionResult> CheckIn()
        {
            await LoadBookingsAsync();

            return View(new AttendanceCheckInViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(AttendanceCheckInViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (model.Method == AttendanceMethod.QR)
            {
                if (!_qrTokens.TryRead(model.QrToken, out var tokenBooking, out _))
                    ModelState.AddModelError(nameof(model.QrToken), "QR không hợp lệ hoặc đã hết hạn.");
                else if (model.BookingId != 0 && model.BookingId != tokenBooking)
                    ModelState.AddModelError(nameof(model.QrToken), "QR không khớp booking được chọn.");
                else model.BookingId = tokenBooking;
            }
            var booking = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            await ValidateCheckInAsync(model, booking);

            if (!ModelState.IsValid)
            {
                await LoadBookingsAsync(model.BookingId);
                return View(model);
            }


            try
            {
                var attendance = new Attendance
                {
                    MemberId = booking!.MemberId,
                    BookingId = booking.BookingId,
                    ClassId = booking.ClassId,
                    CheckInTime = GymClock.Now,
                    Method = model.Method,
                    Note = model.Note?.Trim()
                };

                _context.Attendances.Add(attendance);

                booking.Status = BookingStatus.Attended;
                _context.Bookings.Update(booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Check-in completed successfully.";

                return RedirectToAction(nameof(Details), new { id = attendance.AttendanceId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "An error occurred while checking in.");

                await LoadBookingsAsync(model.BookingId);
                return View(model);
            }
        }

        private async Task LoadBookingsAsync(int? selectedBookingId = null)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(b =>
                    b.Status == BookingStatus.Booked &&
                    b.ClassSchedule != null
                )
                .OrderBy(b => b.ClassSchedule!.StartTime)
                .ToListAsync();

            var bookingItems = bookings.Select(b => new
            {
                b.BookingId,
                DisplayName =
                    $"{b.Member?.User?.FullName} - {b.Member?.User?.Email} - " +
                    $"{b.ClassSchedule?.ClassName} - " +
                    $"{b.ClassSchedule?.StartTime:dd/MM/yyyy HH:mm} - " +
                    $"{b.ClassSchedule?.Room?.RoomName}"
            }).ToList();

            ViewBag.BookingId = new SelectList(
                bookingItems,
                "BookingId",
                "DisplayName",
                selectedBookingId
            );
        }

        private async Task ValidateCheckInAsync(
            AttendanceCheckInViewModel model,
            Booking? booking)
        {
            if (!Enum.IsDefined(model.Method)) ModelState.AddModelError(nameof(model.Method), "Phương thức không hợp lệ.");
            if (model.Method == AttendanceMethod.QR && (!_qrTokens.TryRead(model.QrToken, out var tokenBooking, out var tokenMember) ||
                tokenBooking != booking?.BookingId || tokenMember != booking?.MemberId))
                ModelState.AddModelError(nameof(model.QrToken), "QR không hợp lệ, hết hạn hoặc không thuộc booking này.");
            if (booking == null)
            {
                ModelState.AddModelError("BookingId", "Selected booking does not exist.");
                return;
            }

            if (booking.Status != BookingStatus.Booked)
            {
                ModelState.AddModelError("BookingId", "Only booked bookings can be checked in.");
                return;
            }

            if (booking.Member == null || booking.Member.User == null)
            {
                ModelState.AddModelError("BookingId", "Member information was not found.");
                return;
            }

            if (!booking.Member.User.IsActive || booking.Member.Status != MemberStatus.Active)
            {
                ModelState.AddModelError("BookingId", "Selected member is not active.");
                return;
            }

            if (booking.ClassSchedule == null)
            {
                ModelState.AddModelError("BookingId", "Class schedule information was not found.");
                return;
            }

            var windowError = AttendanceWindowRules.Validate(booking.ClassSchedule.Status,
                booking.ClassSchedule.StartTime, booking.ClassSchedule.EndTime, GymClock.Now);
            if (windowError != null) ModelState.AddModelError("BookingId", windowError);

            bool hasActiveMembership = await _context.Memberships.AnyAsync(m =>
                m.MemberId == booking.MemberId &&
                m.Status == MembershipStatus.Active &&
                m.StartDate.Date <= booking.ClassSchedule.StartTime.Date &&
                m.EndDate.Date >= booking.ClassSchedule.StartTime.Date
            );

            if (!hasActiveMembership)
            {
                ModelState.AddModelError(
                    "BookingId",
                    "This member does not have an active membership on the class date."
                );
            }

            bool alreadyCheckedIn = await _context.Attendances.AnyAsync(a =>
                a.BookingId == booking.BookingId
            );

            if (alreadyCheckedIn)
            {
                ModelState.AddModelError(
                    "BookingId",
                    "This booking has already been checked in."
                );
            }
        }
    }
}
