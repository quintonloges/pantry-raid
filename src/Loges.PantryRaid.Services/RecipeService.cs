using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class RecipeService : IRecipeService {
  private readonly AppDbContext _context;

  public RecipeService(AppDbContext context) {
    _context = context;
  }

  public async Task<RecipeDto> CreateAsync(CreateRecipeDto dto) {
    Recipe recipe = new Recipe {
      Title = dto.Title,
      RecipeSourceId = dto.RecipeSourceId,
      SourceUrl = dto.SourceUrl,
      SourceRecipeId = dto.SourceRecipeId,
      ShortDescription = dto.ShortDescription,
      ImageUrl = dto.ImageUrl,
      TotalTimeMinutes = dto.TotalTimeMinutes,
      Servings = dto.Servings,
      ScrapeStatus = "Manual"
    };

    int index = 0;
    foreach (CreateRecipeIngredientDto ingredientDto in dto.Ingredients) {
      RecipeIngredient recipeIngredient = new RecipeIngredient {
        IngredientId = ingredientDto.IngredientId,
        OriginalText = ingredientDto.OriginalText,
        Quantity = ingredientDto.Quantity,
        Unit = ingredientDto.Unit,
        IsOptional = ingredientDto.IsOptional,
        OrderIndex = index++
      };
      recipe.Ingredients.Add(recipeIngredient);
    }

    _context.Recipes.Add(recipe);
    await _context.SaveChangesAsync();
    
    return MapToDto(recipe);
  }

  private static RecipeDto MapToDto(Recipe recipe) {
    return new RecipeDto {
      Id = recipe.Id,
      Title = recipe.Title,
      RecipeSourceId = recipe.RecipeSourceId,
      SourceUrl = recipe.SourceUrl,
      ShortDescription = recipe.ShortDescription,
      ImageUrl = recipe.ImageUrl,
      TotalTimeMinutes = recipe.TotalTimeMinutes,
      Servings = recipe.Servings,
      ScrapeStatus = recipe.ScrapeStatus,
      Ingredients = recipe.Ingredients.OrderBy(i => i.OrderIndex).Select(i => new RecipeIngredientDto {
        Id = i.Id,
        IngredientId = i.IngredientId,
        OriginalText = i.OriginalText,
        Quantity = i.Quantity,
        Unit = i.Unit,
        IsOptional = i.IsOptional
      }).ToList()
    };
  }
}

