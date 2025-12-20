using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Models;

public class RecipeSource : AuditedEntity {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [Required]
  [MaxLength(255)]
  public string BaseUrl { get; set; } = string.Empty;

  [MaxLength(100)]
  public string? ScraperKey { get; set; }

  public bool IsActive { get; set; } = true;
}

