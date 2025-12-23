using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Loges.PantryRaid.Services;
using Loges.PantryRaid.Services.Interceptors;
using Loges.PantryRaid.Services.Scraping;
using Loges.PantryRaid.Services.Scraping.Impl;
using Loges.PantryRaid.WebAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditingInterceptor>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAdminIngredientService, AdminIngredientService>();
builder.Services.AddScoped<IIngredientGroupService, IngredientGroupService>();
builder.Services.AddScoped<IUserIngredientService, UserIngredientService>();
builder.Services.AddScoped<IRecipeSourceService, RecipeSourceService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.AddScoped<ISubstitutionService, SubstitutionService>();
builder.Services.AddScoped<ISubstitutionEvaluator, SubstitutionEvaluator>();
builder.Services.AddScoped<IUnmappedIngredientService, UnmappedIngredientService>();
builder.Services.AddScoped<IScraper, StubScraper>();
builder.Services.AddScoped<IScrapingService, ScrapingService>();

// Configure NSwag with JWT support
builder.Services.AddOpenApiDocument(config => {
  config.Title = "PantryRaid API";
  config.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme {
    Type = OpenApiSecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT", 
    Description = "Enter your valid JWT token."
  });

  config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

string? connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(connectionString)) {
  throw new InvalidOperationException("Connection string 'Default' not found.");
}

builder.Services.AddDbContext<AppDbContext>((sp, options) => {
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

  options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
});

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
  .AddEntityFrameworkStores<AppDbContext>()
  .AddDefaultTokenProviders();

builder.Services.AddTransient<IDbSeeder, DbSeeder>();

// Authentication & JWT
builder.Services.AddAuthentication(options => {
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
  options.SaveToken = true;
  options.RequireHttpsMetadata = false;
  options.TokenValidationParameters = new TokenValidationParameters() {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidAudience = builder.Configuration["Jwt:Audience"],
    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
  };
});

WebApplication app = builder.Build();

// Seed Data
using (IServiceScope scope = app.Services.CreateScope()) {
  IServiceProvider services = scope.ServiceProvider;
  try {
    AppDbContext context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();

    IDbSeeder seeder = services.GetRequiredService<IDbSeeder>();
    await seeder.SeedAsync();
  } catch (Exception ex) {
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
  }
}

if (app.Environment.IsDevelopment()) {
  // Add OpenAPI 3.0 document serving middleware
  // Available at: http://localhost:<port>/swagger/v1/swagger.json
  app.UseOpenApi();
  // Add web UIs to interact with the document
  // Available at: http://localhost:<port>/swagger
  app.UseSwaggerUi(); // UseSwaggerUI Protected by if (env.IsDevelopment())
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Keep this class for the WebApplicationFactory integration tests to work
public partial class Program { }
