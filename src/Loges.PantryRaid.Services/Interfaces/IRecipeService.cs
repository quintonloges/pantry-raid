using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IRecipeService {
  Task<RecipeDto> CreateAsync(CreateRecipeDto dto);
  Task SetTagsAsync(int recipeId, SetRecipeTagsDto dto);
  Task<RecipeDto?> GetByIdAsync(int id);
}

