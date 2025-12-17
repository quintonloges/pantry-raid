using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers.Health;

[Route("api/health")]
[ApiController]
public class HealthController : ControllerBase {
  [HttpGet]
  public IActionResult Get() {
    return Ok(new { status = "ok" });
  }
}