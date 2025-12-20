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
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
  }

  private async Task<int> CreateIngredientAsync(string name) {
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto {
      Name = name,
      Category = "Test",
      Notes = "Test notes"
    });
    response.EnsureSuccessStatusCode();
    IngredientDto? ing = await response.Content.ReadFromJsonAsync<IngredientDto>();
    return ing!.Id;
  }

  private async Task<int> CreateSourceAsync() {
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/recipe-sources", new CreateRecipeSourceDto {
      Name = $"Test Source {Guid.NewGuid()}",
      BaseUrl = "https://example.com",
      IsActive = true
    });
    response.EnsureSuccessStatusCode();
    RecipeSourceDto? source = await response.Content.ReadFromJsonAsync<RecipeSourceDto>();
    return source!.Id;
  }

  private async Task CreateRecipeAsync(int sourceId, string title, List<(int id, bool optional)> ingredients) {
    CreateRecipeDto createDto = new CreateRecipeDto {
      Title = title,
      RecipeSourceId = sourceId,
      SourceUrl = $"https://example.com/{Guid.NewGuid()}",
      Ingredients = ingredients.Select(i => new CreateRecipeIngredientDto {
        IngredientId = i.id,
        IsOptional = i.optional,
        Quantity = 1,
        Unit = "unit",
        OriginalText = "some text"
      }).ToList()
    };
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/recipes", createDto);
    response.EnsureSuccessStatusCode();
  }

  [Fact]
  public async Task Search_GroupLogic_Works_Correctly() {
    await AuthenticateAsAdminAsync();

    // 1. Setup Data
    int flourId = await CreateIngredientAsync($"Flour_{Guid.NewGuid()}");
    int eggId = await CreateIngredientAsync($"Egg_{Guid.NewGuid()}");
    int milkId = await CreateIngredientAsync($"Milk_{Guid.NewGuid()}");
    int saltId = await CreateIngredientAsync($"Salt_{Guid.NewGuid()}");
    int sugarId = await CreateIngredientAsync($"Sugar_{Guid.NewGuid()}");
    
    int sourceId = await CreateSourceAsync();

    // Pancake: Flour, Egg, Milk (Required)
    await CreateRecipeAsync(sourceId, "Pancakes", new List<(int, bool)> { 
      (flourId, false), (eggId, false), (milkId, false) 
    });

    // Omelette: Egg, Salt (Required), Milk (Optional)
    await CreateRecipeAsync(sourceId, "Omelette", new List<(int, bool)> {
      (eggId, false), (saltId, false), (milkId, true)
    });

    // Cake: Flour, Egg, Milk, Sugar (Required) - 4 ingredients
    await CreateRecipeAsync(sourceId, "Cake", new List<(int, bool)> {
      (flourId, false), (eggId, false), (milkId, false), (sugarId, false)
    });

    HttpClient publicClient = _factory.CreateClient();

    // Case 1: User has {Flour, Egg, Milk}
    // Pancakes: 0 missing (Group 0)
    // Omelette: Missing Salt (Group 1)
    // Cake: Missing Sugar (Group 1)
    SearchRequestDto request1 = new SearchRequestDto {
      IngredientIds = new List<int> { flourId, eggId, milkId }
    };
    SearchResponseDto? result1 = await (await publicClient.PostAsJsonAsync("/api/search", request1)).Content.ReadFromJsonAsync<SearchResponseDto>();
    
    RecipeGroupDto group0_1 = result1!.Results.First(g => g.MissingCount == 0);
    Assert.Contains(group0_1.Recipes, r => r.Recipe.Title == "Pancakes");
    Assert.DoesNotContain(group0_1.Recipes, r => r.Recipe.Title == "Omelette");

    RecipeGroupDto group1_1 = result1.Results.First(g => g.MissingCount == 1);
    Assert.Contains(group1_1.Recipes, r => r.Recipe.Title == "Omelette"); // Missing Salt
    Assert.Contains(group1_1.Recipes, r => r.Recipe.Title == "Cake"); // Missing Sugar

    // Case 2: User has {Egg}
    // Pancakes: Missing Flour, Milk (Group 2)
    // Omelette: Missing Salt (Group 1) - Milk is optional!
    // Cake: Missing Flour, Milk, Sugar (Group 3)
    SearchRequestDto request2 = new SearchRequestDto {
      IngredientIds = new List<int> { eggId }
    };
    SearchResponseDto? result2 = await (await publicClient.PostAsJsonAsync("/api/search", request2)).Content.ReadFromJsonAsync<SearchResponseDto>();

    Assert.Contains(result2!.Results.First(g => g.MissingCount == 2).Recipes, r => r.Recipe.Title == "Pancakes");
    Assert.Contains(result2.Results.First(g => g.MissingCount == 1).Recipes, r => r.Recipe.Title == "Omelette");
    Assert.Contains(result2.Results.First(g => g.MissingCount == 3).Recipes, r => r.Recipe.Title == "Cake");

    // Case 3: User has nothing
    // Pancakes: Missing 3 (Group 3)
    // Omelette: Missing 2 (Egg, Salt) (Group 2)
    // Cake: Missing 4 (Flour, Egg, Milk, Sugar) -> Should be EXCLUDED (>3)
    SearchRequestDto request3 = new SearchRequestDto { IngredientIds = new List<int>() };
    SearchResponseDto? result3 = await (await publicClient.PostAsJsonAsync("/api/search", request3)).Content.ReadFromJsonAsync<SearchResponseDto>();

    Assert.Contains(result3!.Results.First(g => g.MissingCount == 3).Recipes, r => r.Recipe.Title == "Pancakes");
    Assert.Contains(result3.Results.First(g => g.MissingCount == 2).Recipes, r => r.Recipe.Title == "Omelette");
    
    // Check exclusion of Cake
    List<SearchResultRecipeDto> allRecipes3 = result3.Results.SelectMany(g => g.Recipes).ToList();
    Assert.DoesNotContain(allRecipes3, r => r.Recipe.Title == "Cake");

    // Case 4: Filter "Must include Egg"
    // With {Egg, Flour, Milk}
    // Should return Pancakes (has Egg), Omelette (has Egg), Cake (has Egg).
    // If we create "Bread" (Flour, Water) - no Egg - it should be filtered out.
    int waterId = await CreateIngredientAsync($"Water_{Guid.NewGuid()}");
    await CreateRecipeAsync(sourceId, "Bread", new List<(int, bool)> { (flourId, false), (waterId, false) });

    SearchRequestDto request4 = new SearchRequestDto {
      IngredientIds = new List<int> { flourId, eggId, milkId, waterId },
      Filters = new SearchFiltersDto {
        MustIncludeIngredientIds = new List<int> { eggId }
      }
    };
    SearchResponseDto? result4 = await (await publicClient.PostAsJsonAsync("/api/search", request4)).Content.ReadFromJsonAsync<SearchResponseDto>();
    
    List<SearchResultRecipeDto> allRecipes4 = result4!.Results.SelectMany(g => g.Recipes).ToList();
    Assert.Contains(allRecipes4, r => r.Recipe.Title == "Pancakes");
    Assert.Contains(allRecipes4, r => r.Recipe.Title == "Omelette");
    Assert.DoesNotContain(allRecipes4, r => r.Recipe.Title == "Bread");
  }

  private class LoginResult {
      public string? Token { get; set; }
  }
}
