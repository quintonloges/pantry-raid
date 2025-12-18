using System.Collections.Generic;

namespace Loges.PantryRaid.Models;

public class IngredientGroup : AuditedEntity {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<IngredientGroupItem> Items { get; set; } = new List<IngredientGroupItem>();
}

