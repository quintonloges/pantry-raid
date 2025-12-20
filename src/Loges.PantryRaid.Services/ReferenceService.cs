using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class ReferenceService : IReferenceService {
  private readonly AppDbContext _context;

  public ReferenceService(AppDbContext context) {
    _context = context;
  }

  public async Task<List<IngredientDto>> GetIngredientsAsync(string? query) {
    IQueryable<Ingredient> q = _context.Ingredients.AsQueryable();
    if (!string.IsNullOrWhiteSpace(query)) {
      q = q.Where(i => i.Name.Contains(query) || i.Slug.Contains(query)); // Simple search
    }
    return await q
      .OrderBy(i => i.Name)
      .Select(i => new IngredientDto { 
        Id = i.Id, 
        Name = i.Name, 
        Slug = i.Slug,
        Aliases = i.Aliases,
        Category = i.Category,
        Notes = i.Notes,
        GlobalRecipeCount = i.GlobalRecipeCount
      })
      .ToListAsync();
  }

  public async Task<List<CuisineDto>> GetCuisinesAsync() {
    return await _context.Cuisines
      .Select(c => new CuisineDto { Id = c.Id, Name = c.Name })
      .OrderBy(c => c.Name)
      .ToListAsync();
  }

  public async Task<CuisineDto> CreateCuisineAsync(CreateReferenceDto dto) {
    Cuisine entity = new Cuisine { Name = dto.Name };
    _context.Cuisines.Add(entity);
    await _context.SaveChangesAsync();
    return new CuisineDto { Id = entity.Id, Name = entity.Name };
  }

  public async Task DeleteCuisineAsync(int id) {
    Cuisine? entity = await _context.Cuisines.FindAsync(id);
    if (entity != null) {
      _context.Cuisines.Remove(entity); // Soft delete handled by query filter and overrides if configured, but here standard remove. AuditedEntity usually implies soft delete?
      // Check AuditedEntity.cs: It has IsDeleted.
      // If I use Remove(), EF might hard delete unless I override Remove or state management.
      // Usually soft delete is handled by setting IsDeleted = true.
      entity.IsDeleted = true;
      entity.DeletedAt = DateTime.UtcNow;
      // We need to check if tracking changes are configured to handle soft delete or if I should do it manually.
      // AppDbContext has global query filter for !IsDeleted.
      // But it doesn't seem to have override SaveChanges to handle soft delete automatically on Remove().
      // So I should set IsDeleted manually.
      await _context.SaveChangesAsync();
    }
  }

  public async Task<List<ProteinDto>> GetProteinsAsync() {
    return await _context.Proteins
      .Select(p => new ProteinDto { Id = p.Id, Name = p.Name })
      .OrderBy(p => p.Name)
      .ToListAsync();
  }

  public async Task<ProteinDto> CreateProteinAsync(CreateReferenceDto dto) {
    var entity = new Protein { Name = dto.Name };
    _context.Proteins.Add(entity);
    await _context.SaveChangesAsync();
    return new ProteinDto { Id = entity.Id, Name = entity.Name };
  }

  public async Task DeleteProteinAsync(int id) {
    var entity = await _context.Proteins.FindAsync(id);
    if (entity != null) {
      entity.IsDeleted = true;
      entity.DeletedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
    }
  }

  public async Task<List<DietaryTagDto>> GetDietaryTagsAsync() {
    return await _context.DietaryTags
      .Select(d => new DietaryTagDto { Id = d.Id, Name = d.Name })
      .OrderBy(d => d.Name)
      .ToListAsync();
  }

  public async Task<DietaryTagDto> CreateDietaryTagAsync(CreateReferenceDto dto) {
    DietaryTag entity = new DietaryTag { Name = dto.Name };
    _context.DietaryTags.Add(entity);
    await _context.SaveChangesAsync();
    return new DietaryTagDto { Id = entity.Id, Name = entity.Name };
  }

  public async Task DeleteDietaryTagAsync(int id) {
    DietaryTag? entity = await _context.DietaryTags.FindAsync(id);
    if (entity != null) {
      entity.IsDeleted = true;
      entity.DeletedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
    }
  }
}

