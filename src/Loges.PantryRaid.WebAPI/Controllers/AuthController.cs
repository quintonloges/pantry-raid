using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Loges.PantryRaid.Models;

namespace Loges.PantryRaid.WebAPI.Controllers.Auth;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase {
  private readonly UserManager<AppUser> _userManager;
  private readonly IConfiguration _configuration;

  public AuthController(UserManager<AppUser> userManager, IConfiguration configuration) {
    _userManager = userManager;
    _configuration = configuration;
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterRequest model) {
    AppUser? userExists = await _userManager.FindByNameAsync(model.Email);
    if (userExists != null) {
      return BadRequest(new { Message = "User already exists!" });
    }

    AppUser user = new() {
      Email = model.Email,
      SecurityStamp = Guid.NewGuid().ToString(),
      UserName = model.Email
    };
    IdentityResult result = await _userManager.CreateAsync(user, model.Password);
    if (!result.Succeeded) {
      return BadRequest(new { Message = "User creation failed", Errors = result.Errors });
    }

    return Ok(new { Message = "User created successfully!" });
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginRequest model) {
    AppUser? user = await _userManager.FindByNameAsync(model.Email);
    // Note: FindByNameAsync respects the global query filter for IsDeleted via AppDbContext
    
    if (user != null && await _userManager.CheckPasswordAsync(user, model.Password)) {
      IList<string> userRoles = await _userManager.GetRolesAsync(user);

      List<Claim> authClaims = new List<Claim> {
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      };

      foreach (string userRole in userRoles) {
        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
      }

      JwtSecurityToken token = GetToken(authClaims);

      return Ok(new {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        expiration = token.ValidTo
      });
    }
    return Unauthorized();
  }

  [Authorize]
  [HttpGet("me")]
  public async Task<IActionResult> Me() {
    // Check if user still exists/is not deleted
    AppUser? user = await _userManager.FindByNameAsync(User.Identity!.Name!);
    if (user == null) {
        return Unauthorized();
    }
    return Ok(new { Email = user.Email });
  }

  [Authorize]
  [HttpPost("change-password")]
  public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model) {
    AppUser? user = await _userManager.FindByNameAsync(User.Identity!.Name!);
    if (user == null) {
      return Unauthorized();
    }

    IdentityResult result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
    if (!result.Succeeded) {
      return BadRequest(new { Message = "Password change failed", Errors = result.Errors });
    }
    return Ok(new { Message = "Password changed successfully" });
  }

  [Authorize]
  [HttpDelete("account")]
  public async Task<IActionResult> DeleteAccount() {
    AppUser? user = await _userManager.FindByNameAsync(User.Identity!.Name!);
    if (user == null) {
      return Unauthorized();
    }

    // UserManager.DeleteAsync calls the store's delete, which calls DbSet.Remove.
    // AppDbContext intercepts Deleted state and converts to Soft Delete (Modified + IsDeleted=true).
    IdentityResult result = await _userManager.DeleteAsync(user);
    if (!result.Succeeded) {
        return BadRequest(new { Message = "Account deletion failed", Errors = result.Errors });
    }
    
    return Ok(new { Message = "Account deleted successfully" });
  }

  private JwtSecurityToken GetToken(List<Claim> authClaims) {
    SymmetricSecurityKey authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

    JwtSecurityToken token = new JwtSecurityToken(
      issuer: _configuration["Jwt:Issuer"],
      audience: _configuration["Jwt:Audience"],
      expires: DateTime.UtcNow.AddHours(3),
      claims: authClaims,
      signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
    );

    return token;
  }
}

public class RegisterRequest {
  public required string Email { get; set; }
  public required string Password { get; set; }
}

public class LoginRequest {
  public required string Email { get; set; }
  public required string Password { get; set; }
}

public class ChangePasswordRequest {
  public required string CurrentPassword { get; set; }
  public required string NewPassword { get; set; }
}
