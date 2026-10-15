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
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public TrainersController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // GET: Admin/Trainers
        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            ViewData["StatusType"] = typeof(TrainerStatus);
            var parsedStatus = Enum.TryParse<TrainerStatus>(status, true, out var value) && Enum.IsDefined(value) ? (TrainerStatus?)value : null;
            var trainers = await _context.Trainers
                .Where(t => search == null || (t.FullName.Contains(search) || t.Email.Contains(search) || t.Specialization.Contains(search)))
                .Where(t => parsedStatus == null || t.Status == parsedStatus)
                .OrderBy(t => t.FullName)
                .ToPagedListAsync(this);

            return View(trainers);
        }

        // GET: Admin/Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // GET: Admin/Trainers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Trainers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trainer trainer)
        {
            trainer.TrainerId = 0;
            await ValidateTrainerUniqueAsync(trainer);

            if (ModelState.IsValid)
            {
                _context.Trainers.Add(new Trainer { FullName = trainer.FullName.Trim(), Specialization = trainer.Specialization.Trim(),
                    Email = trainer.Email, Phone = trainer.Phone, ExperienceYears = trainer.ExperienceYears, Status = trainer.Status });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(trainer);
        }

        // GET: Admin/Trainers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Admin/Trainers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trainer trainer)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (id != trainer.TrainerId)
            {
                return NotFound();
            }

            await ValidateTrainerUniqueAsync(trainer);

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Trainers.FindAsync(id);
                    if (existing == null) return NotFound();
                    existing.FullName = trainer.FullName.Trim();
                    existing.Email = trainer.Email;
                    existing.Phone = trainer.Phone;
                    existing.Specialization = trainer.Specialization.Trim();
                    existing.ExperienceYears = trainer.ExperienceYears;
                    existing.Status = trainer.Status;
                    await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(trainer.TrainerId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(trainer);
        }

        // GET: Admin/Trainers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Admin/Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var trainer = await _context.Trainers.FindAsync(id);

            if (trainer != null)
            {
                if (await _context.ClassSchedules.AnyAsync(c => c.TrainerId == trainer.TrainerId && c.Status == ClassStatus.Scheduled && c.EndTime > GymClock.Now))
                {
                    ModelState.AddModelError(string.Empty, "Cần chuyển hoặc hủy các lớp sắp tới trước khi ngừng hoạt động tài nguyên.");
                    return View("Delete", trainer);
                }
                trainer.Status = SmartGymManagement.Enums.TrainerStatus.Inactive;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(t => t.TrainerId == id);
        }

        private async Task ValidateTrainerUniqueAsync(Trainer trainer)
        {
            if (trainer.Status != TrainerStatus.Active && await _context.ClassSchedules.AnyAsync(c => c.TrainerId == trainer.TrainerId && c.Status == ClassStatus.Scheduled && c.EndTime > GymClock.Now))
                ModelState.AddModelError(string.Empty, "Cần chuyển hoặc hủy các lớp sắp tới trước khi ngừng hoạt động tài nguyên.");

            if (!Enum.IsDefined(trainer.Status)) ModelState.AddModelError(nameof(trainer.Status), "Trạng thái không hợp lệ.");
            trainer.Email = (trainer.Email ?? string.Empty).Trim().ToLower();
            trainer.Phone = (trainer.Phone ?? string.Empty).Trim();

            bool emailExists = await _context.Trainers.AnyAsync(t =>
                t.TrainerId != trainer.TrainerId &&
                t.Email.ToLower() == trainer.Email
            );

            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already used by another trainer.");
            }

            bool phoneExists = await _context.Trainers.AnyAsync(t =>
                t.TrainerId != trainer.TrainerId &&
                t.Phone == trainer.Phone
            );

            if (phoneExists)
            {
                ModelState.AddModelError("Phone", "This phone number is already used by another trainer.");
            }
        }
    }
}