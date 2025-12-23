using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class ScrapingTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public ScrapingTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  private class LoginResultDto {
    public string? Token { get; set; }
  }

  private async Task<string> GetAdminTokenAsync(HttpClient client) {
    string email = $"admin_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register
    HttpResponseMessage registerRes = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });
    registerRes.EnsureSuccessStatusCode();

    // 2. Assign Admin Role directly in DB
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
      AppUser? user = await userManager.FindByEmailAsync(email);
      if (user != null) {
        if (!await userManager.IsInRoleAsync(user, "Admin")) {
           await userManager.AddToRoleAsync(user, "Admin");
        }
      }
    }

    // 3. Login
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    loginResponse.EnsureSuccessStatusCode();
    
    LoginResultDto? result = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
    return result!.Token!;
  }

  private async Task<string> GetUserTokenAsync(HttpClient client) {
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register
    HttpResponseMessage registerRes = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });
    registerRes.EnsureSuccessStatusCode();

    // 2. Login
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    loginResponse.EnsureSuccessStatusCode();
    
    LoginResultDto? result = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
    return result!.Token!;
  }

  [Fact]
  public async Task Scrape_NonAdminUser_ReturnsForbidden() {
    // Arrange
    HttpClient client = _factory.CreateClient();
    string token = await GetUserTokenAsync(client);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    ScrapeRequestDto request = new ScrapeRequestDto { Url = "https://example-recipe-site.com/recipes/forbidden" };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/scrape", request);

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Scrape_Anonymous_ReturnsUnauthorized() {
    // Arrange
    HttpClient client = _factory.CreateClient();
    // No Authorization header

    ScrapeRequestDto request = new ScrapeRequestDto { Url = "https://example-recipe-site.com/recipes/unauthorized" };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/scrape", request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Scrape_NewRecipe_CreatesRecipeAndUnmappedIngredients() {
    // Arrange
    HttpClient client = _factory.CreateClient();
    string token = await GetAdminTokenAsync(client);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    string domain = "example-recipe-site.com";
    string url = $"https://{domain}/recipes/{Guid.NewGuid()}";

    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      // Ensure source exists
      if (!await context.RecipeSources.AnyAsync(s => s.BaseUrl == domain)) {
        context.RecipeSources.Add(new RecipeSource {
          Name = "Example Site",
          BaseUrl = domain,
          IsActive = true
        });
        await context.SaveChangesAsync();
      }
    }

    ScrapeRequestDto request = new ScrapeRequestDto { Url = url };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/scrape", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    ScrapeResultDto? result = await response.Content.ReadFromJsonAsync<ScrapeResultDto>();
    Assert.NotNull(result);
    Assert.Equal(ScrapeResultStatus.Success, result.Status);
    Assert.NotNull(result.RecipeId);

    // Verify DB
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      Recipe? recipe = await context.Recipes
        .Include(r => r.Ingredients)
        .FirstOrDefaultAsync(r => r.Id == result.RecipeId);
      
      Assert.NotNull(recipe);
      Assert.Equal("Stub Scraped Recipe", recipe.Title);
      Assert.Equal(3, recipe.Ingredients.Count);

      List<UnmappedIngredient> unmapped = await context.UnmappedIngredients
        .Where(u => u.RecipeId == recipe.Id)
        .ToListAsync();
      
      Assert.Equal(3, unmapped.Count);
    }
  }

  [Fact]
  public async Task Scrape_ExistingRecipe_ReturnsSkipped() {
    // Arrange
    HttpClient client = _factory.CreateClient();
    string token = await GetAdminTokenAsync(client);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    string domain = "example-recipe-site.com";
    string url = $"https://{domain}/recipes/existing-{Guid.NewGuid()}";

    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      // Ensure source exists
      RecipeSource? source = await context.RecipeSources.FirstOrDefaultAsync(s => s.BaseUrl == domain);
      if (source == null) {
        source = new RecipeSource {
          Name = "Example Site",
          BaseUrl = domain,
          IsActive = true
        };
        context.RecipeSources.Add(source);
        await context.SaveChangesAsync();
      }
      
      // Create existing recipe
      Recipe recipe = new Recipe {
        Title = "Existing Recipe",
        SourceUrl = url,
        RecipeSourceId = source.Id,
        SourceRecipeId = "existing-1",
        ShortDescription = "Desc",
        ScrapeStatus = "Manual"
      };
      context.Recipes.Add(recipe);
      await context.SaveChangesAsync();
    }

    ScrapeRequestDto request = new ScrapeRequestDto { Url = url };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/scrape", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    ScrapeResultDto? result = await response.Content.ReadFromJsonAsync<ScrapeResultDto>();
    Assert.NotNull(result);
    Assert.Equal(ScrapeResultStatus.Skipped, result.Status);
  }
}
