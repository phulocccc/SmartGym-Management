using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MembersController(
            ApplicationDbContext context, BookingService reservations,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _reservations = reservations;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            ViewData["StatusType"] = typeof(MemberStatus);
            var parsedStatus = Enum.TryParse<MemberStatus>(status, true, out var value) && Enum.IsDefined(value) ? (MemberStatus?)value : null;
            var members = await _context.Members
                .Include(m => m.User)
                .Where(m => search == null || (m.User != null && (m.User.FullName.Contains(search) || m.User.Email!.Contains(search))))
                .Where(m => parsedStatus == null || m.Status == parsedStatus)
                .OrderByDescending(m => m.JoinDate)
                .ToPagedListAsync(this);

            return View(members);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        public IActionResult Create()
        {
            return View(new MemberFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberFormViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            await ValidateMemberAsync(model, null);

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim().ToLower(),
                    Email = model.Email.Trim().ToLower(),
                    EmailConfirmed = true,
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim(),
                    CreatedAt = GymClock.Now,
                    IsActive = model.IsActive
                };

                var createUserResult = await _userManager.CreateAsync(user);

                if (!createUserResult.Succeeded)
                {
                    foreach (var error in createUserResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Member"));
                    if (!roleResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể tạo vai trò hội viên.");
                        return View(model);
                    }
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, "Member");

                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    return View(model);
                }

                var member = new Member
                {
                    UserId = user.Id,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender?.Trim(),
                    Address = model.Address?.Trim(),
                    EmergencyContact = model.EmergencyContact?.Trim(),
                    JoinDate = GymClock.Now,
                    Status = model.Status
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                TempData["AccountSetupLink"] = Url.Action("Index", "AccountSetup", new { area = "", userId = user.Id, token = encodedToken });
                TempData["SuccessMessage"] = "Đã tạo hội viên. Chuyển liên kết thiết lập mật khẩu riêng cho hội viên qua kênh bảo mật.";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null || member.User == null)
            {
                return NotFound();
            }

            var model = new MemberFormViewModel
            {
                MemberId = member.MemberId,
                UserId = member.UserId,
                FullName = member.User.FullName,
                Email = member.User.Email ?? string.Empty,
                PhoneNumber = member.User.PhoneNumber,
                DateOfBirth = member.DateOfBirth,
                Gender = member.Gender,
                Address = member.Address,
                EmergencyContact = member.EmergencyContact,
                Status = member.Status,
                IsActive = member.User.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MemberFormViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();

            if (id != model.MemberId)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null || member.User == null)
            {
                return NotFound();
            }

            await _context.Entry(member.User).ReloadAsync();
            await ValidateMemberAsync(model, member.UserId);

            if (ModelState.IsValid)
            {
                member.User.FullName = model.FullName.Trim();
                member.User.Email = model.Email.Trim().ToLower();
                member.User.UserName = model.Email.Trim().ToLower();
                member.User.PhoneNumber = model.PhoneNumber?.Trim();
                member.User.IsActive = model.IsActive;

                var updateUserResult = await _userManager.UpdateAsync(member.User);

                if (!updateUserResult.Succeeded)
                {
                    foreach (var error in updateUserResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                member.DateOfBirth = model.DateOfBirth;
                member.Gender = model.Gender?.Trim();
                member.Address = model.Address?.Trim();
                member.EmergencyContact = model.EmergencyContact?.Trim();
                member.Status = model.Status;

                _context.Members.Update(member);
                await _context.SaveChangesAsync();
                var stampResult = await _userManager.UpdateSecurityStampAsync(member.User);
                if (!stampResult.Succeeded) return BadRequest("Không thể cập nhật tài khoản. Vui lòng thử lại.");
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            if (member != null)
            {
                member.Status = MemberStatus.Inactive;

                if (member.User != null)
                {
                    member.User.IsActive = false;
                    var result = await _userManager.UpdateAsync(member.User);
                    if (!result.Succeeded) return BadRequest("Không thể khóa tài khoản.");
                    await _userManager.UpdateSecurityStampAsync(member.User);
                }

                _context.Members.Update(member);
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueSetupLink(int id)
        {
            var member = await _context.Members.Include(m => m.User).SingleOrDefaultAsync(m => m.MemberId == id);
            if (member?.User == null) return NotFound();
            if (!member.User.IsActive || await _userManager.HasPasswordAsync(member.User))
                return BadRequest("Tài khoản không hoạt động hoặc đã có mật khẩu.");
            var result = await _userManager.UpdateSecurityStampAsync(member.User);
            if (!result.Succeeded) return BadRequest("Không thể cấp lại liên kết lúc này.");
            var token = await _userManager.GeneratePasswordResetTokenAsync(member.User);
            TempData["AccountSetupLink"] = Url.Action("Index", "AccountSetup", new { area = "", userId = member.UserId,
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)) });
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateMemberAsync(MemberFormViewModel model, string? currentUserId)
        {
            if (!Enum.IsDefined(model.Status)) ModelState.AddModelError(nameof(model.Status), "Trạng thái không hợp lệ.");
            if (!string.IsNullOrEmpty(model.Gender) && model.Gender != "Male" && model.Gender != "Female")
                ModelState.AddModelError(nameof(model.Gender), "Giới tính phải là Male hoặc Female.");
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                model.Email = model.Email.Trim().ToLower();

                bool isSmartGymEmail = Regex.IsMatch(
                    model.Email,
                    @"^[A-Za-z0-9._%+-]+@smartgym\.com$"
                );

                if (!isSmartGymEmail)
                {
                    ModelState.AddModelError("Email", "Email must be valid and end with @smartgym.com.");
                }

                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);

                if (existingUserByEmail != null && existingUserByEmail.Id != currentUserId)
                {
                    ModelState.AddModelError("Email", "This email is already used by another user.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                model.PhoneNumber = model.PhoneNumber.Trim();

                bool phoneExists = await _context.Users.AnyAsync(u =>
                    u.Id != currentUserId &&
                    u.PhoneNumber == model.PhoneNumber
                );

                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "This phone number is already used by another user.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                model.FullName = model.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.Gender))
            {
                model.Gender = model.Gender.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                model.Address = model.Address.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.EmergencyContact))
            {
                model.EmergencyContact = model.EmergencyContact.Trim();
            }
        }
    }
}