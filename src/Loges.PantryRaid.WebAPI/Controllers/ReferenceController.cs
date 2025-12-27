using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/reference")]
[ApiController]
public class ReferenceController : ControllerBase {
  private readonly IReferenceService _service;
  private readonly IRecipeSourceService _sourceService;
  private readonly IIngredientGroupService _groupService;

  public ReferenceController(IReferenceService service, IRecipeSourceService sourceService, IIngredientGroupService groupService) {
    _service = service;
    _sourceService = sourceService;
    _groupService = groupService;
  }

  [HttpGet("sources")]
  public async Task<ActionResult<List<RecipeSourceDto>>> GetSources() {
    return await _sourceService.GetAllAsync();
  }

  [HttpGet("ingredient-groups")]
  public async Task<ActionResult<List<IngredientGroupDto>>> GetIngredientGroups() {
    return await _groupService.GetAllGroupsAsync();
  }

  [HttpGet("ingredients")]
  public async Task<ActionResult<List<IngredientDto>>> GetIngredients([FromQuery] string? query) {
    return await _service.GetIngredientsAsync(query);
  }

  [HttpGet("cuisines")]
  public async Task<ActionResult<List<CuisineDto>>> GetCuisines() {
    return await _service.GetCuisinesAsync();
  }

  [HttpGet("proteins")]
  public async Task<ActionResult<List<ProteinDto>>> GetProteins() {
    return await _service.GetProteinsAsync();
  }

  [HttpGet("dietary-tags")]
  public async Task<ActionResult<List<DietaryTagDto>>> GetDietaryTags() {
    return await _service.GetDietaryTagsAsync();
  }
}
