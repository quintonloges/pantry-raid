using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/reference")]
[ApiController]
public class ReferenceController : ControllerBase {
  private readonly AppDbContext _context;

  public ReferenceController(AppDbContext context) {
    _context = context;
  }

  [HttpGet("ingredients")]
  public async Task<ActionResult<IEnumerable<IngredientDto>>> GetIngredients([FromQuery] string? query = null) {
    IQueryable<Ingredient> dbQuery = _context.Ingredients.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(query)) {
      dbQuery = dbQuery.Where(i => i.Name.Contains(query));
    }

    // Limit to 50 results to prevent massive payloads, especially for empty queries
    List<IngredientDto> ingredients = await dbQuery
      .OrderBy(i => i.Name)
      .Take(50)
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

    return Ok(ingredients);
  }
}

