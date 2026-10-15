using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Controllers;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountSetupController(UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public IActionResult Index(string userId, string token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return View(new AccountSetupViewModel { UserId = userId, Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AccountSetupViewModel model)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByIdAsync(model.UserId);
        if (user == null || !user.IsActive || await users.HasPasswordAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Liên kết không hợp lệ hoặc tài khoản đã có mật khẩu.");
            return View(model);
        }
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token)); }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Liên kết không hợp lệ.");
            return View(model);
        }
        var result = await users.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
