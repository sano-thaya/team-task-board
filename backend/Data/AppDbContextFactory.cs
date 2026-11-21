
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TeamTaskBoard.Api.Data;

public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load the connection string from User Secrets.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AppDbContext>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing."
            );

        var options = new DbContextOptionsBuilder<AppDbContext>();

        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 4, 13))
        );

        return new AppDbContext(options.Options);
    }
}
