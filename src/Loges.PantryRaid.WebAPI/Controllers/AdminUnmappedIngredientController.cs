using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/unmapped-ingredients")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUnmappedIngredientController : ControllerBase {
  private readonly IUnmappedIngredientService _service;

  public AdminUnmappedIngredientController(IUnmappedIngredientService service) {
    _service = service;
  }

  [HttpGet]
  public async Task<ActionResult<List<UnmappedIngredientDto>>> GetUnmappedIngredients([FromQuery] string? status) {
    List<UnmappedIngredientDto> items = await _service.GetUnmappedIngredientsAsync(status);
    return Ok(items);
  }

  [HttpPut("{id}/resolve")]
  public async Task<IActionResult> ResolveUnmappedIngredient(int id, [FromBody] ResolveUnmappedIngredientRequest request) {
    try {
      await _service.ResolveUnmappedIngredientAsync(id, request.ResolvedIngredientId);
      return NoContent();
    }
    catch (KeyNotFoundException) {
      return NotFound();
    }
    catch (InvalidOperationException ex) {
      return BadRequest(ex.Message);
    }
  }

  [HttpPut("{id}/suggest")]
  public async Task<IActionResult> SuggestUnmappedIngredient(int id, [FromBody] SuggestUnmappedIngredientRequest request) {
    try {
      await _service.SuggestUnmappedIngredientAsync(id, request.SuggestedIngredientId);
      return NoContent();
    }
    catch (KeyNotFoundException) {
      return NotFound();
    }
    catch (InvalidOperationException ex) {
      return BadRequest(ex.Message);
    }
  }
}
