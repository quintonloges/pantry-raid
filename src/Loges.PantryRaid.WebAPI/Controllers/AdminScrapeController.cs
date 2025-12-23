using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ScrapeController : ControllerBase {
  private readonly IScrapingService _scrapingService;

  public ScrapeController(IScrapingService scrapingService) {
    _scrapingService = scrapingService;
  }

  [HttpPost]
  public async Task<ActionResult<ScrapeResultDto>> Scrape([FromBody] ScrapeRequestDto request) {
    if (string.IsNullOrWhiteSpace(request.Url)) {
      return BadRequest("URL is required.");
    }

    ScrapeResultDto result = await _scrapingService.ScrapeRecipeAsync(request.Url);

    // Always return 200 OK as per requirements, unless it's a bad request structure.
    // "Log failures and return 200 with a result payload"
    return Ok(result);
  }
}
