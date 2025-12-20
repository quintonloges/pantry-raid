using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Models;

public class RecipeIngredient : AuditedEntity {
  // Id is in base

  public int RecipeId { get; set; }
  public Recipe? Recipe { get; set; }

  public int? IngredientId { get; set; }
  public Ingredient? Ingredient { get; set; }

  [Required]
  [MaxLength(255)]
  public string OriginalText { get; set; } = string.Empty;

  public double? Quantity { get; set; }
  
  [MaxLength(50)]
  public string? Unit { get; set; }
  
  public int OrderIndex { get; set; }
  
  public bool IsOptional { get; set; }
}

