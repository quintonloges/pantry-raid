using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loges.PantryRaid.Dtos;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class SearchTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string _adminToken = string.Empty;

  public SearchTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
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

    HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new {
      Email = "admin@example.com",
      Password = "Admin123!"
    });
    loginResponse.EnsureSuccessStatusCode();
    LoginResult? result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    _adminToken = result!.Token!;
  }

  [Fact]
  public async Task Search_Returns_Correct_Shape() {
    // 1. Seed Data (Optional for this test since we just check shape, but good practice)
    await AuthenticateAsAdminAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

    // Create Source
    CreateRecipeSourceDto createSourceDto = new CreateRecipeSourceDto {
      Name = $"Search Test Source {Guid.NewGuid()}",
      BaseUrl = "https://example.com",
      IsActive = true
    };
    HttpResponseMessage sourceResponse = await _client.PostAsJsonAsync("/api/admin/recipe-sources", createSourceDto);
    RecipeSourceDto? source = await sourceResponse.Content.ReadFromJsonAsync<RecipeSourceDto>();

    // Create Recipe
    CreateRecipeDto createRecipeDto = new CreateRecipeDto {
      Title = "Search Test Recipe",
      RecipeSourceId = source!.Id,
      SourceUrl = $"https://example.com/recipe/{Guid.NewGuid()}",
      Ingredients = new List<CreateRecipeIngredientDto> {
        new() { OriginalText = "1 cup flour", Quantity = 1, Unit = "cup" }
      }
    };
    await _client.PostAsJsonAsync("/api/admin/recipes", createRecipeDto);

    // 2. Call Search
    HttpClient publicClient = _factory.CreateClient();
    SearchRequestDto searchRequest = new SearchRequestDto {
      IngredientIds = new List<int>(), // Empty for now
      Filters = new SearchFiltersDto(),
      AllowSubstitutions = false
    };

    HttpResponseMessage response = await publicClient.PostAsJsonAsync("/api/search", searchRequest);
    
    // 3. Assert Response Shape
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    SearchResponseDto? result = await response.Content.ReadFromJsonAsync<SearchResponseDto>();
    
    Assert.NotNull(result);
    Assert.NotNull(result.Results);
    
    // Spec says grouped by missing count 0-3
    // Even if empty, we expect the structure to allow for it.
    // My controller returns the groups explicitly.
    Assert.Equal(4, result.Results.Count);
    Assert.Contains(result.Results, g => g.MissingCount == 0);
    Assert.Contains(result.Results, g => g.MissingCount == 1);
    Assert.Contains(result.Results, g => g.MissingCount == 2);
    Assert.Contains(result.Results, g => g.MissingCount == 3);
  }

  private class LoginResult {
      public string? Token { get; set; }
  }
}

