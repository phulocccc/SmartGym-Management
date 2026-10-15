using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartGymManagement.Data;

// EF tooling can construct the model without running Program, seeding or opening a connection.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Use the same default configuration providers as Program, including
        // environment-specific JSON, development User Secrets, environment
        // variables and forwarded command-line arguments. Do not build/run the app.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ApplicationDbContext).Assembly.GetName().Name
        });
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseConfiguration.Configure(options, builder.Configuration);
        return new ApplicationDbContext(options.Options);
    }
}
