using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loges.PantryRaid.Models;

public class UnmappedIngredient : AuditedEntity {
  public int RecipeId { get; set; }
  public Recipe? Recipe { get; set; }

  public int RecipeSourceId { get; set; }
  public RecipeSource? RecipeSource { get; set; }

  [Required]
  [MaxLength(500)]
  public string OriginalText { get; set; } = string.Empty;

  public int? SuggestedIngredientId { get; set; }
  public Ingredient? SuggestedIngredient { get; set; }

  public int? ResolvedIngredientId { get; set; }
  public Ingredient? ResolvedIngredient { get; set; }

  [Column(TypeName = "varchar(20)")]
  public UnmappedIngredientStatus Status { get; set; } = UnmappedIngredientStatus.New;
}
