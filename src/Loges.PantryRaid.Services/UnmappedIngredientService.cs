using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class UnmappedIngredientService : IUnmappedIngredientService {
  private readonly AppDbContext _context;

  public UnmappedIngredientService(AppDbContext context) {
    _context = context;
  }

  public async Task<List<UnmappedIngredientDto>> GetUnmappedIngredientsAsync(string? status) {
    IQueryable<UnmappedIngredient> query = _context.UnmappedIngredients
      .Include(u => u.Recipe)
      .Include(u => u.RecipeSource)
      .Include(u => u.SuggestedIngredient)
      .Include(u => u.ResolvedIngredient)
      .AsNoTracking()
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UnmappedIngredientStatus>(status, true, out UnmappedIngredientStatus statusEnum)) {
      query = query.Where(u => u.Status == statusEnum);
    }

    List<UnmappedIngredient> items = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

    return items.Select(u => new UnmappedIngredientDto {
      Id = u.Id,
      RecipeId = u.RecipeId,
      RecipeTitle = u.Recipe?.Title ?? "Unknown",
      RecipeSourceId = u.RecipeSourceId,
      RecipeSourceName = u.RecipeSource?.Name ?? "Unknown",
      OriginalText = u.OriginalText,
      SuggestedIngredientId = u.SuggestedIngredientId,
      SuggestedIngredientName = u.SuggestedIngredient?.Name,
      ResolvedIngredientId = u.ResolvedIngredientId,
      ResolvedIngredientName = u.ResolvedIngredient?.Name,
      Status = (UnmappedIngredientStatusDto)u.Status,
      CreatedAt = u.CreatedAt
    }).ToList();
  }

  public async Task ResolveUnmappedIngredientAsync(int id, int resolvedIngredientId) {
    UnmappedIngredient? item = await _context.UnmappedIngredients.FindAsync(id);
    if (item == null) {
      throw new KeyNotFoundException($"Unmapped ingredient with ID {id} not found.");
    }

    bool ingredientExists = await _context.Ingredients.AnyAsync(i => i.Id == resolvedIngredientId);
    if (!ingredientExists) {
      throw new InvalidOperationException($"Ingredient with ID {resolvedIngredientId} does not exist.");
    }

    item.ResolvedIngredientId = resolvedIngredientId;
    item.Status = UnmappedIngredientStatus.Resolved;

    await _context.SaveChangesAsync();
  }

  public async Task SuggestUnmappedIngredientAsync(int id, int suggestedIngredientId) {
    UnmappedIngredient? item = await _context.UnmappedIngredients.FindAsync(id);
    if (item == null) {
      throw new KeyNotFoundException($"Unmapped ingredient with ID {id} not found.");
    }

    bool ingredientExists = await _context.Ingredients.AnyAsync(i => i.Id == suggestedIngredientId);
    if (!ingredientExists) {
      throw new InvalidOperationException($"Ingredient with ID {suggestedIngredientId} does not exist.");
    }

    item.SuggestedIngredientId = suggestedIngredientId;
    item.Status = UnmappedIngredientStatus.Suggested;

    await _context.SaveChangesAsync();
  }
}
