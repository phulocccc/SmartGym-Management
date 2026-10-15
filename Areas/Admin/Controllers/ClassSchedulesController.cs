using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ClassSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public ClassSchedulesController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            ViewData["StatusType"] = typeof(ClassStatus);
            var parsedStatus = Enum.TryParse<ClassStatus>(status, true, out var value) && Enum.IsDefined(value) ? (ClassStatus?)value : null;
            var classSchedules = await _context.ClassSchedules
                .Include(c => c.Trainer)
                .Include(c => c.Room)
                .Where(c => search == null || (c.ClassName.Contains(search)))
                .Where(c => parsedStatus == null || c.Status == parsedStatus)
                .OrderBy(c => c.StartTime)
                .ToPagedListAsync(this);

            return View(classSchedules);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSchedule = await _context.ClassSchedules
                .Include(c => c.Trainer)
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classSchedule == null)
            {
                return NotFound();
            }

            return View(classSchedule);
        }

        public async Task<IActionResult> Create()
        {
            await LoadTrainersAsync();
            await LoadRoomsAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassSchedule classSchedule)
        {
            classSchedule.ClassId = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            await ValidateClassScheduleAsync(classSchedule);

            if (ModelState.IsValid)
            {
                _context.ClassSchedules.Add(new ClassSchedule { ClassName = classSchedule.ClassName.Trim(), TrainerId = classSchedule.TrainerId,
                    RoomId = classSchedule.RoomId, StartTime = classSchedule.StartTime, EndTime = classSchedule.EndTime,
                    Capacity = classSchedule.Capacity, Status = classSchedule.Status });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadTrainersAsync(classSchedule.TrainerId);
            await LoadRoomsAsync(classSchedule.RoomId);

            return View(classSchedule);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSchedule = await _context.ClassSchedules.FindAsync(id);

            if (classSchedule == null)
            {
                return NotFound();
            }

            await LoadTrainersAsync(classSchedule.TrainerId);
            await LoadRoomsAsync(classSchedule.RoomId);

            return View(classSchedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassSchedule classSchedule)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (id != classSchedule.ClassId)
            {
                return NotFound();
            }

            await ValidateClassScheduleAsync(classSchedule);

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.ClassSchedules.FindAsync(id);
                    if (existing == null) return NotFound();
                    existing.ClassName = classSchedule.ClassName.Trim();
                    existing.TrainerId = classSchedule.TrainerId;
                    existing.RoomId = classSchedule.RoomId;
                    existing.StartTime = classSchedule.StartTime;
                    existing.EndTime = classSchedule.EndTime;
                    existing.Capacity = classSchedule.Capacity;
                    existing.Status = classSchedule.Status;
                    if (existing.Status == ClassStatus.Cancelled) await CancelClassBookingsAsync(existing.ClassId);
                    await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassScheduleExists(classSchedule.ClassId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadTrainersAsync(classSchedule.TrainerId);
            await LoadRoomsAsync(classSchedule.RoomId);

            return View(classSchedule);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSchedule = await _context.ClassSchedules
                .Include(c => c.Trainer)
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classSchedule == null)
            {
                return NotFound();
            }

            return View(classSchedule);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var classSchedule = await _context.ClassSchedules.FindAsync(id);

            if (classSchedule != null)
            {
                if (await _context.Attendances.AnyAsync(a => a.ClassId == id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể hủy lớp đã có điểm danh.");
                    return View("Delete", classSchedule);
                }
                classSchedule.Status = ClassStatus.Cancelled;
                await CancelClassBookingsAsync(id);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(DateTime startTime, DateTime endTime, int? classId)
        {
            if (endTime <= startTime)
            {
                return Json(new
                {
                    success = false,
                    message = "End time must be after start time."
                });
            }

            var duration = endTime - startTime;

            if (duration.TotalHours > 3)
            {
                return Json(new
                {
                    success = false,
                    message = "A class session cannot be longer than 3 hours."
                });
            }

            var bookedRoomIds = await _context.ClassSchedules
                .Where(c =>
                    c.ClassId != classId &&
                    c.RoomId != null &&
                    c.Status != ClassStatus.Cancelled &&
                    startTime < c.EndTime &&
                    endTime > c.StartTime
                )
                .Select(c => c.RoomId!.Value)
                .Distinct()
                .ToListAsync();

            var availableRooms = await _context.Rooms
                .Where(r => r.IsActive && !bookedRoomIds.Contains(r.RoomId))
                .OrderBy(r => r.RoomName)
                .Select(r => new
                {
                    roomId = r.RoomId,
                    roomName = r.RoomName + " - Capacity: " + r.Capacity
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                rooms = availableRooms
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTrainers(DateTime startTime, DateTime endTime, int? classId)
        {
            if (endTime <= startTime)
            {
                return Json(new
                {
                    success = false,
                    message = "End time must be after start time."
                });
            }

            var duration = endTime - startTime;

            if (duration.TotalHours > 3)
            {
                return Json(new
                {
                    success = false,
                    message = "A class session cannot be longer than 3 hours."
                });
            }

            var bookedTrainerIds = await _context.ClassSchedules
                .Where(c =>
                    c.ClassId != classId &&
                    c.Status != ClassStatus.Cancelled &&
                    startTime < c.EndTime &&
                    endTime > c.StartTime
                )
                .Select(c => c.TrainerId)
                .Distinct()
                .ToListAsync();

            var availableTrainers = await _context.Trainers
                .Where(t =>
                    t.Status == TrainerStatus.Active &&
                    !bookedTrainerIds.Contains(t.TrainerId)
                )
                .OrderBy(t => t.FullName)
                .Select(t => new
                {
                    trainerId = t.TrainerId,
                    fullName = t.FullName + " - " + t.Specialization
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                trainers = availableTrainers
            });
        }

        private async Task CancelClassBookingsAsync(int classId)
        {
            var bookings = await _context.Bookings.Where(b => b.ClassId == classId && b.Status == BookingStatus.Booked).ToListAsync();
            foreach (var booking in bookings)
            {
                booking.Status = BookingStatus.Cancelled;
                _context.Notifications.Add(new Notification { MemberId = booking.MemberId,
                    Title = "Lớp học đã hủy", Message = "Lớp bạn đã đặt đã bị hủy. Vui lòng chọn một lớp khác." });
            }
        }

        private bool ClassScheduleExists(int id)
        {
            return _context.ClassSchedules.Any(c => c.ClassId == id);
        }

        private async Task LoadTrainersAsync(int? selectedTrainerId = null)
        {
            var trainers = await _context.Trainers
                .Where(t => t.Status == TrainerStatus.Active || t.TrainerId == selectedTrainerId)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.TrainerId = new SelectList(
                trainers,
                "TrainerId",
                "FullName",
                selectedTrainerId
            );
        }

        private async Task LoadRoomsAsync(int? selectedRoomId = null)
        {
            var rooms = await _context.Rooms
                .Where(r => r.IsActive || r.RoomId == selectedRoomId)
                .OrderBy(r => r.RoomName)
                .ToListAsync();

            ViewBag.RoomId = new SelectList(
                rooms,
                "RoomId",
                "RoomName",
                selectedRoomId
            );
        }

        private async Task ValidateClassScheduleAsync(ClassSchedule classSchedule)
        {
            if (classSchedule.EndTime <= classSchedule.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            var original = classSchedule.ClassId == 0 ? null : await _context.ClassSchedules.AsNoTracking().SingleOrDefaultAsync(c => c.ClassId == classSchedule.ClassId);
            if (!Enum.IsDefined(classSchedule.Status)) ModelState.AddModelError(nameof(classSchedule.Status), "Trạng thái không hợp lệ.");
            if (classSchedule.StartTime < GymClock.Now && (original == null || original.StartTime != classSchedule.StartTime))
            {
                ModelState.AddModelError("StartTime", "Start time cannot be in the past.");
            }

            var duration = classSchedule.EndTime - classSchedule.StartTime;

            if (duration.TotalHours > 3)
            {
                ModelState.AddModelError("EndTime", "A class session cannot be longer than 3 hours.");
            }

            if (classSchedule.Capacity <= 0)
            {
                ModelState.AddModelError("Capacity", "Capacity must be greater than 0.");
            }

            if (classSchedule.Capacity > 30)
            {
                ModelState.AddModelError("Capacity", "Capacity cannot be greater than 30.");
            }

            var trainer = await _context.Trainers.SingleOrDefaultAsync(t => t.TrainerId == classSchedule.TrainerId);
            if (trainer == null || (classSchedule.Status == ClassStatus.Scheduled && trainer.Status != TrainerStatus.Active))
                ModelState.AddModelError(nameof(classSchedule.TrainerId), "Huấn luyện viên không hoạt động hoặc không tồn tại.");
            var existingBookings = await _context.Bookings.Include(b => b.ClassSchedule)
                .Where(b => b.ClassId == classSchedule.ClassId && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended)).ToListAsync();
            if (existingBookings.Count > classSchedule.Capacity)
                ModelState.AddModelError(nameof(classSchedule.Capacity), "Sức chứa không thể thấp hơn số chỗ đã giữ.");
            if (original != null && (original.StartTime != classSchedule.StartTime || original.EndTime != classSchedule.EndTime))
            {
                if (existingBookings.Any(b => b.Status == BookingStatus.Attended))
                    ModelState.AddModelError(nameof(classSchedule.StartTime), "Không thể đổi giờ lớp đã có điểm danh.");
                foreach (var booking in existingBookings.Where(b => b.Status == BookingStatus.Booked))
                {
                    if (!await _context.Memberships.AnyAsync(m => m.MemberId == booking.MemberId && m.Status == MembershipStatus.Active &&
                        m.StartDate.Date <= classSchedule.StartTime.Date && m.EndDate.Date >= classSchedule.StartTime.Date))
                        ModelState.AddModelError(nameof(classSchedule.StartTime), "Giờ mới nằm ngoài thời hạn gói của hội viên đã đặt lớp.");
                    if (await _context.Bookings.OverlappingReservations(booking.MemberId, classSchedule.ClassId,
                        classSchedule.StartTime, classSchedule.EndTime).AnyAsync())
                        ModelState.AddModelError(nameof(classSchedule.StartTime), "Giờ mới trùng lịch của hội viên đã đặt lớp.");
                }
            }
            if (classSchedule.Status == ClassStatus.Cancelled)
            {
                if (existingBookings.Any(b => b.Status == BookingStatus.Attended))
                    ModelState.AddModelError(nameof(classSchedule.Status), "Không thể hủy lớp đã có điểm danh.");
                return;
            }
            if (classSchedule.Status == ClassStatus.Completed && classSchedule.EndTime > GymClock.Now)
                ModelState.AddModelError(nameof(classSchedule.Status), "Không thể hoàn tất lớp trước giờ kết thúc.");
            bool isTrainerBooked = await _context.ClassSchedules.AnyAsync(c =>
                c.ClassId != classSchedule.ClassId &&
                c.TrainerId == classSchedule.TrainerId &&
                c.Status != ClassStatus.Cancelled &&
                classSchedule.StartTime < c.EndTime &&
                classSchedule.EndTime > c.StartTime
            );

            if (isTrainerBooked)
            {
                ModelState.AddModelError("TrainerId", "This trainer already has another class during the selected time range. Please choose another trainer.");
            }

            if (classSchedule.RoomId == null)
            {
                ModelState.AddModelError("RoomId", "Room is required.");
                return;
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == classSchedule.RoomId);

            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Selected room does not exist.");
                return;
            }

            if (!room.IsActive && classSchedule.Status == ClassStatus.Scheduled)
            {
                ModelState.AddModelError("RoomId", "Selected room is not active.");
                return;
            }

            if (classSchedule.Status == ClassStatus.Scheduled && classSchedule.Capacity > room.Capacity)
            {
                ModelState.AddModelError("Capacity", $"Class capacity cannot exceed room capacity ({room.Capacity}).");
            }

            bool isRoomBooked = await _context.ClassSchedules.AnyAsync(c =>
                c.ClassId != classSchedule.ClassId &&
                c.RoomId == classSchedule.RoomId &&
                c.Status != ClassStatus.Cancelled &&
                classSchedule.StartTime < c.EndTime &&
                classSchedule.EndTime > c.StartTime
            );

            if (isRoomBooked)
            {
                ModelState.AddModelError("RoomId", "This room is already booked during the selected time range. Please choose another room.");
            }
        }
    }
}
