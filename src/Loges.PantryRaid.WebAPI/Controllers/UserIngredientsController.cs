using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/user/ingredients")]
[ApiController]
[Authorize]
public class UserIngredientsController : ControllerBase {
  private readonly IUserIngredientService _service;

  public UserIngredientsController(IUserIngredientService service) {
    _service = service;
  }

  [HttpGet]
  public async Task<ActionResult<List<IngredientDto>>> GetUserIngredients() {
    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    if (string.IsNullOrEmpty(userId)) {
      return Unauthorized();
    }

    List<IngredientDto> ingredients = await _service.GetUserIngredientsAsync(userId);
    return Ok(ingredients);
  }

  [HttpPut]
  public async Task<ActionResult> ReplaceUserIngredients([FromBody] List<int> ingredientIds) {
    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    if (string.IsNullOrEmpty(userId)) {
      return Unauthorized();
    }

    try {
      await _service.ReplaceUserIngredientsAsync(userId, ingredientIds);
      return Ok();
    } catch (ArgumentException ex) {
      return BadRequest(new { message = ex.Message });
    }
  }
}

