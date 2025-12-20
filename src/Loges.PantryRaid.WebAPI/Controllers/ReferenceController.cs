using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/reference")]
[ApiController]
public class ReferenceController : ControllerBase {
  private readonly IReferenceService _service;
  private readonly IRecipeSourceService _sourceService;

  public ReferenceController(IReferenceService service, IRecipeSourceService sourceService) {
    _service = service;
    _sourceService = sourceService;
  }

  [HttpGet("sources")]
  public async Task<ActionResult<List<RecipeSourceDto>>> GetSources() {
    return await _sourceService.GetAllAsync();
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
