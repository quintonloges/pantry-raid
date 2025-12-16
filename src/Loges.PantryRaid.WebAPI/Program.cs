using Loges.PantryRaid.EFCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApiDocument();

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(connectionString)) {
  throw new InvalidOperationException("Connection string 'Default' not found.");
}

builder.Services.AddDbContext<AppDbContext>(options => {
  // Check if we're in design-time mode (e.g. running 'dotnet ef migrations')
  // In design-time, we might not have a running DB, so we can't auto-detect server version.
  // We'll fallback to a specific version (e.g. 8.0.36) to allow migrations to generate.
  // Or we just try/catch the auto-detect.
  try {
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
  }
  catch (Exception) {
    // Fallback for design-time or if DB is offline.
    // We'll use a default version (e.g. 8.0.36) to allow the context to be built.
    // This handles MySqlConnector.MySqlException, SocketException, etc.
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.36-mysql"));
  }
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  // Add OpenAPI 3.0 document serving middleware
  // Available at: http://localhost:<port>/swagger/v1/swagger.json
  app.UseOpenApi();
  // Add web UIs to interact with the document
  // Available at: http://localhost:<port>/swagger
  app.UseSwaggerUi(); // UseSwaggerUI Protected by if (env.IsDevelopment())
}

app.MapControllers();

app.Run();

// Keep this class for the WebApplicationFactory integration tests to work
public partial class Program { }
