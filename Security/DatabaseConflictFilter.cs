using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace SmartGymManagement.Security;

public sealed class DatabaseConflictFilter(ILogger<DatabaseConflictFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception.GetBaseException() is SqlException sql && sql.Number is 2601 or 2627 or 1205 or 51000)
        {
            logger.LogWarning("Database operation conflicted (SQL code {Code}).", sql.Number);
            context.Result = new ObjectResult("Yêu cầu trùng hoặc dữ liệu vừa thay đổi. Vui lòng tải lại trang và thử lại.")
            { StatusCode = StatusCodes.Status409Conflict };
            context.ExceptionHandled = true;
        }
    }
}
