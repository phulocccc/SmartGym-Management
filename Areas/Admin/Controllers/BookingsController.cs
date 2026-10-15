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
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public BookingsController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            ViewData["StatusType"] = typeof(BookingStatus);
            var parsedStatus = Enum.TryParse<BookingStatus>(status, true, out var value) && Enum.IsDefined(value) ? (BookingStatus?)value : null;
            var bookings = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .Where(b => search == null || ((b.Member != null && b.Member.User != null && b.Member.User.FullName.Contains(search)) || (b.ClassSchedule != null && b.ClassSchedule.ClassName.Contains(search))))
                .Where(b => parsedStatus == null || b.Status == parsedStatus)
                .OrderByDescending(b => b.BookingDate)
                .ToPagedListAsync(this);

            return View(bookings);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View(new BookingCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            await ValidateBookingAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model.MemberId, model.ClassId);
                return View(model);
            }

            var booking = new Booking
            {
                MemberId = model.MemberId,
                ClassId = model.ClassId,
                BookingDate = GymClock.Now,
                Status = BookingStatus.Booked,
                Note = model.Note?.Trim()
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
                await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Booking created successfully.";

            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
        }

        // GET: Admin/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var model = new BookingEditViewModel
            {
                BookingId = booking.BookingId,

                MemberName = booking.Member?.User?.FullName
                    ?? "Unknown Member",

                MemberEmail = booking.Member?.User?.Email
                    ?? string.Empty,

                ClassName = booking.ClassSchedule?.ClassName
                    ?? "Unknown Class",

                StartTime = booking.ClassSchedule?.StartTime
                    ?? DateTime.MinValue,

                EndTime = booking.ClassSchedule?.EndTime
                    ?? DateTime.MinValue,

                Status = booking.Status,

                Note = booking.Note
            };

            return View(model);
        }

        // POST: Admin/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            BookingEditViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (id != model.BookingId)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var transitionError = await _reservations.ValidateTransitionAsync(booking, model.Status);
            if (transitionError != null) ModelState.AddModelError(nameof(model.Status), transitionError);
            if (ModelState.IsValid)
            {
                booking.Status = model.Status;
                booking.Note = model.Note?.Trim();

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Booking updated successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = booking.BookingId }
                );
            }

            model.MemberName =
                booking.Member?.User?.FullName ?? "Unknown Member";

            model.MemberEmail =
                booking.Member?.User?.Email ?? string.Empty;

            model.ClassName =
                booking.ClassSchedule?.ClassName ?? "Unknown Class";

            model.StartTime =
                booking.ClassSchedule?.StartTime ?? DateTime.MinValue;

            model.EndTime =
                booking.ClassSchedule?.EndTime ?? DateTime.MinValue;

            return View(model);
        }

        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Member)
                    .ThenInclude(m => m!.User)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Trainer)
                .Include(b => b.ClassSchedule)
                    .ThenInclude(c => c!.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == BookingStatus.Booked)
            {
                booking.Status = BookingStatus.Cancelled;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownsAsync(int? selectedMemberId = null, int? selectedClassId = null)
        {
            var members = await _context.Members
                .Include(m => m.User)
                .Where(m =>
                    m.Status == MemberStatus.Active &&
                    m.User != null &&
                    m.User.IsActive
                )
                .OrderBy(m => m.User!.FullName)
                .ToListAsync();

            var memberItems = members.Select(m => new
            {
                m.MemberId,
                DisplayName = $"{m.User?.FullName} - {m.User?.Email}"
            }).ToList();

            ViewBag.MemberId = new SelectList(
                memberItems,
                "MemberId",
                "DisplayName",
                selectedMemberId
            );

            var classes = await _context.ClassSchedules
                .Include(c => c.Trainer)
                .Include(c => c.Room)
                .Where(c =>
                    c.Status == ClassStatus.Scheduled &&
                    c.StartTime > GymClock.Now
                )
                .OrderBy(c => c.StartTime)
                .ToListAsync();

            var classIds = classes.Select(c => c.ClassId).ToList();
            var bookingCounts = await _context.Bookings.Where(b => classIds.Contains(b.ClassId) &&
                (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended)).GroupBy(b => b.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.ClassId, g => g.Count);
            var classItems = new List<object>();

            foreach (var classSchedule in classes)
            {
                int bookedCount = bookingCounts.GetValueOrDefault(classSchedule.ClassId);

                int remainingSlots = classSchedule.Capacity - bookedCount;

                classItems.Add(new
                {
                    classSchedule.ClassId,
                    DisplayName =
                        $"{classSchedule.ClassName} - {classSchedule.StartTime:dd/MM/yyyy HH:mm} - {classSchedule.Trainer?.FullName} - {classSchedule.Room?.RoomName} - Slots: {remainingSlots}"
                });
            }

            ViewBag.ClassId = new SelectList(
                classItems,
                "ClassId",
                "DisplayName",
                selectedClassId
            );
        }

        private async Task ValidateBookingAsync(BookingCreateViewModel model)
        {
            var error = await _reservations.ValidateAsync(model.MemberId, model.ClassId);
            if (error != null) ModelState.AddModelError(string.Empty, error);
        }
    }
}