using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Models.Interfaces;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace Loges.PantryRaid.EFCore;

public class AppDbContext : IdentityDbContext<AppUser> {
  private readonly ICurrentUserService? _currentUserService;

  public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUserService = null) : base(options) {
    _currentUserService = currentUserService;
  }

  public DbSet<SystemNote> SystemNotes { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()) {
      if (typeof(IAuditedEntity).IsAssignableFrom(entityType.ClrType)) {
        MethodInfo method = SetGlobalQueryFilterMethod.MakeGenericMethod(entityType.ClrType);
        method.Invoke(this, new object[] { modelBuilder });
      }
    }
  }

  static readonly MethodInfo SetGlobalQueryFilterMethod = typeof(AppDbContext)
    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
    .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQueryFilter));

  private void SetGlobalQueryFilter<T>(ModelBuilder builder) where T : class, IAuditedEntity {
    builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
  }

  public override int SaveChanges() {
    ApplyAuditInformation();
    return base.SaveChanges();
  }

  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
    ApplyAuditInformation();
    return base.SaveChangesAsync(cancellationToken);
  }

  private void ApplyAuditInformation() {
    IEnumerable<EntityEntry<IAuditedEntity>> entries = ChangeTracker.Entries<IAuditedEntity>();
    DateTime utcNow = DateTime.UtcNow;
    string? user = _currentUserService?.UserId;

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
