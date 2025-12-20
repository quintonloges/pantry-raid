using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/recipe-sources")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminRecipeSourceController : ControllerBase {
  private readonly IRecipeSourceService _service;

  public AdminRecipeSourceController(IRecipeSourceService service) {
    _service = service;
  }

  [HttpPost]
  public async Task<ActionResult<RecipeSourceDto>> Create(CreateRecipeSourceDto dto) {
    RecipeSourceDto result = await _service.CreateAsync(dto);
    return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
  }
}

