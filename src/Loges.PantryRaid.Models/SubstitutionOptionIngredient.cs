using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models {
  public class SubstitutionOptionIngredient {
    public int Id { get; set; }

    public int SubstitutionOptionId { get; set; }
    [ForeignKey(nameof(SubstitutionOptionId))]
    public SubstitutionOption? SubstitutionOption { get; set; }

    public int IngredientId { get; set; }
    [ForeignKey(nameof(IngredientId))]
    public Ingredient? Ingredient { get; set; }

    // Optional: Quantity/Unit ratio could go here later.
    // For MVP, we just track that these ingredients are required.
  }
}
