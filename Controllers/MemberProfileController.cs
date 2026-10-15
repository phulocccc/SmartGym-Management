using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Models;
using SmartGymManagement.Services;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Controllers;

[Authorize(Roles = "Member")]
public sealed class MemberProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    BookingService reservations) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();
        var member = await db.Members.SingleOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return NotFound();
        return View(new MemberProfileViewModel { FullName = user.FullName, PhoneNumber = user.PhoneNumber,
            DateOfBirth = member.DateOfBirth, Gender = member.Gender, Address = member.Address, EmergencyContact = member.EmergencyContact });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MemberProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await reservations.LockAsync();
        await db.Entry(user).ReloadAsync();
        var member = await db.Members.SingleOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return NotFound();
        if (!string.IsNullOrEmpty(model.Gender) && model.Gender != "Male" && model.Gender != "Female")
            ModelState.AddModelError(nameof(model.Gender), "Giới tính không hợp lệ.");
        var phone = model.PhoneNumber?.Trim();
        if (!string.IsNullOrEmpty(phone) && await db.Users.AnyAsync(u => u.Id != user.Id && u.PhoneNumber == phone))
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã được sử dụng.");
        if (!ModelState.IsValid) return View(model);
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = phone;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        member.DateOfBirth = model.DateOfBirth?.Date;
        member.Gender = model.Gender;
        member.Address = model.Address?.Trim();
        member.EmergencyContact = model.EmergencyContact?.Trim();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }
}
