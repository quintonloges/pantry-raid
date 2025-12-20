using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class ReferenceMetadataTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public ReferenceMetadataTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  private async Task<(HttpClient, string)> CreateAdminClientAsync() {
    string adminEmail = $"admin_{Guid.NewGuid()}@example.com";
    string adminPassword = "AdminPassword123!";

    HttpClient client = _factory.WithWebHostBuilder(builder => {
      builder.ConfigureAppConfiguration((context, config) => {
        config.AddInMemoryCollection(new Dictionary<string, string?> {
          { "ADMIN_EMAIL", adminEmail },
          { "ADMIN_PASSWORD", adminPassword }
        });
      });
    }).CreateClient();

    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = adminEmail,
      Password = adminPassword
    });
    loginResponse.EnsureSuccessStatusCode();
    LoginResult? result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
    return (client, result.Token!);
  }

  [Fact]
  public async Task ReferenceFlow_Lifecycle() {
    HttpClient client = _factory.CreateClient();
    (HttpClient adminClient, string _) = await CreateAdminClientAsync();

    // 1. Admin creates tags
    HttpResponseMessage cuisine = await adminClient.PostAsJsonAsync("/api/admin/reference/cuisines", new CreateReferenceDto { Name = "Italian" });
    cuisine.EnsureSuccessStatusCode();
    CuisineDto? cuisineDto = await cuisine.Content.ReadFromJsonAsync<CuisineDto>();

    HttpResponseMessage protein = await adminClient.PostAsJsonAsync("/api/admin/reference/proteins", new CreateReferenceDto { Name = "Chicken" });
    protein.EnsureSuccessStatusCode();
    ProteinDto? proteinDto = await protein.Content.ReadFromJsonAsync<ProteinDto>();

    HttpResponseMessage dietary = await adminClient.PostAsJsonAsync("/api/admin/reference/dietary-tags", new CreateReferenceDto { Name = "Gluten Free" });
    dietary.EnsureSuccessStatusCode();
    DietaryTagDto? dietaryDto = await dietary.Content.ReadFromJsonAsync<DietaryTagDto>();

    Assert.NotNull(cuisineDto);
    Assert.NotNull(proteinDto);
    Assert.NotNull(dietaryDto);

    // 2. Public lists tags
    List<CuisineDto>? cuisines = await client.GetFromJsonAsync<List<CuisineDto>>("/api/reference/cuisines");
    Assert.Contains(cuisines!, c => c.Id == cuisineDto.Id && c.Name == "Italian");

    List<ProteinDto>? proteins = await client.GetFromJsonAsync<List<ProteinDto>>("/api/reference/proteins");
    Assert.Contains(proteins!, p => p.Id == proteinDto.Id && p.Name == "Chicken");

    List<DietaryTagDto>? dietaries = await client.GetFromJsonAsync<List<DietaryTagDto>>("/api/reference/dietary-tags");
    Assert.Contains(dietaries!, d => d.Id == dietaryDto.Id && d.Name == "Gluten Free");

    // 3. Admin creates a recipe
    // We need a recipe source first or just use existing.
    // Let's seed a source if needed, or assume one exists. 
    // Best to create one via service or seed.
    int sourceId;
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      RecipeSource source = new RecipeSource { Name = "Test Source " + Guid.NewGuid(), BaseUrl = "http://test.com" };
      db.RecipeSources.Add(source);
      await db.SaveChangesAsync();
      sourceId = source.Id;
    }

    CreateRecipeDto createRecipeDto = new CreateRecipeDto {
      Title = "Chicken Parm",
      RecipeSourceId = sourceId,
      SourceUrl = "http://test.com/chicken-parm",
      Ingredients = new List<CreateRecipeIngredientDto>()
    };

    HttpResponseMessage recipeResponse = await adminClient.PostAsJsonAsync("/api/admin/recipes", createRecipeDto);
    recipeResponse.EnsureSuccessStatusCode();
    RecipeDto? recipeDto = await recipeResponse.Content.ReadFromJsonAsync<RecipeDto>();
    Assert.NotNull(recipeDto);

    // 4. Admin assigns tags
    SetRecipeTagsDto setTagsDto = new SetRecipeTagsDto {
      CuisineIds = new List<int> { cuisineDto.Id },
      ProteinIds = new List<int> { proteinDto.Id },
      DietaryTagIds = new List<int> { dietaryDto.Id }
    };

    HttpResponseMessage setTagsResponse = await adminClient.PutAsJsonAsync($"/api/admin/recipes/{recipeDto.Id}/tags", setTagsDto);
    setTagsResponse.EnsureSuccessStatusCode();

    // 5. Verify tags are assigned (by fetching recipe again? No endpoint for getting single recipe public/admin yet?
    // Wait, I didn't verify if there is GET /api/recipes/{id}.
    // I added GetByIdAsync to service, but not sure if exposed in controller.
    // AdminRecipeController usually returns DTO on create.
    // Let's check if I can fetch it.
    // If not, I can verify via database scope.
    
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      Recipe? savedRecipe = await db.Recipes
        .Include(r => r.RecipeCuisines)
        .Include(r => r.RecipeProteins)
        .Include(r => r.RecipeDietaryTags)
        .FirstOrDefaultAsync(r => r.Id == recipeDto.Id);
        
      Assert.NotNull(savedRecipe);
      Assert.Single(savedRecipe.RecipeCuisines);
      Assert.Equal(cuisineDto.Id, savedRecipe.RecipeCuisines.First().CuisineId);
      
      Assert.Single(savedRecipe.RecipeProteins);
      Assert.Equal(proteinDto.Id, savedRecipe.RecipeProteins.First().ProteinId);
      
      Assert.Single(savedRecipe.RecipeDietaryTags);
      Assert.Equal(dietaryDto.Id, savedRecipe.RecipeDietaryTags.First().DietaryTagId);
    }
  }
}

