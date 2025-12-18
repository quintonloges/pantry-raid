using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase {
  private readonly IAdminIngredientService _ingredientService;

  public AdminController(IAdminIngredientService ingredientService) {
    _ingredientService = ingredientService;
  }

  [HttpGet("ping")]
  public IActionResult Ping() {
    return Ok(new { admin = "ok" });
  }

  [HttpPost("ingredients")]
  public async Task<ActionResult<IngredientDto>> CreateIngredient(IngredientCreateDto dto) {
    try {
      IngredientDto result = await _ingredientService.CreateIngredientAsync(dto);
      return CreatedAtAction(nameof(CreateIngredient), new { id = result.Id }, result);
    } catch (InvalidOperationException ex) {
      return Conflict(new { message = ex.Message });
    }
  }

  [HttpPut("ingredients/{id}")]
  public async Task<ActionResult<IngredientDto>> UpdateIngredient(int id, IngredientUpdateDto dto) {
    try {
      IngredientDto result = await _ingredientService.UpdateIngredientAsync(id, dto);
      return Ok(result);
    } catch (KeyNotFoundException) {
      return NotFound();
    } catch (InvalidOperationException ex) {
      return Conflict(new { message = ex.Message });
    }
  }

  [HttpDelete("ingredients/{id}")]
  public async Task<IActionResult> DeleteIngredient(int id) {
    try {
      await _ingredientService.DeleteIngredientAsync(id);
      return NoContent();
    } catch (KeyNotFoundException) {
      return NotFound();
    }
  }
}
