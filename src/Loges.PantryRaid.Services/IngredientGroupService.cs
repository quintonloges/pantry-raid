using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class IngredientGroupService : IIngredientGroupService {
  private readonly AppDbContext _context;

  public IngredientGroupService(AppDbContext context) {
    _context = context;
  }

  public async Task<List<IngredientGroupDto>> GetAllGroupsAsync() {
    List<IngredientGroup> groups = await _context.IngredientGroups
      .Include(g => g.Items)
      .ThenInclude(i => i.Ingredient)
      .OrderBy(g => g.Name)
      .AsNoTracking()
      .ToListAsync();

    return groups.Select(MapToDto).ToList();
  }

  public async Task<IngredientGroupDto?> GetGroupByIdAsync(int id) {
    IngredientGroup? group = await _context.IngredientGroups
      .Include(g => g.Items)
      .ThenInclude(i => i.Ingredient)
      .FirstOrDefaultAsync(g => g.Id == id);

    return group == null ? null : MapToDto(group);
  }

  public async Task<IngredientGroupDto> CreateGroupAsync(CreateIngredientGroupDto dto) {
    IngredientGroup group = new IngredientGroup {
      Name = dto.Name,
      Description = dto.Description
    };

    if (dto.InitialIngredientIds != null && dto.InitialIngredientIds.Any()) {
      int index = 0;
      foreach (int ingredientId in dto.InitialIngredientIds) {
        // Validate ingredient exists? Or just add. Foreign key will fail if not exists.
        // Better to check or let it fail? Let's assume valid IDs for now or catch exception.
        // However, to be safe and avoid partial fails if we want strictness:
        group.Items.Add(new IngredientGroupItem {
          IngredientId = ingredientId,
          OrderIndex = index++
        });
      }
    }

    _context.IngredientGroups.Add(group);
    await _context.SaveChangesAsync();

    // Reload to get ingredient names if needed, but for create return, usually basic is fine.
    // But MapToDto needs Ingredient.Name.
    // So we might need to load references or just return what we have (if names are missing).
    // Let's reload to be safe and consistent.
    return (await GetGroupByIdAsync(group.Id))!;
  }

  public async Task<IngredientGroupDto?> UpdateGroupAsync(int id, UpdateIngredientGroupDto dto) {
    IngredientGroup? group = await _context.IngredientGroups.FindAsync(id);
    if (group == null) {
      return null;
    }

    group.Name = dto.Name;
    group.Description = dto.Description;

    await _context.SaveChangesAsync();
    return await GetGroupByIdAsync(id);
  }

  public async Task<bool> DeleteGroupAsync(int id) {
    IngredientGroup? group = await _context.IngredientGroups
      .Include(g => g.Items)
      .FirstOrDefaultAsync(g => g.Id == id);
        
    if (group == null) {
      return false;
    }

    _context.IngredientGroupItems.RemoveRange(group.Items);
    _context.IngredientGroups.Remove(group);
    
    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<bool> SetGroupItemsAsync(int id, List<int> ingredientIds) {
    IngredientGroup? group = await _context.IngredientGroups
      .Include(g => g.Items)
      .FirstOrDefaultAsync(g => g.Id == id);
        
    if (group == null) {
      return false;
    }

    // Remove existing items
    // Since IngredientGroupItem is also Audited, should we soft delete them?
    // Spec says "No orphaned items".
    // If we soft delete the group, we might want to keep items.
    // But here we are REPLACING the list.
    // If we soft delete, the unique constraint (if any) might bite us if we re-add same items?
    // There is no unique constraint on (GroupId, IngredientId) defined in `OnModelCreating` yet.
    // But logically, hard deleting the items for a REPLACEMENT operation seems cleaner if we don't need history of items in a group.
    // If I use _context.IngredientGroupItems.RemoveRange(group.Items), and the interceptor soft-deletes them, 
    // then when I add new ones with same IngredientId, will it conflict?
    // Only if I have a unique index including IsDeleted=false.
    // Let's assume hard delete for items for now or check Interceptor.
    
    // For now, I'll clear the collection.
    _context.IngredientGroupItems.RemoveRange(group.Items);
    
    int index = 0;
    foreach (int ingredientId in ingredientIds) {
      group.Items.Add(new IngredientGroupItem {
        IngredientId = ingredientId,
        OrderIndex = index++
      });
    }

    await _context.SaveChangesAsync();
    return true;
  }

  private static IngredientGroupDto MapToDto(IngredientGroup group) {
    return new IngredientGroupDto {
      Id = group.Id,
      Name = group.Name,
      Description = group.Description,
      Items = group.Items.OrderBy(i => i.OrderIndex).Select(i => new IngredientGroupItemDto {
          IngredientId = i.IngredientId,
          Name = i.Ingredient?.Name ?? string.Empty,
          OrderIndex = i.OrderIndex
      }).ToList()
    };
  }
}

