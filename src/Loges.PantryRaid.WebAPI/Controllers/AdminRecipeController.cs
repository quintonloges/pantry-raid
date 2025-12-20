using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/recipes")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminRecipeController : ControllerBase {
  private readonly IRecipeService _service;

  public AdminRecipeController(IRecipeService service) {
    _service = service;
  }

  [HttpPost]
  public async Task<ActionResult<RecipeDto>> Create(CreateRecipeDto dto) {
    RecipeDto result = await _service.CreateAsync(dto);
    return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
  }

  [HttpPut("{id}/tags")]
  public async Task<IActionResult> SetTags(int id, SetRecipeTagsDto dto) {
    try {
      await _service.SetTagsAsync(id, dto);
      return NoContent();
    } catch (ArgumentException) {
      return NotFound();
    }
  }
}

