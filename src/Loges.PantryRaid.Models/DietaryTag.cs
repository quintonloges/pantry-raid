using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Models;

public class DietaryTag : AuditedEntity {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  public ICollection<RecipeDietaryTag> RecipeDietaryTags { get; set; } = new List<RecipeDietaryTag>();
}

