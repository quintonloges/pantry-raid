using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IUserIngredientService {
  Task<List<IngredientDto>> GetUserIngredientsAsync(string userId);
  Task ReplaceUserIngredientsAsync(string userId, IEnumerable<int> ingredientIds);
}

