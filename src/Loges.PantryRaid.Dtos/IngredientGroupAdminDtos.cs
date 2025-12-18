using System.Collections.Generic;

namespace Loges.PantryRaid.Dtos;

public class CreateIngredientGroupDto {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int>? InitialIngredientIds { get; set; }
}

public class UpdateIngredientGroupDto {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SetIngredientGroupItemsDto {
    public List<int> IngredientIds { get; set; } = new();
}

