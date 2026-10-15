using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartGymManagement.Models;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Security;

public sealed class ActiveUserSignInManager : SignInManager<ApplicationUser>
{
    private readonly ApplicationDbContext _db;
    public ActiveUserSignInManager(UserManager<ApplicationUser> users, IHttpContextAccessor accessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claims, IOptions<IdentityOptions> options,
        ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation, ApplicationDbContext db)
        : base(users, accessor, claims, options, logger, schemes, confirmation) { _db = db; }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
        => user.IsActive && !await _db.Members.AnyAsync(m => m.UserId == user.Id && m.Status != MemberStatus.Active)
            && await base.CanSignInAsync(user);
}
