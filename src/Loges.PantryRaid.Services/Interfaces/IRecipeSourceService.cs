using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IRecipeSourceService {
  Task<RecipeSourceDto> CreateAsync(CreateRecipeSourceDto dto);
  Task<List<RecipeSourceDto>> GetAllAsync();
}

