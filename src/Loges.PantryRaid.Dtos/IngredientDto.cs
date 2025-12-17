namespace Loges.PantryRaid.Dtos;

public class IngredientDto {
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Slug { get; set; } = string.Empty;
  public List<string> Aliases { get; set; } = new();
  public string? Category { get; set; }
  public string? Notes { get; set; }
  public int GlobalRecipeCount { get; set; }
}

