using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InventoryAlert.Api.Infrastructure.Persistence;

public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration.Get<AppSettings>();
        var connectionString = settings?.Database?.DefaultConnection;
        
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("InventoryAlert.Api"));

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
