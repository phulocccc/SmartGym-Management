using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SmartGymManagement.Security;

public sealed class AdminAuditFilter(ILogger<AdminAuditFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (context.HttpContext.User.IsInRole("Admin") && HttpMethods.IsPost(context.HttpContext.Request.Method) &&
            result.Exception == null && result.Result is not ViewResult && result.HttpContext.Response.StatusCode < 400 &&
            result.Result is not (BadRequestObjectResult or NotFoundResult or NotFoundObjectResult or ForbidResult))
            logger.LogInformation("AdminAudit Actor={Actor} Controller={Controller} Action={Action} Record={Record}",
                context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), context.RouteData.Values["controller"],
                context.RouteData.Values["action"], context.RouteData.Values["id"]);
    }
}
