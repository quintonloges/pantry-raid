using Loges.PantryRaid.Models.Interfaces;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Loges.PantryRaid.Services.Interceptors;

public class AuditingInterceptor : SaveChangesInterceptor {
  private readonly ICurrentUserService _currentUserService;

  public AuditingInterceptor(ICurrentUserService currentUserService) {
    _currentUserService = currentUserService;
  }

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
    ApplyAudit(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
    ApplyAudit(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void ApplyAudit(DbContext? context) {
    if (context == null) {
      return;
    }
     
    DateTime utcNow = DateTime.UtcNow;
    string? user = _currentUserService.UserId;

    IEnumerable<EntityEntry<IAuditedEntity>> entries = context.ChangeTracker.Entries<IAuditedEntity>();

    foreach (EntityEntry<IAuditedEntity> entry in entries) {
      if (entry.State == EntityState.Added) {
        entry.Entity.CreatedAt = utcNow;
        entry.Entity.CreatedBy = user;
        entry.Entity.IsDeleted = false;
      } else if (entry.State == EntityState.Modified) {
        // Prevent modification of CreatedAt
        entry.Property(x => x.CreatedAt).IsModified = false;
        entry.Property(x => x.CreatedBy).IsModified = false;
        
        entry.Entity.UpdatedAt = utcNow;
        entry.Entity.UpdatedBy = user;
      } else if (entry.State == EntityState.Deleted) {
        // Soft delete
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAt = utcNow;
        entry.Entity.DeletedBy = user;
        entry.Entity.UpdatedAt = utcNow; 
        entry.Entity.UpdatedBy = user;
      }
    }
  }
}

