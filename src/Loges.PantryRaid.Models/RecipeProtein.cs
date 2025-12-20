namespace Loges.PantryRaid.Models;

public class RecipeProtein {
  public int RecipeId { get; set; }
  public Recipe Recipe { get; set; } = null!;

  public int ProteinId { get; set; }
  public Protein Protein { get; set; } = null!;
}

