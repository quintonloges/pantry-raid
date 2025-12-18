using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class AdminIngredientService : IAdminIngredientService {
  private readonly AppDbContext _context;

  public AdminIngredientService(AppDbContext context) {
    _context = context;
  }

  public async Task<IngredientDto> CreateIngredientAsync(IngredientCreateDto dto) {
    string slug = Ingredient.GenerateSlug(dto.Name);

    // Validate uniqueness (including deleted items to prevent DB index violation)
    bool isDuplicate = await _context.Ingredients
      .IgnoreQueryFilters()
      .AnyAsync(i => i.Slug == slug || i.Name.ToLower() == dto.Name.ToLower());

    if (isDuplicate) {
      throw new InvalidOperationException($"Ingredient with name '{dto.Name}' or slug '{slug}' already exists.");
    }

    Ingredient ingredient = new Ingredient {
      Name = dto.Name,
      Slug = slug,
      Aliases = dto.Aliases ?? new List<string>(),
      Category = dto.Category,
      Notes = dto.Notes,
      GlobalRecipeCount = 0
    };

    _context.Ingredients.Add(ingredient);
    await _context.SaveChangesAsync();

    return MapToDto(ingredient);
  }

  public async Task<IngredientDto> UpdateIngredientAsync(int id, IngredientUpdateDto dto) {
    Ingredient? ingredient = await _context.Ingredients.FindAsync(id);
    if (ingredient == null) {
      throw new KeyNotFoundException($"Ingredient with ID {id} not found.");
    }

    string newSlug = Ingredient.GenerateSlug(dto.Name);

    // Validate uniqueness if name changed
    if (newSlug != ingredient.Slug || !string.Equals(dto.Name, ingredient.Name, StringComparison.CurrentCultureIgnoreCase)) {
      bool isDuplicate = await _context.Ingredients
        .IgnoreQueryFilters()
        .AnyAsync(i => i.Id != id && (i.Slug == newSlug || i.Name.ToLower() == dto.Name.ToLower()));

      if (isDuplicate) {
        throw new InvalidOperationException($"Ingredient with name '{dto.Name}' or slug '{newSlug}' already exists.");
      }
    }

    ingredient.Name = dto.Name;
    ingredient.Slug = newSlug;
    ingredient.Aliases = dto.Aliases ?? new List<string>();
    ingredient.Category = dto.Category;
    ingredient.Notes = dto.Notes;

    await _context.SaveChangesAsync();

    return MapToDto(ingredient);
  }

  public async Task DeleteIngredientAsync(int id) {
    Ingredient? ingredient = await _context.Ingredients.FindAsync(id);
    if (ingredient == null) {
      // Idempotent delete? Or throw? Usually idempotent is nicer, but for explicit admin delete maybe throw if not found.
      // Given it's an API, 404 is appropriate if not found.
      throw new KeyNotFoundException($"Ingredient with ID {id} not found.");
    }

    _context.Ingredients.Remove(ingredient); // DbContext handles soft delete via interceptor
    await _context.SaveChangesAsync();
  }

  private static IngredientDto MapToDto(Ingredient ingredient) {
    return new IngredientDto {
      Id = ingredient.Id,
      Name = ingredient.Name,
      Slug = ingredient.Slug,
      Aliases = ingredient.Aliases,
      Category = ingredient.Category,
      Notes = ingredient.Notes,
      GlobalRecipeCount = ingredient.GlobalRecipeCount
    };
  }
}

