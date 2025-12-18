using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IAdminIngredientService {
  Task<IngredientDto> CreateIngredientAsync(IngredientCreateDto dto);
  Task<IngredientDto> UpdateIngredientAsync(int id, IngredientUpdateDto dto);
  Task DeleteIngredientAsync(int id);
}

