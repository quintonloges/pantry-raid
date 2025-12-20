using Loges.PantryRaid.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/search")]
[ApiController]
public class SearchController : ControllerBase {
  [HttpPost]
  public ActionResult<SearchResponseDto> Search([FromBody] SearchRequestDto request) {
    // Contract-first: implementation will come later.
    // Returning empty structure to satisfy shape requirements.
    return Ok(new SearchResponseDto {
      Results = new List<RecipeGroupDto> {
        new() { MissingCount = 0, Recipes = new() },
        new() { MissingCount = 1, Recipes = new() },
        new() { MissingCount = 2, Recipes = new() },
        new() { MissingCount = 3, Recipes = new() }
      },
      Cursor = null
    });
  }
}

