using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IUnmappedIngredientService {
    Task<List<UnmappedIngredientDto>> GetUnmappedIngredientsAsync(string? status);
    Task ResolveUnmappedIngredientAsync(int id, int resolvedIngredientId);
    Task SuggestUnmappedIngredientAsync(int id, int suggestedIngredientId);
}

