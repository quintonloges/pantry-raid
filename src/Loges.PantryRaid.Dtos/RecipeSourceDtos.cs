using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Dtos;

public class RecipeSourceDto {
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string BaseUrl { get; set; } = string.Empty;
  public string? ScraperKey { get; set; }
  public bool IsActive { get; set; }
}

public class CreateRecipeSourceDto {
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

