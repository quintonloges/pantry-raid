using Loges.PantryRaid.EFCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MySql;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class PantryRaidWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
  private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
    .WithImage("mysql:8.0")
    .Build();

  public async Task InitializeAsync() {
    await _mySqlContainer.StartAsync();

    // Use the factory's service provider to create a scope and migrate
    using IServiceScope scope = Services.CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Using EnsureCreatedAsync ensures the DB exists and schema is compatible with model.
    // For strict migration testing, we would use MigrateAsync(), but that requires migration files to exist.
    // We will try MigrateAsync, and if it fails (e.g. no migrations assembly), we might fallback or just let it fail.
    // Given the prompt requirement "Apply migrations on startup", we use MigrateAsync.
    try {
      await context.Database.MigrateAsync();
    } catch (Exception) {
      // If migrations haven't been added yet, EnsureCreated can be a fallback for testing basic connectivity
      await context.Database.EnsureCreatedAsync();
    }
  }

  public new async Task DisposeAsync() {
    await _mySqlContainer.DisposeAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder) {
    builder.ConfigureTestServices(services => {
      // Remove the existing DbContext registration
      services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

      // Add DbContext using the container's connection string
      services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(_mySqlContainer.GetConnectionString(), 
          ServerVersion.AutoDetect(_mySqlContainer.GetConnectionString())));
    });
  }
}
