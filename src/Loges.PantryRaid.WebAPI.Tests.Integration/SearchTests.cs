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

  private async Task<int> CreateSubstitutionGroupAsync(int targetIngredientId) {
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/substitutions/groups", new CreateSubstitutionGroupRequest {
      TargetIngredientId = targetIngredientId
    });
    response.EnsureSuccessStatusCode();
    SubstitutionGroupDto? group = await response.Content.ReadFromJsonAsync<SubstitutionGroupDto>();
    return group!.Id;
  }

  private async Task CreateSubstitutionOptionAsync(int groupId, List<int> ingredientIds, string note = "") {
    HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/admin/substitutions/groups/{groupId}/options", new CreateSubstitutionOptionRequest {
      SubstitutionGroupId = groupId,
      Note = note,
      IngredientIds = ingredientIds
    });
    response.EnsureSuccessStatusCode();
  }

  [Fact]
  public async Task Search_Substitutions_Works_Correctly() {
    await AuthenticateAsAdminAsync();

    // 1. Setup Data
    int flourId = await CreateIngredientAsync($"Flour_{Guid.NewGuid()}");
    int eggId = await CreateIngredientAsync($"Egg_{Guid.NewGuid()}");
    int milkId = await CreateIngredientAsync($"Milk_{Guid.NewGuid()}");
    int buttermilkId = await CreateIngredientAsync($"Buttermilk_{Guid.NewGuid()}");
    int vinegarId = await CreateIngredientAsync($"Vinegar_{Guid.NewGuid()}");
    
    int sourceId = await CreateSourceAsync();

    // Recipe: Pancakes with Buttermilk (Required)
    // Ingredients: Flour, Egg, Buttermilk
    await CreateRecipeAsync(sourceId, "Buttermilk Pancakes", new List<(int, bool)> { 
      (flourId, false), (eggId, false), (buttermilkId, false) 
    });

    // Substitution Rule: Buttermilk -> Milk + Vinegar
    int groupId = await CreateSubstitutionGroupAsync(buttermilkId);
    await CreateSubstitutionOptionAsync(groupId, new List<int> { milkId, vinegarId }, "Homemade");

    HttpClient publicClient = _factory.CreateClient();

    // User has: Flour, Egg, Milk, Vinegar. Missing Buttermilk.
    // Without substitutions: Missing 1 (Buttermilk) -> Group 1.
    // With substitutions: Missing 0 (Satisfied by Milk+Vinegar) -> Group 0.

    List<int> userIngredients = new List<int> { flourId, eggId, milkId, vinegarId };

    // 1. Without Substitutions
    SearchRequestDto requestNoSub = new SearchRequestDto {
      IngredientIds = userIngredients,
      AllowSubstitutions = false
    };
    SearchResponseDto? resultNoSub = await (await publicClient.PostAsJsonAsync("/api/search", requestNoSub)).Content.ReadFromJsonAsync<SearchResponseDto>();

    RecipeGroupDto group1_NoSub = resultNoSub!.Results.First(g => g.MissingCount == 1);
    Assert.Contains(group1_NoSub.Recipes, r => r.Recipe.Title == "Buttermilk Pancakes");
    
    // Verify it's NOT in group 0
    RecipeGroupDto group0_NoSub = resultNoSub.Results.First(g => g.MissingCount == 0);
    Assert.DoesNotContain(group0_NoSub.Recipes, r => r.Recipe.Title == "Buttermilk Pancakes");

    // 2. With Substitutions
    SearchRequestDto requestSub = new SearchRequestDto {
      IngredientIds = userIngredients,
      AllowSubstitutions = true
    };
    SearchResponseDto? resultSub = await (await publicClient.PostAsJsonAsync("/api/search", requestSub)).Content.ReadFromJsonAsync<SearchResponseDto>();

    RecipeGroupDto group0_Sub = resultSub!.Results.First(g => g.MissingCount == 0);
    Assert.Contains(group0_Sub.Recipes, r => r.Recipe.Title == "Buttermilk Pancakes");
    
    SearchResultRecipeDto? recipe = group0_Sub.Recipes.First(r => r.Recipe.Title == "Buttermilk Pancakes");
    Assert.NotEmpty(recipe.SubstitutionNotes);
    Assert.Contains("Substitute", recipe.SubstitutionNotes.First());
    // Note: The note text format depends on ingredient names.
    // We didn't capture the exact names for Milk/Vinegar in variables but we know them.
    // But "Homemade" note should be there.
    Assert.Contains("Homemade", recipe.SubstitutionNotes.First());
  }

  [Fact]
  public async Task Search_SubstitutionCycles_DoNotCrash() {
    await AuthenticateAsAdminAsync();

    // 1. Setup Data
    int ingAId = await CreateIngredientAsync($"A_{Guid.NewGuid()}");
    int ingBId = await CreateIngredientAsync($"B_{Guid.NewGuid()}");
    int sourceId = await CreateSourceAsync();

    // Recipe needs A
    await CreateRecipeAsync(sourceId, "Cycle Recipe", new List<(int, bool)> { (ingAId, false) });

    // Rules: A -> B, B -> A
    int groupA = await CreateSubstitutionGroupAsync(ingAId);
    await CreateSubstitutionOptionAsync(groupA, new List<int> { ingBId }, "Use B");

    int groupB = await CreateSubstitutionGroupAsync(ingBId);
    await CreateSubstitutionOptionAsync(groupB, new List<int> { ingAId }, "Use A");

    HttpClient publicClient = _factory.CreateClient();

    // User has nothing. 
    // Logic: Need A. Try sub A -> B. Need B. Try sub B -> A. Need A. Cycle detected.
    // Should fail gracefully and return missing A.

    SearchRequestDto request = new SearchRequestDto {
      IngredientIds = new List<int>(), // Empty
      AllowSubstitutions = true
    };

    // The test passes if this call completes without timeout/stack overflow
    SearchResponseDto? result = await (await publicClient.PostAsJsonAsync("/api/search", request)).Content.ReadFromJsonAsync<SearchResponseDto>();

    Assert.NotNull(result);
    // Recipe has 1 ingredient, missing. So missing count 1.
    RecipeGroupDto group1 = result.Results.First(g => g.MissingCount == 1);
    Assert.Contains(group1.Recipes, r => r.Recipe.Title == "Cycle Recipe");
  }

  private class LoginResult {
    public string? Token { get; set; }
  }
}
