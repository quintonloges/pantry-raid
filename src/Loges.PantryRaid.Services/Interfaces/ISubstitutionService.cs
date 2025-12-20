using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces {
  public interface ISubstitutionService {
    Task<IEnumerable<SubstitutionGroupDto>> GetAllGroupsAsync();
    Task<SubstitutionGroupDto?> GetGroupByIdAsync(int id);
    Task<SubstitutionGroupDto> CreateGroupAsync(CreateSubstitutionGroupRequest request);
    Task DeleteGroupAsync(int id);

    Task<SubstitutionOptionDto> CreateOptionAsync(CreateSubstitutionOptionRequest request);
    Task<SubstitutionOptionDto?> UpdateOptionAsync(int id, UpdateSubstitutionOptionRequest request);
    Task DeleteOptionAsync(int id);
    Task UpdateOptionIngredientsAsync(int optionId, IEnumerable<int> ingredientIds);
  }
}

