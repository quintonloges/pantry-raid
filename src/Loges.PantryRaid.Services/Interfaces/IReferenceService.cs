using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IReferenceService {
  Task<List<IngredientDto>> GetIngredientsAsync(string? query);

  Task<List<CuisineDto>> GetCuisinesAsync();
  Task<CuisineDto> CreateCuisineAsync(CreateReferenceDto dto);
  Task DeleteCuisineAsync(int id);

  Task<List<ProteinDto>> GetProteinsAsync();
  Task<ProteinDto> CreateProteinAsync(CreateReferenceDto dto);
  Task DeleteProteinAsync(int id);

  Task<List<DietaryTagDto>> GetDietaryTagsAsync();
  Task<DietaryTagDto> CreateDietaryTagAsync(CreateReferenceDto dto);
  Task DeleteDietaryTagAsync(int id);
}

