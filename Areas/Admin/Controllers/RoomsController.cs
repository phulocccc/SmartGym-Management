using SmartGymManagement.Enums;
using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Models;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public RoomsController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            var rooms = await _context.Rooms
                .Where(r => search == null || (r.RoomName.Contains(search)))
                .OrderBy(r => r.RoomName)
                .ToPagedListAsync(this);

            return View(rooms);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            room.RoomId = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            await ValidateRoomAsync(room);

            if (ModelState.IsValid)
            {
                _context.Rooms.Add(new Room { RoomName = room.RoomName, Capacity = room.Capacity, IsActive = room.IsActive });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (id != room.RoomId)
            {
                return NotFound();
            }

            await ValidateRoomAsync(room);

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Rooms.FindAsync(id);
                    if (existing == null) return NotFound();
                    existing.RoomName = room.RoomName;
                    existing.Capacity = room.Capacity;
                    existing.IsActive = room.IsActive;
                    await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var room = await _context.Rooms.FindAsync(id);

            if (room != null)
            {
                if (await _context.ClassSchedules.AnyAsync(c => c.RoomId == room.RoomId && c.Status == ClassStatus.Scheduled && c.EndTime > GymClock.Now))
                {
                    ModelState.AddModelError(string.Empty, "Cần chuyển hoặc hủy các lớp sắp tới trước khi ngừng hoạt động tài nguyên.");
                    return View("Delete", room);
                }
                room.IsActive = false;
                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(r => r.RoomId == id);
        }

        private async Task ValidateRoomAsync(Room room)
        {
            if (!room.IsActive && await _context.ClassSchedules.AnyAsync(c => c.RoomId == room.RoomId && c.Status == ClassStatus.Scheduled && c.EndTime > GymClock.Now))
                ModelState.AddModelError(string.Empty, "Cần chuyển hoặc hủy các lớp sắp tới trước khi ngừng hoạt động tài nguyên.");

            room.RoomName = (room.RoomName ?? string.Empty).Trim();
            if (await _context.ClassSchedules.AnyAsync(c => c.RoomId == room.RoomId &&
                c.Status == SmartGymManagement.Enums.ClassStatus.Scheduled && c.Capacity > room.Capacity))
                ModelState.AddModelError(nameof(room.Capacity), "Sức chứa phòng không thể thấp hơn sức chứa lớp đã lên lịch.");

            bool roomNameExists = await _context.Rooms.AnyAsync(r =>
                r.RoomId != room.RoomId &&
                r.RoomName.ToLower() == room.RoomName.ToLower()
            );

            if (roomNameExists)
            {
                ModelState.AddModelError("RoomName", "This room name already exists.");
            }
        }
    }
}