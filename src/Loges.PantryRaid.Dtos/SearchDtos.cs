using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Dtos;

public class SearchRequestDto {
  public List<int> IngredientIds { get; set; } = new();
  public SearchFiltersDto Filters { get; set; } = new();
  public bool AllowSubstitutions { get; set; }
  public string? Cursor { get; set; }
}

public class SearchFiltersDto {
  public int? ProteinId { get; set; }
  public List<int>? CuisineIds { get; set; }
  public List<int>? DietaryTagIds { get; set; }
  public List<int>? MustIncludeIngredientIds { get; set; }
  public List<int>? SourceIds { get; set; }
}

public class SearchResponseDto {
  public List<RecipeGroupDto> Results { get; set; } = new();
  public string? Cursor { get; set; }
}

public class RecipeGroupDto {
  public int MissingCount { get; set; }
  public List<SearchResultRecipeDto> Recipes { get; set; } = new();
}

public class SearchResultRecipeDto {
  public RecipeDto Recipe { get; set; } = new();
  public List<string> HaveIngredients { get; set; } = new();
  public List<string> MissingIngredients { get; set; } = new();
  public List<string> SubstitutionNotes { get; set; } = new();
}

