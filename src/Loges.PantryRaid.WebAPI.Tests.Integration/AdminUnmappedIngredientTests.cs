using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class AdminUnmappedIngredientTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string? _adminToken;
  private readonly (string Email, string Password) _adminCredentials;

  public AdminUnmappedIngredientTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;

    string adminEmail = $"admin_{Guid.NewGuid()}@example.com";
    string adminPassword = "AdminPassword123!";

    _client = _factory.WithWebHostBuilder(builder => {
      builder.ConfigureAppConfiguration((context, config) => {
        config.AddInMemoryCollection(new Dictionary<string, string?> {
          { "ADMIN_EMAIL", adminEmail },
          { "ADMIN_PASSWORD", adminPassword }
        });
      });
    }).CreateClient();

    _adminCredentials = (adminEmail, adminPassword);
  }

  private async Task<string> GetAdminTokenAsync() {
    if (_adminToken != null) {
      return _adminToken;
    }

    HttpResponseMessage loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = _adminCredentials.Email,
      Password = _adminCredentials.Password
    });
    loginRes.EnsureSuccessStatusCode();
    LoginResult? result = await loginRes.Content.ReadFromJsonAsync<LoginResult>();
    _adminToken = result!.Token!;
    return _adminToken;
  }

  private async Task<string> GetUserTokenAsync() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });
    HttpResponseMessage res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
    LoginResult? result = await res.Content.ReadFromJsonAsync<LoginResult>();
    return result!.Token!;
  }

  private async Task<(int ItemId, int IngredientId)> SeedUnmappedItemAndIngredientAsync() {
    using IServiceScope scope = _factory.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Seed Source
    RecipeSource source = new RecipeSource { Name = "Test Source " + Guid.NewGuid(), BaseUrl = "http://test.com", ScraperKey = "test", IsActive = true };
    db.RecipeSources.Add(source);
    
    // Seed Recipe
    Recipe recipe = new Recipe { Title = "Test Recipe", SourceUrl = "http://test.com/recipe", RecipeSource = source };
    db.Recipes.Add(recipe);

    // Seed Ingredient
    Ingredient ingredient = new Ingredient { Name = "Real Ingredient " + Guid.NewGuid(), Slug = "real-" + Guid.NewGuid() };
    db.Ingredients.Add(ingredient);

    // Seed Unmapped Item
    UnmappedIngredient item = new UnmappedIngredient {
      Recipe = recipe,
      RecipeSource = source,
      OriginalText = "Unknown Item " + Guid.NewGuid(),
      Status = UnmappedIngredientStatus.New
    };
    db.UnmappedIngredients.Add(item);

    await db.SaveChangesAsync();
    return (item.Id, ingredient.Id);
  }

  [Fact]
  public async Task GetUnmappedIngredients_AsAdmin_ReturnsList() {
    await SeedUnmappedItemAndIngredientAsync();
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage response = await _client.GetAsync("/api/admin/unmapped-ingredients");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    List<UnmappedIngredientDto>? list = await response.Content.ReadFromJsonAsync<List<UnmappedIngredientDto>>();
    Assert.NotNull(list);
    Assert.NotEmpty(list);
  }

  [Fact]
  public async Task ResolveUnmappedIngredient_AsAdmin_Success() {
    var (itemId, ingredientId) = await SeedUnmappedItemAndIngredientAsync();
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/admin/unmapped-ingredients/{itemId}/resolve", new ResolveUnmappedIngredientRequest {
      ResolvedIngredientId = ingredientId
    });
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify status
    using IServiceScope scope = _factory.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    UnmappedIngredient? item = await db.UnmappedIngredients.FindAsync(itemId);
    Assert.Equal(UnmappedIngredientStatus.Resolved, item!.Status);
    Assert.Equal(ingredientId, item.ResolvedIngredientId);
  }

  [Fact]
  public async Task SuggestUnmappedIngredient_AsAdmin_Success() {
    var (itemId, ingredientId) = await SeedUnmappedItemAndIngredientAsync();
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/admin/unmapped-ingredients/{itemId}/suggest", new SuggestUnmappedIngredientRequest {
      SuggestedIngredientId = ingredientId
    });
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify status
    using IServiceScope scope = _factory.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    UnmappedIngredient? item = await db.UnmappedIngredients.FindAsync(itemId);
    Assert.Equal(UnmappedIngredientStatus.Suggested, item!.Status);
    Assert.Equal(ingredientId, item.SuggestedIngredientId);
  }

  [Fact]
  public async Task Endpoints_AsUser_Forbidden() {
    string token = await GetUserTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage getRes = await client.GetAsync("/api/admin/unmapped-ingredients");
    Assert.Equal(HttpStatusCode.Forbidden, getRes.StatusCode);

    HttpResponseMessage putRes = await client.PutAsJsonAsync("/api/admin/unmapped-ingredients/1/resolve", new ResolveUnmappedIngredientRequest { ResolvedIngredientId = 1 });
    Assert.Equal(HttpStatusCode.Forbidden, putRes.StatusCode);
  }
}
