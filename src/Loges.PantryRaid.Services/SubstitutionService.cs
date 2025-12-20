using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services {
  public class SubstitutionService : ISubstitutionService {
    private readonly AppDbContext _context;

    public SubstitutionService(AppDbContext context) {
      _context = context;
    }

    public async Task<IEnumerable<SubstitutionGroupDto>> GetAllGroupsAsync() {
      List<SubstitutionGroup> groups = await _context.SubstitutionGroups
        .AsNoTracking()
        .Include(g => g.TargetIngredient)
        .Include(g => g.Options)
          .ThenInclude(o => o.Ingredients)
            .ThenInclude(i => i.Ingredient)
        .OrderBy(g => g.TargetIngredient != null ? g.TargetIngredient.Name : string.Empty)
        .ToListAsync();

      return groups.Select(MapGroupToDto);
    }

    public async Task<SubstitutionGroupDto?> GetGroupByIdAsync(int id) {
      SubstitutionGroup? group = await _context.SubstitutionGroups
        .AsNoTracking()
        .Include(g => g.TargetIngredient)
        .Include(g => g.Options)
          .ThenInclude(o => o.Ingredients)
            .ThenInclude(i => i.Ingredient)
        .FirstOrDefaultAsync(g => g.Id == id);

      return group == null ? null : MapGroupToDto(group);
    }

    public async Task<SubstitutionGroupDto> CreateGroupAsync(CreateSubstitutionGroupRequest request) {
      // Check if group for this ingredient already exists
      SubstitutionGroup? existing = await _context.SubstitutionGroups
        .FirstOrDefaultAsync(g => g.TargetIngredientId == request.TargetIngredientId);
      
      if (existing != null) {
        // Optionally could throw exception or return existing. 
        // For now, let's create a new one only if it doesn't exist?
        // Actually, one ingredient could technically have multiple groups? 
        // The spec implies "substitution_group (target ingredient)" which suggests 1 group per ingredient.
        // Or maybe the group IS the target ingredient.
        // Let's assume 1 group per target ingredient for simplicity.
        return await GetGroupByIdAsync(existing.Id) ?? MapGroupToDto(existing);
      }

      SubstitutionGroup group = new SubstitutionGroup {
        TargetIngredientId = request.TargetIngredientId
      };

      _context.SubstitutionGroups.Add(group);
      await _context.SaveChangesAsync();

      // Need to reload to get navigation properties if we needed them, but for now basic DTO is fine.
      // But MapGroupToDto expects TargetIngredient to be loaded for Name.
      await _context.Entry(group).Reference(g => g.TargetIngredient).LoadAsync();
      
      return MapGroupToDto(group);
    }

    public async Task DeleteGroupAsync(int id) {
      SubstitutionGroup? group = await _context.SubstitutionGroups.FindAsync(id);
      if (group != null) {
        _context.SubstitutionGroups.Remove(group);
        await _context.SaveChangesAsync();
      }
    }

    public async Task<SubstitutionOptionDto> CreateOptionAsync(CreateSubstitutionOptionRequest request) {
      SubstitutionOption option = new SubstitutionOption  {
        SubstitutionGroupId = request.SubstitutionGroupId,
        Note = request.Note
      };

      _context.SubstitutionOptions.Add(option);
      await _context.SaveChangesAsync();

      if (request.IngredientIds != null && request.IngredientIds.Any()) {
        foreach (var ingId in request.IngredientIds) {
          _context.SubstitutionOptionIngredients.Add(new SubstitutionOptionIngredient {
            SubstitutionOptionId = option.Id,
            IngredientId = ingId
          });
        }
        await _context.SaveChangesAsync();
      }

      // Reload for return
      SubstitutionOption? loadedOption = await _context.SubstitutionOptions
        .AsNoTracking()
        .Include(o => o.Ingredients)
          .ThenInclude(i => i.Ingredient)
        .FirstAsync(o => o.Id == option.Id);
      
      return MapOptionToDto(loadedOption);
    }

    public async Task<SubstitutionOptionDto?> UpdateOptionAsync(int id, UpdateSubstitutionOptionRequest request) {
      SubstitutionOption? option = await _context.SubstitutionOptions
        .Include(o => o.Ingredients)
          .ThenInclude(i => i.Ingredient)
        .FirstOrDefaultAsync(o => o.Id == id);
        
      if (option == null) {
        return null;
      }

      option.Note = request.Note;
      await _context.SaveChangesAsync();

      return MapOptionToDto(option);
    }

    public async Task DeleteOptionAsync(int id) {
      SubstitutionOption? option = await _context.SubstitutionOptions.FindAsync(id);
      if (option != null) {
        _context.SubstitutionOptions.Remove(option);
        await _context.SaveChangesAsync();
      }
    }

    public async Task UpdateOptionIngredientsAsync(int optionId, IEnumerable<int> ingredientIds) {
      SubstitutionOption? option = await _context.SubstitutionOptions
        .Include(o => o.Ingredients)
        .FirstOrDefaultAsync(o => o.Id == optionId);
      
      if (option == null) {
        return; // Or throw
      }

      // Hard replace strategy
      _context.SubstitutionOptionIngredients.RemoveRange(option.Ingredients);
      
      foreach (int ingId in ingredientIds) {
        _context.SubstitutionOptionIngredients.Add(new SubstitutionOptionIngredient {
          SubstitutionOptionId = optionId,
          IngredientId = ingId
        });
      }

      await _context.SaveChangesAsync();
    }

    private static SubstitutionGroupDto MapGroupToDto(SubstitutionGroup group) {
      return new SubstitutionGroupDto {
        Id = group.Id,
        TargetIngredientId = group.TargetIngredientId,
        TargetIngredientName = group.TargetIngredient?.Name ?? "Unknown",
        Options = group.Options?.Select(MapOptionToDto) ?? Enumerable.Empty<SubstitutionOptionDto>()
      };
    }

    private static SubstitutionOptionDto MapOptionToDto(SubstitutionOption option) {
      return new SubstitutionOptionDto {
        Id = option.Id,
        SubstitutionGroupId = option.SubstitutionGroupId,
        Note = option.Note,
        Ingredients = option.Ingredients?.Select(i => new SubstitutionOptionIngredientDto {
          IngredientId = i.IngredientId,
          IngredientName = i.Ingredient?.Name ?? "Unknown"
        }) ?? Enumerable.Empty<SubstitutionOptionIngredientDto>()
      };
    }
  }
}

