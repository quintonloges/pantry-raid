using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models {
  public class SubstitutionGroup : AuditedEntity {
    public int TargetIngredientId { get; set; }
    [ForeignKey(nameof(TargetIngredientId))]
    public Ingredient? TargetIngredient { get; set; }

    public ICollection<SubstitutionOption> Options { get; set; } = new List<SubstitutionOption>();
  }
}
