namespace Loges.PantryRaid.Models;

public class RecipeCuisine {
  public int RecipeId { get; set; }
  public Recipe Recipe { get; set; } = null!;

  public int CuisineId { get; set; }
  public Cuisine Cuisine { get; set; } = null!;
}

