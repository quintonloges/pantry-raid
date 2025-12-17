using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loges.PantryRaid.WebAPI.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase {  
  [HttpGet("ping")]
  public IActionResult Ping() {
    return Ok(new { admin = "ok" });
  }
}

