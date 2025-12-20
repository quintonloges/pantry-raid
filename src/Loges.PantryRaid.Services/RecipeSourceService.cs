using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class RecipeSourceService : IRecipeSourceService {
  private readonly AppDbContext _context;

  public RecipeSourceService(AppDbContext context) {
    _context = context;
  }

  public async Task<RecipeSourceDto> CreateAsync(CreateRecipeSourceDto dto) {
    RecipeSource source = new RecipeSource {
      Name = dto.Name,
      BaseUrl = dto.BaseUrl,
      ScraperKey = dto.ScraperKey,
      IsActive = dto.IsActive
    };

    _context.RecipeSources.Add(source);
    await _context.SaveChangesAsync();

    return MapToDto(source);
  }

  public async Task<List<RecipeSourceDto>> GetAllAsync() {
    List<RecipeSource> sources = await _context.RecipeSources
      .OrderBy(s => s.Name)
      .AsNoTracking()
      .ToListAsync();

    return sources.Select(MapToDto).ToList();
  }

  private static RecipeSourceDto MapToDto(RecipeSource source) {
    return new RecipeSourceDto {
      Id = source.Id,
      Name = source.Name,
      BaseUrl = source.BaseUrl,
      ScraperKey = source.ScraperKey,
      IsActive = source.IsActive
    };
  }
}

