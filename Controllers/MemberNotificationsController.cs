using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Models;
using SmartGymManagement.Services;

namespace SmartGymManagement.Controllers;

[Authorize(Roles = "Member")]
public sealed class MemberNotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();
        var member = await db.Members.SingleOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return NotFound();
        return View(await db.Notifications.AsNoTracking().Where(n => n.MemberId == member.MemberId)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.NotificationId).ToPagedListAsync(this));
    }
}
