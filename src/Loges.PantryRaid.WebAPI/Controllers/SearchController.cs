using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/search")]
[ApiController]
public class SearchController : ControllerBase {
  private readonly IRecipeService _recipeService;

  public SearchController(IRecipeService recipeService) {
    _recipeService = recipeService;
  }

  [HttpPost]
  public async Task<ActionResult<SearchResponseDto>> Search([FromBody] SearchRequestDto request) {
    SearchResponseDto result = await _recipeService.SearchAsync(request);
    return Ok(result);
  }
}

