using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class ReferenceTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public ReferenceTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  [Fact]
  public async Task GetIngredients_ReturnsSortedList() {
    HttpClient client = _factory.CreateClient();
    
    // Seed data
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      context.Ingredients.RemoveRange(context.Ingredients); // Clear existing
      
      context.Ingredients.AddRange(
        new Ingredient { Name = "Banana", Slug = "banana", Aliases = new() },
        new Ingredient { Name = "Apple", Slug = "apple", Aliases = new() },
        new Ingredient { Name = "Carrot", Slug = "carrot", Aliases = new() }
      );
      await context.SaveChangesAsync();
    }

    // Act
    List<IngredientDto>? response = await client.GetFromJsonAsync<List<IngredientDto>>("/api/reference/ingredients");

    // Assert
    Assert.NotNull(response);
    Assert.True(response.Count >= 3);
    
    // Check if our seeded items are present and sorted relative to each other
    // Note: Other tests might run in parallel or pre-seeded data might exist if cleanup isn't perfect,
    // so we filter to our known set for sorting check.
    List<IngredientDto> relevantItems = response.Where(i => new[] { "Apple", "Banana", "Carrot" }.Contains(i.Name)).ToList();
    
    Assert.Equal("Apple", relevantItems[0].Name);
    Assert.Equal("Banana", relevantItems[1].Name);
    Assert.Equal("Carrot", relevantItems[2].Name);
  }

  [Fact]
  public async Task GetIngredients_WithQuery_ReturnsFilteredList() {
    HttpClient client = _factory.CreateClient();

    // Seed data
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      // We don't clear here to allow parallel execution if needed, just add unique items
      
      if (!await context.Ingredients.AnyAsync(i => i.Slug == "chicken-breast")) {
        context.Ingredients.AddRange(
          new Ingredient { Name = "Chicken Breast", Slug = "chicken-breast", Aliases = new() },
          new Ingredient { Name = "Chicken Thigh", Slug = "chicken-thigh", Aliases = new() },
          new Ingredient { Name = "Beef Flank", Slug = "beef-flank", Aliases = new() }
        );
        await context.SaveChangesAsync();
      }
    }

    // Act
    List<IngredientDto>? response = await client.GetFromJsonAsync<List<IngredientDto>>("/api/reference/ingredients?query=Chicken");

    // Assert
    Assert.NotNull(response);
    Assert.Contains(response, i => i.Name == "Chicken Breast");
    Assert.Contains(response, i => i.Name == "Chicken Thigh");
    Assert.DoesNotContain(response, i => i.Name == "Beef Flank");
  }

  [Fact]
  public async Task GetIngredients_ExcludesDeleted() {
    HttpClient client = _factory.CreateClient();
    string uniqueSlug = $"deleted-{Guid.NewGuid()}";

    // Seed data
    using (IServiceScope scope = _factory.Services.CreateScope()) {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      Ingredient ingredient = new Ingredient { 
        Name = "Deleted Item", 
        Slug = uniqueSlug, 
        Aliases = new() 
      };
      context.Ingredients.Add(ingredient);
      await context.SaveChangesAsync();

      context.Ingredients.Remove(ingredient);
      await context.SaveChangesAsync();
    }

    // Act
    List<IngredientDto>? response = await client.GetFromJsonAsync<List<IngredientDto>>("/api/reference/ingredients?query=Deleted");

    // Assert
    Assert.NotNull(response);
    Assert.DoesNotContain(response, i => i.Slug == uniqueSlug);
  }

  [Fact]
  public async Task Ingredient_Slug_IsUnique() {
    using IServiceScope scope = _factory.Services.CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    string slug = "unique-salt";
    
    context.Ingredients.Add(new Ingredient { Name = "Salt 1", Slug = slug, Aliases = new() });
    await context.SaveChangesAsync();

    context.Ingredients.Add(new Ingredient { Name = "Salt 2", Slug = slug, Aliases = new() });
    
    // Assert
    await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
  }
}

