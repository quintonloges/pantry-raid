using Loges.PantryRaid.EFCore;
using System.Security.Claims;

namespace Loges.PantryRaid.WebAPI.Services;

public class CurrentUserService : ICurrentUserService {
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CurrentUserService(IHttpContextAccessor httpContextAccessor) {
    _httpContextAccessor = httpContextAccessor;
  }

  public string? UserId => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
}

