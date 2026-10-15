using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Models;
using SmartGymManagement.Security;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    DatabaseConfiguration.Configure(options, builder.Configuration));

// Identity configuration
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 10;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<SignInManager<ApplicationUser>, ActiveUserSignInManager>();
builder.Services.AddScoped<ActiveAccountFilter>();
builder.Services.AddScoped<AdminAuditFilter>();
builder.Services.AddScoped<DatabaseConflictFilter>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddSingleton<BookingQrTokenService>();
builder.Services.AddScoped<MembershipMaintenanceService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddHttpClient<FitnessAssistantService>(client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/Identity/Account/Login") || path.StartsWithSegments("/Identity/Account/Register") ||
            path.StartsWithSegments("/AccountSetup"))
            return RateLimitPartition.GetFixedWindowLimiter("accounts:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
        return RateLimitPartition.GetNoLimiter("other");
    });
    options.AddPolicy("chatbot", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.GetUserAsync(context.Principal);
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var blockedProfile = user != null && await db.Members.AnyAsync(m => m.UserId == user.Id && m.Status != SmartGymManagement.Enums.MemberStatus.Active);
            if (user == null || !user.IsActive || blockedProfile)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        }
    };
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<ActiveAccountFilter>();
    options.Filters.AddService<AdminAuditFilter>();
    options.Filters.AddService<DatabaseConflictFilter>();
});
builder.Services.AddRazorPages();

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Reports}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedData:Enabled"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    await DbSeeder.SeedAsync(services);
}

app.Run();
