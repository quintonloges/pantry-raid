using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models;

public class Recipe : AuditedEntity {
  [Required]
  [MaxLength(255)]
  public string Title { get; set; } = string.Empty;

  public int RecipeSourceId { get; set; }
  public RecipeSource? RecipeSource { get; set; }

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

  public string? RawHtml { get; set; }
  public DateTime? ScrapedAt { get; set; }
  
  [MaxLength(20)]
  public string ScrapeStatus { get; set; } = "Pending";

  public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
  public ICollection<RecipeCuisine> RecipeCuisines { get; set; } = new List<RecipeCuisine>();
  public ICollection<RecipeProtein> RecipeProteins { get; set; } = new List<RecipeProtein>();
  public ICollection<RecipeDietaryTag> RecipeDietaryTags { get; set; } = new List<RecipeDietaryTag>();
}

