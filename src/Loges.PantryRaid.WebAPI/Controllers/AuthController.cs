using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Loges.PantryRaid.WebAPI.Controllers.Auth;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase {
  private readonly UserManager<IdentityUser> _userManager;
  private readonly IConfiguration _configuration;

  public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration) {
    _userManager = userManager;
    _configuration = configuration;
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterRequest model) {
    IdentityUser? userExists = await _userManager.FindByNameAsync(model.Email);
    if (userExists != null) {
      return BadRequest(new { Message = "User already exists!" });
    }

    IdentityUser user = new() {
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
    IdentityUser? user = await _userManager.FindByNameAsync(model.Email);
    if (user != null && await _userManager.CheckPasswordAsync(user, model.Password)) {
      IList<string> userRoles = await _userManager.GetRolesAsync(user);

      List<Claim> authClaims = new List<Claim> {
        new Claim(ClaimTypes.Name, user.UserName!),
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
  public IActionResult Me() {
    return Ok(new { Email = User.Identity?.Name });
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

