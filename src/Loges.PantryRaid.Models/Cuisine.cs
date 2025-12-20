using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Models;

public class Cuisine : AuditedEntity {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  public ICollection<RecipeCuisine> RecipeCuisines { get; set; } = new List<RecipeCuisine>();
}

