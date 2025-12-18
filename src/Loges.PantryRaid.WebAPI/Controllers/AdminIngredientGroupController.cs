using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/ingredient-groups")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminIngredientGroupController : ControllerBase {
  private readonly IIngredientGroupService _service;

  public AdminIngredientGroupController(IIngredientGroupService service) {
    _service = service;
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<IngredientGroupDto>> GetGroup(int id) {
    IngredientGroupDto? group = await _service.GetGroupByIdAsync(id);
    if (group == null) {
      return NotFound();
    }
    return Ok(group);
  }

  [HttpPost]
  public async Task<ActionResult<IngredientGroupDto>> CreateGroup(CreateIngredientGroupDto dto) {
    IngredientGroupDto group = await _service.CreateGroupAsync(dto);
    return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
  }

  [HttpPut("{id}")]
  public async Task<ActionResult<IngredientGroupDto>> UpdateGroup(int id, UpdateIngredientGroupDto dto) {
    IngredientGroupDto? group = await _service.UpdateGroupAsync(id, dto);
    if (group == null) {
      return NotFound();
    }
    return Ok(group);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteGroup(int id) {
    bool success = await _service.DeleteGroupAsync(id);
    if (!success) {
      return NotFound();
    }
    return NoContent();
  }

  [HttpPut("{id}/items")]
  public async Task<IActionResult> SetGroupItems(int id, SetIngredientGroupItemsDto dto) {
    bool success = await _service.SetGroupItemsAsync(id, dto.IngredientIds);
    if (!success) {
      return NotFound();
    }
    return NoContent();
  }
}

