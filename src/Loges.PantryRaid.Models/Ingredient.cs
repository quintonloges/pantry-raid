using System.Text.RegularExpressions;

namespace Loges.PantryRaid.Models;

public class Ingredient : AuditedEntity {
  public required string Name { get; set; }
  public required string Slug { get; set; }
  public List<string> Aliases { get; set; } = new();
  public string? Category { get; set; }
  public string? Notes { get; set; }
  public int GlobalRecipeCount { get; set; } = 0;

  public static string GenerateSlug(string name) {
    if (string.IsNullOrWhiteSpace(name)) {
      return string.Empty;
    }
    
    // Lowercase
    string slug = name.ToLowerInvariant();
    
    // Remove invalid chars
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    
    // Convert multiple spaces into one space
    slug = Regex.Replace(slug, @"\s+", " ").Trim();
    
    // Replace spaces with hyphens
    slug = slug.Replace(" ", "-");
    
    return slug;
  }
}

