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

  [Fact]
  public async Task NormalUser_CannotCreate_Source_Or_Recipe() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register & Login
    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    LoginResult? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    string token = loginResult!.Token!;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // 2. Try Create Source
    CreateRecipeSourceDto createSourceDto = new CreateRecipeSourceDto {
      Name = "Hacker Source",
      BaseUrl = "https://hacker.com",
      IsActive = true
    };
    HttpResponseMessage sourceResponse = await client.PostAsJsonAsync("/api/admin/recipe-sources", createSourceDto);
    Assert.Equal(HttpStatusCode.Forbidden, sourceResponse.StatusCode);

    // 3. Try Create Recipe
    CreateRecipeDto createRecipeDto = new CreateRecipeDto {
      Title = "Hacker Recipe",
      RecipeSourceId = 1,
      SourceUrl = "https://hacker.com/recipe",
      Ingredients = new List<CreateRecipeIngredientDto>()
    };
    HttpResponseMessage recipeResponse = await client.PostAsJsonAsync("/api/admin/recipes", createRecipeDto);
    Assert.Equal(HttpStatusCode.Forbidden, recipeResponse.StatusCode);
  }

  private class LoginResult {
      public string? Token { get; set; }
  }
}
