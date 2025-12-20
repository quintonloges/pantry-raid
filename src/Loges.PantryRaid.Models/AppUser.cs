using Microsoft.AspNetCore.Identity;
using Loges.PantryRaid.Models.Interfaces;

namespace Loges.PantryRaid.Models;

public class AppUser : IdentityUser, IAuditedEntity {
  public DateTime CreatedAt { get; set; }
  public string? CreatedBy { get; set; }
  
  public DateTime? UpdatedAt { get; set; }
  public string? UpdatedBy { get; set; }
  
  public bool IsDeleted { get; set; }
  public DateTime? DeletedAt { get; set; }
  public string? DeletedBy { get; set; }

  public virtual ICollection<UserIngredient> UserIngredients { get; set; } = new List<UserIngredient>();
}

