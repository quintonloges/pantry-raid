using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models {
  public class SubstitutionOption : AuditedEntity {
    public int SubstitutionGroupId { get; set; }
    [ForeignKey(nameof(SubstitutionGroupId))]
    public SubstitutionGroup? SubstitutionGroup { get; set; }

    public string? Note { get; set; } // e.g. "Mix and let sit for 5 mins"

    public ICollection<SubstitutionOptionIngredient> Ingredients { get; set; } = new List<SubstitutionOptionIngredient>();
  }
}

