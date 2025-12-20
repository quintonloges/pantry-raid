using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/reference")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReferenceController : ControllerBase {
  private readonly IReferenceService _service;

  public AdminReferenceController(IReferenceService service) {
    _service = service;
  }

  [HttpPost("cuisines")]
  public async Task<ActionResult<CuisineDto>> CreateCuisine(CreateReferenceDto dto) {
    return await _service.CreateCuisineAsync(dto);
  }

  [HttpDelete("cuisines/{id}")]
  public async Task<IActionResult> DeleteCuisine(int id) {
    await _service.DeleteCuisineAsync(id);
    return NoContent();
  }

  [HttpPost("proteins")]
  public async Task<ActionResult<ProteinDto>> CreateProtein(CreateReferenceDto dto) {
    return await _service.CreateProteinAsync(dto);
  }

  [HttpDelete("proteins/{id}")]
  public async Task<IActionResult> DeleteProtein(int id) {
    await _service.DeleteProteinAsync(id);
    return NoContent();
  }

  [HttpPost("dietary-tags")]
  public async Task<ActionResult<DietaryTagDto>> CreateDietaryTag(CreateReferenceDto dto) {
    return await _service.CreateDietaryTagAsync(dto);
  }

  [HttpDelete("dietary-tags/{id}")]
  public async Task<IActionResult> DeleteDietaryTag(int id) {
    await _service.DeleteDietaryTagAsync(id);
    return NoContent();
  }
}

