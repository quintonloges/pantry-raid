using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class RecipeTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string _adminToken = string.Empty;

  public RecipeTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
    // Configure admin credentials
    _client = _factory.WithWebHostBuilder(builder => {
      builder.ConfigureAppConfiguration((context, config) => {
        config.AddInMemoryCollection(new Dictionary<string, string?> {
          { "ADMIN_EMAIL", "admin@example.com" },
          { "ADMIN_PASSWORD", "Admin123!" }
        });
      });
    }).CreateClient();
  }

  private async Task AuthenticateAsAdminAsync() {
    if (!string.IsNullOrEmpty(_adminToken)) {
      return;
    }

    HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = "admin@example.com",
      Password = "Admin123!"
    });
    loginResponse.EnsureSuccessStatusCode();
    LoginResult? result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    _adminToken = result!.Token!;
  }

  [Fact]
  public async Task Admin_CanCreateSource_And_Recipe() {
    await AuthenticateAsAdminAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

    // 1. Create Source
    CreateRecipeSourceDto createSourceDto = new CreateRecipeSourceDto {
      Name = $"Test Source {Guid.NewGuid()}",
      BaseUrl = "https://example.com",
      IsActive = true
    };

    HttpResponseMessage sourceResponse = await _client.PostAsJsonAsync("/api/admin/recipe-sources", createSourceDto);
    Assert.Equal(HttpStatusCode.Created, sourceResponse.StatusCode);
    RecipeSourceDto? source = await sourceResponse.Content.ReadFromJsonAsync<RecipeSourceDto>();
    Assert.NotNull(source);

    // 2. Create Recipe
    CreateRecipeDto createRecipeDto = new CreateRecipeDto {
      Title = "Test Recipe",
      RecipeSourceId = source.Id,
      SourceUrl = $"https://example.com/recipe/{Guid.NewGuid()}",
      Ingredients = new List<CreateRecipeIngredientDto> {
        new CreateRecipeIngredientDto { OriginalText = "1 cup flour", Quantity = 1, Unit = "cup" }
      }
    };

    HttpResponseMessage recipeResponse = await _client.PostAsJsonAsync("/api/admin/recipes", createRecipeDto);
    Assert.Equal(HttpStatusCode.Created, recipeResponse.StatusCode);
    RecipeDto? recipe = await recipeResponse.Content.ReadFromJsonAsync<RecipeDto>();
    Assert.NotNull(recipe);
    Assert.Single(recipe.Ingredients);
    Assert.Equal("1 cup flour", recipe.Ingredients[0].OriginalText);

    // 3. Public List Sources
    HttpClient publicClient = _factory.CreateClient();
    HttpResponseMessage listResponse = await publicClient.GetAsync("/api/reference/sources");
    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    List<RecipeSourceDto>? sources = await listResponse.Content.ReadFromJsonAsync<List<RecipeSourceDto>>();
    Assert.Contains(sources!, s => s.Id == source.Id);
  }

  private class LoginResult {
      public string? Token { get; set; }
  }
}

