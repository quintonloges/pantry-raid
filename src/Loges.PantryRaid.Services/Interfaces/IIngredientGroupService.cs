using System.Collections.Generic;
using System.Threading.Tasks;
using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IIngredientGroupService {
    Task<List<IngredientGroupDto>> GetAllGroupsAsync();
    Task<IngredientGroupDto?> GetGroupByIdAsync(int id);
    Task<IngredientGroupDto> CreateGroupAsync(CreateIngredientGroupDto dto);
    Task<IngredientGroupDto?> UpdateGroupAsync(int id, UpdateIngredientGroupDto dto);
    Task<bool> DeleteGroupAsync(int id);
    Task<bool> SetGroupItemsAsync(int id, List<int> ingredientIds);
}

