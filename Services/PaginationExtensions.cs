using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SmartGymManagement.Services;

public record PaginationInfo(int Page, int TotalPages, int TotalItems);

public static class PaginationExtensions
{
    public static async Task<List<T>> ToPagedListAsync<T>(this IQueryable<T> query, Controller controller)
    {
        const int pageSize = 20;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = int.TryParse(controller.Request.Query["page"], out var requested) ? Math.Clamp(requested, 1, pages) : 1;
        controller.ViewData["Pagination"] = new PaginationInfo(page, pages, total);
        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
}
