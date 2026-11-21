using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;

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
    }
}