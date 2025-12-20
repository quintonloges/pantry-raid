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

  public async Task<RecipeDto?> GetByIdAsync(int id) {
    Recipe? recipe = await _context.Recipes
      .Include(r => r.Ingredients)
      .Include(r => r.RecipeCuisines).ThenInclude(rc => rc.Cuisine)
      .Include(r => r.RecipeProteins).ThenInclude(rp => rp.Protein)
      .Include(r => r.RecipeDietaryTags).ThenInclude(rd => rd.DietaryTag)
      .FirstOrDefaultAsync(r => r.Id == id);
      
    if (recipe == null) {
      return null;
    }
    return MapToDto(recipe);
  }

  public async Task SetTagsAsync(int recipeId, SetRecipeTagsDto dto) {
    Recipe? recipe = await _context.Recipes
      .Include(r => r.RecipeCuisines)
      .Include(r => r.RecipeProteins)
      .Include(r => r.RecipeDietaryTags)
      .FirstOrDefaultAsync(r => r.Id == recipeId);

    if (recipe == null) {
      throw new ArgumentException("Recipe not found", nameof(recipeId));
    }

    // Clear existing
    _context.RecipeCuisines.RemoveRange(recipe.RecipeCuisines);
    _context.RecipeProteins.RemoveRange(recipe.RecipeProteins);
    _context.RecipeDietaryTags.RemoveRange(recipe.RecipeDietaryTags);

    // Add new
    foreach (int id in dto.CuisineIds.Distinct()) {
      _context.RecipeCuisines.Add(new RecipeCuisine { RecipeId = recipeId, CuisineId = id });
    }
    foreach (int id in dto.ProteinIds.Distinct()) {
      _context.RecipeProteins.Add(new RecipeProtein { RecipeId = recipeId, ProteinId = id });
    }
    foreach (int id in dto.DietaryTagIds.Distinct()) {
      _context.RecipeDietaryTags.Add(new RecipeDietaryTag { RecipeId = recipeId, DietaryTagId = id });
    }

    await _context.SaveChangesAsync();
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
      }).ToList(),
      Cuisines = recipe.RecipeCuisines.Select(rc => new CuisineDto { Id = rc.Cuisine.Id, Name = rc.Cuisine.Name }).ToList(),
      Proteins = recipe.RecipeProteins.Select(rp => new ProteinDto { Id = rp.Protein.Id, Name = rp.Protein.Name }).ToList(),
      DietaryTags = recipe.RecipeDietaryTags.Select(rd => new DietaryTagDto { Id = rd.DietaryTag.Id, Name = rd.DietaryTag.Name }).ToList()
    };
  }
}
