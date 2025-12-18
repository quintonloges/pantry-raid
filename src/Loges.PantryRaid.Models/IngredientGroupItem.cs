using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models;

public class IngredientGroupItem : AuditedEntity {
    public int IngredientGroupId { get; set; }
    [ForeignKey(nameof(IngredientGroupId))]
    public IngredientGroup Group { get; set; } = null!;

    public int IngredientId { get; set; }
    [ForeignKey(nameof(IngredientId))]
    public Ingredient Ingredient { get; set; } = null!;

    public int OrderIndex { get; set; }
}

