using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Dtos {
  public class SubstitutionGroupDto {
    public int Id { get; set; }
    public int TargetIngredientId { get; set; }
    public string TargetIngredientName { get; set; } = string.Empty;
    public IEnumerable<SubstitutionOptionDto> Options { get; set; } = Enumerable.Empty<SubstitutionOptionDto>();
  }

  public class SubstitutionOptionDto {
    public int Id { get; set; }
    public int SubstitutionGroupId { get; set; }
    public string? Note { get; set; }
    public IEnumerable<SubstitutionOptionIngredientDto> Ingredients { get; set; } = Enumerable.Empty<SubstitutionOptionIngredientDto>();
  }

  public class SubstitutionOptionIngredientDto {
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
  }

  public class CreateSubstitutionGroupRequest {
    [Required]
    public int TargetIngredientId { get; set; }
  }

  public class CreateSubstitutionOptionRequest {
    [Required]
    public int SubstitutionGroupId { get; set; }
    public string? Note { get; set; }
    // Can optionally create ingredients immediately
    public IEnumerable<int> IngredientIds { get; set; } = Enumerable.Empty<int>();
  }
  
  public class UpdateSubstitutionOptionRequest {
    public string? Note { get; set; }
  }

  public class ReplaceSubstitutionIngredientsRequest {
    [Required]
    public IEnumerable<int> IngredientIds { get; set; } = Enumerable.Empty<int>();
  }
}

