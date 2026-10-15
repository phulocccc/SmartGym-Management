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
    public class MembershipPackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public MembershipPackagesController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            var packages = await _context.MembershipPackages
                .Where(p => search == null || (p.PackageName.Contains(search)))
                .OrderByDescending(p => p.CreatedAt)
                .ToPagedListAsync(this);

            return View(packages);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.MembershipPackages
                .FirstOrDefaultAsync(m => m.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembershipPackage package)
        {
            if (ModelState.IsValid)
            {
                package.CreatedAt = GymClock.Now;
                _context.Add(new MembershipPackage { PackageName = package.PackageName.Trim(), Description = package.Description?.Trim(),
                    Price = package.Price, DurationDays = package.DurationDays, IsActive = package.IsActive });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(package);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.MembershipPackages.FindAsync(id);

            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MembershipPackage package)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (id != package.PackageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.MembershipPackages.FindAsync(id);
                    if (existing == null) return NotFound();
                    existing.PackageName = package.PackageName.Trim();
                    existing.Description = package.Description?.Trim();
                    existing.Price = package.Price;
                    existing.DurationDays = package.DurationDays;
                    existing.IsActive = package.IsActive;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MembershipPackages.Any(e => e.PackageId == package.PackageId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(package);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.MembershipPackages
                .FirstOrDefaultAsync(m => m.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var package = await _context.MembershipPackages.FindAsync(id);

            if (package != null)
            {
                package.IsActive = false;
                await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}