using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers {
  [ApiController]
  [Route("api/admin/substitutions")]
  [Authorize(Roles = "Admin")]
  public class SubstitutionController : ControllerBase {
    private readonly ISubstitutionService _substitutionService;

    public SubstitutionController(ISubstitutionService substitutionService) {
      _substitutionService = substitutionService;
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IEnumerable<SubstitutionGroupDto>>> GetAllGroups() {
      IEnumerable<SubstitutionGroupDto> groups = await _substitutionService.GetAllGroupsAsync();
      return Ok(groups);
    }

    [HttpGet("groups/{id}")]
    public async Task<ActionResult<SubstitutionGroupDto>> GetGroupById(int id) {
      SubstitutionGroupDto? group = await _substitutionService.GetGroupByIdAsync(id);
      if (group == null) {
        return NotFound();
      }
      return Ok(group);
    }

    [HttpPost("groups")]
    public async Task<ActionResult<SubstitutionGroupDto>> CreateGroup([FromBody] CreateSubstitutionGroupRequest request) {
      SubstitutionGroupDto group = await _substitutionService.CreateGroupAsync(request);
      return CreatedAtAction(nameof(GetGroupById), new { id = group.Id }, group);
    }

    [HttpDelete("groups/{id}")]
    public async Task<IActionResult> DeleteGroup(int id) {
      await _substitutionService.DeleteGroupAsync(id);
      return NoContent();
    }

    [HttpPost("groups/{groupId}/options")]
    public async Task<ActionResult<SubstitutionOptionDto>> CreateOption(int groupId, [FromBody] CreateSubstitutionOptionRequest request) {
      if (groupId != request.SubstitutionGroupId) {
        // Basic validation, though service takes request.
        request.SubstitutionGroupId = groupId;
      }
      
      SubstitutionOptionDto option = await _substitutionService.CreateOptionAsync(request);
      // Ideally we'd have a GetOption endpoint to link to, but sticking to group context for now.
      return Ok(option); 
    }

    [HttpPut("options/{id}")]
    public async Task<ActionResult<SubstitutionOptionDto>> UpdateOption(int id, [FromBody] UpdateSubstitutionOptionRequest request) {
      SubstitutionOptionDto? option = await _substitutionService.UpdateOptionAsync(id, request);
      if (option == null) {
        return NotFound();
      }
      return Ok(option);
    }

    [HttpDelete("options/{id}")]
    public async Task<IActionResult> DeleteOption(int id) {
      await _substitutionService.DeleteOptionAsync(id);
      return NoContent();
    }

    [HttpPut("options/{id}/ingredients")]
    public async Task<IActionResult> UpdateOptionIngredients(int id, [FromBody] ReplaceSubstitutionIngredientsRequest request) {
      await _substitutionService.UpdateOptionIngredientsAsync(id, request.IngredientIds);
      return NoContent();
    }
  }
}

