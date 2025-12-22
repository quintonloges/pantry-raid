namespace Loges.PantryRaid.Dtos;

public class UnmappedIngredientDto {
  public int Id { get; set; }
  public int RecipeId { get; set; }
  public string RecipeTitle { get; set; } = string.Empty;
  public int RecipeSourceId { get; set; }
  public string RecipeSourceName { get; set; } = string.Empty;
  public string OriginalText { get; set; } = string.Empty;
  public int? SuggestedIngredientId { get; set; }
  public string? SuggestedIngredientName { get; set; }
  public int? ResolvedIngredientId { get; set; }
  public string? ResolvedIngredientName { get; set; }
  public UnmappedIngredientStatusDto Status { get; set; }
  public DateTime CreatedAt { get; set; }
}

public class ResolveUnmappedIngredientRequest {
  public int ResolvedIngredientId { get; set; }
}

public class SuggestUnmappedIngredientRequest {
  public int SuggestedIngredientId { get; set; }
}
