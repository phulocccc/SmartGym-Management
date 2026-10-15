using Microsoft.EntityFrameworkCore;

namespace SmartGymManagement.Data;

public static class DatabaseConfiguration
{
    public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

        options.UseSqlServer(connectionString);
    }
}
