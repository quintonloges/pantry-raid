using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Dtos;

public class RecipeDto {
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public int RecipeSourceId { get; set; }
  public string SourceUrl { get; set; } = string.Empty;
  public string? ShortDescription { get; set; }
  public string? ImageUrl { get; set; }
  public int? TotalTimeMinutes { get; set; }
  public int? Servings { get; set; }
  public string ScrapeStatus { get; set; } = string.Empty;
  public List<RecipeIngredientDto> Ingredients { get; set; } = new();
  public List<CuisineDto> Cuisines { get; set; } = new();
  public List<ProteinDto> Proteins { get; set; } = new();
  public List<DietaryTagDto> DietaryTags { get; set; } = new();
}

public class RecipeIngredientDto {
  public int Id { get; set; }
  public int? IngredientId { get; set; }
  public string OriginalText { get; set; } = string.Empty;
  public double? Quantity { get; set; }
  public string? Unit { get; set; }
  public bool IsOptional { get; set; }
}

public class CreateRecipeDto {
  [Required]
  [MaxLength(255)]
  public string Title { get; set; } = string.Empty;

  [Required]
  public int RecipeSourceId { get; set; }

  [Required]
  [MaxLength(500)]
  public string SourceUrl { get; set; } = string.Empty;

  [MaxLength(100)]
  public string? SourceRecipeId { get; set; }

  [MaxLength(500)]
  public string? ShortDescription { get; set; }

  [MaxLength(500)]
  public string? ImageUrl { get; set; }

  public int? TotalTimeMinutes { get; set; }
  public int? Servings { get; set; }

  public List<CreateRecipeIngredientDto> Ingredients { get; set; } = new();
}

public class CreateRecipeIngredientDto {
  public int? IngredientId { get; set; }

  [Required]
  [MaxLength(255)]
  public string OriginalText { get; set; } = string.Empty;

  public double? Quantity { get; set; }
  
  [MaxLength(50)]
  public string? Unit { get; set; }
  
  public bool IsOptional { get; set; }
}

