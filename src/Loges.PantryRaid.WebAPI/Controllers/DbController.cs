using Loges.PantryRaid.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.WebAPI.Controllers.Db;

[Route("api/db")]
[ApiController]
public class DbController : ControllerBase {
  private readonly AppDbContext _context;

  public DbController(AppDbContext context) {
    _context = context;
  }

  [HttpGet("ping")]
  public async Task<IActionResult> Ping() {
    try {
      if (await _context.Database.CanConnectAsync()) {
        return Ok(new { db = "ok" });
      }
      return StatusCode(500, new { db = "failed" });
    }
    catch (Exception ex) {
      return StatusCode(500, new { db = "failed", error = ex.Message });
    }
  }
}

