using System;

namespace Loges.PantryRaid.Models;

public abstract class AuditedEntity {
  public int Id { get; set; }
  
  public DateTime CreatedAt { get; set; }
  public string? CreatedBy { get; set; }
  
  public DateTime? UpdatedAt { get; set; }
  public string? UpdatedBy { get; set; }
  
  public bool IsDeleted { get; set; }
  public DateTime? DeletedAt { get; set; }
  public string? DeletedBy { get; set; }
}

