using System;

namespace Loges.PantryRaid.Models.Interfaces;

public interface IAuditedEntity {
  DateTime CreatedAt { get; set; }
  string? CreatedBy { get; set; }
  
  DateTime? UpdatedAt { get; set; }
  string? UpdatedBy { get; set; }
  
  bool IsDeleted { get; set; }
  DateTime? DeletedAt { get; set; }
  string? DeletedBy { get; set; }
}

