using Loges.PantryRaid.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Loges.PantryRaid.Services;

public class CurrentUserService : ICurrentUserService {
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CurrentUserService(IHttpContextAccessor httpContextAccessor) {
    _httpContextAccessor = httpContextAccessor;
  }

  public string? UserId => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
}

