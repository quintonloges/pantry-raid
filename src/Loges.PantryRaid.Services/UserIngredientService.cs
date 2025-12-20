using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Loges.PantryRaid.Services;

public class UserIngredientService : IUserIngredientService {
  private readonly AppDbContext _context;

  public UserIngredientService(AppDbContext context) {
    _context = context;
  }

  public async Task<List<IngredientDto>> GetUserIngredientsAsync(string userId) {
    return await _context.UserIngredients
      .AsNoTracking()
      .Where(ui => ui.UserId == userId)
      .Include(ui => ui.Ingredient)
      .OrderBy(ui => ui.Ingredient.Name)
      .Select(ui => new IngredientDto {
        Id = ui.Ingredient.Id,
        Name = ui.Ingredient.Name,
        Slug = ui.Ingredient.Slug,
        Aliases = ui.Ingredient.Aliases,
        Category = ui.Ingredient.Category,
        Notes = ui.Ingredient.Notes,
        GlobalRecipeCount = ui.Ingredient.GlobalRecipeCount
      })
      .ToListAsync();
  }

  public async Task ReplaceUserIngredientsAsync(string userId, IEnumerable<int> ingredientIds) {
    List<int> uniqueIds = ingredientIds.Distinct().ToList();

    if (uniqueIds.Count > 100) {
      throw new ArgumentException("Cannot have more than 100 ingredients in pantry.");
    }

    // Use a transaction for the replace operation
    using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

    try {
      // Clear existing
      List<UserIngredient> existing = await _context.UserIngredients
        .Where(ui => ui.UserId == userId)
        .ToListAsync();
      
      _context.UserIngredients.RemoveRange(existing);

      // Add new
      IEnumerable<UserIngredient> newIngredients = uniqueIds.Select(id => new UserIngredient {
        UserId = userId,
        IngredientId = id
      });

      await _context.UserIngredients.AddRangeAsync(newIngredients);
      await _context.SaveChangesAsync();
      
      await transaction.CommitAsync();
    } catch {
      await transaction.RollbackAsync();
      throw;
    }
  }
}

