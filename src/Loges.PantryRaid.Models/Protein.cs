using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Models;

public class Protein : AuditedEntity {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  public ICollection<RecipeProtein> RecipeProteins { get; set; } = new List<RecipeProtein>();
}

