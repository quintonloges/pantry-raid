using System.ComponentModel.DataAnnotations;

namespace Loges.PantryRaid.Dtos;

public class CuisineDto {
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
}

public class ProteinDto {
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
}

public class DietaryTagDto {
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
}

public class CreateReferenceDto {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;
}

public class SetRecipeTagsDto {
  public List<int> CuisineIds { get; set; } = new();
  public List<int> ProteinIds { get; set; } = new();
  public List<int> DietaryTagIds { get; set; } = new();
}

