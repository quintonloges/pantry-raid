namespace Loges.PantryRaid.Models;

public class RecipeDietaryTag {
  public int RecipeId { get; set; }
  public Recipe Recipe { get; set; } = null!;

  public int DietaryTagId { get; set; }
  public DietaryTag DietaryTag { get; set; } = null!;
}

