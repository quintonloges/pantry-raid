namespace Loges.PantryRaid.Models;

public class UserIngredient {
  public required string UserId { get; set; }
  public virtual AppUser User { get; set; } = null!;

  public int IngredientId { get; set; }
  public virtual Ingredient Ingredient { get; set; } = null!;
}

