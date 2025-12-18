using System.Collections.Generic;

namespace Loges.PantryRaid.Dtos;

public class IngredientGroupDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<IngredientGroupItemDto> Items { get; set; } = new();
}

public class IngredientGroupItemDto {
    public int IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

