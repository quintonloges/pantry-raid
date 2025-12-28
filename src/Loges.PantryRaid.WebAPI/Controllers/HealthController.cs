using Loges.PantryRaid.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers.Health;

[Route("api/health")]
[ApiController]
public class HealthController : ControllerBase {
  [HttpGet]
  [ProducesResponseType(typeof(HealthResponseDto), 200)]
  public IActionResult Get() {
    return Ok(new HealthResponseDto { Status = "ok" });
  }
}
