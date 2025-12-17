using Loges.PantryRaid.Models;
using Microsoft.AspNetCore.Identity;

namespace Loges.PantryRaid.WebAPI.Data;

public class DbSeeder : IDbSeeder {
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly UserManager<AppUser> _userManager;
  private readonly IConfiguration _configuration;

  public DbSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    IConfiguration configuration) {
    _roleManager = roleManager;
    _userManager = userManager;
    _configuration = configuration;
  }

  public async Task SeedAsync() {
    // 1. Seed Roles
    string[] roleNames = { "Admin", "User" };
    foreach (string roleName in roleNames) {
      if (!await _roleManager.RoleExistsAsync(roleName)) {
        await _roleManager.CreateAsync(new IdentityRole(roleName));
      }
    }

    // 2. Seed Admin User
    string? adminEmail = _configuration["ADMIN_EMAIL"];
    string? adminPassword = _configuration["ADMIN_PASSWORD"];

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword)) {
      AppUser? adminUser = await _userManager.FindByEmailAsync(adminEmail);

      if (adminUser == null) {
        adminUser = new AppUser {
          UserName = adminEmail,
          Email = adminEmail,
          EmailConfirmed = true // Assume confirmed if seeded
        };

        IdentityResult createResult = await _userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded) {
          await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
      } else {
        // Ensure existing admin user has Admin role
        if (!await _userManager.IsInRoleAsync(adminUser, "Admin")) {
          await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
      }
    }
  }
}
