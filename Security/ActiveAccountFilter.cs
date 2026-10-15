using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Security;

public sealed class ActiveAccountFilter(UserManager<ApplicationUser> users, ApplicationDbContext db)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(context.HttpContext.User);
            var blockedMember = user != null && context.HttpContext.User.IsInRole("Member") &&
                await db.Members.AnyAsync(m => m.UserId == user.Id && m.Status != MemberStatus.Active);
            if (user == null || !user.IsActive || blockedMember)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        await next();
    }
}
