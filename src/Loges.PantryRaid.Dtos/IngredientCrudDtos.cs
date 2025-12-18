namespace Loges.PantryRaid.Dtos;

public class IngredientCreateDto {
  public required string Name { get; set; }
  public List<string> Aliases { get; set; } = new();
  public string? Category { get; set; }
  public string? Notes { get; set; }
}

public class IngredientUpdateDto {
  public required string Name { get; set; }
  public List<string> Aliases { get; set; } = new();
  public string? Category { get; set; }
  public string? Notes { get; set; }
}

