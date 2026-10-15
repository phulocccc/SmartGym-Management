using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.Areas.Identity.Pages.Account;

public sealed class RegisterModel(ApplicationDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles, IConfiguration configuration) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(100), RegularExpression(@"^[A-Za-z0-9._%+-]+@smartgym\.com$", ErrorMessage = "Email hội viên phải thuộc @smartgym.com.")] public string Email { get; set; } = string.Empty;
        [Required, MinLength(10), StringLength(128), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => configuration.GetValue<bool>("Registration:Enabled") ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!configuration.GetValue<bool>("Registration:Enabled")) return NotFound();
        if (!ModelState.IsValid) return Page();
        var email = Input.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản với email này.");
            return Page();
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await roles.RoleExistsAsync("Member"))
        {
            var createdRole = await roles.CreateAsync(new IdentityRole("Member"));
            if (!createdRole.Succeeded) { ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản lúc này."); return Page(); }
        }
        var user = new ApplicationUser { UserName = email, Email = email, FullName = Input.FullName.Trim(), IsActive = true };
        var result = await users.CreateAsync(user, Input.Password);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, "Member");
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }
        db.Members.Add(new Member { UserId = user.Id, Status = MemberStatus.Active });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToPage("Login");
    }
}
