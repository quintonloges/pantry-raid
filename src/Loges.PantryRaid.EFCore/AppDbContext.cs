using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Loges.PantryRaid.Models;
using System.Linq.Expressions;

namespace Loges.PantryRaid.EFCore;

public class AppDbContext : IdentityDbContext<IdentityUser> {
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
  }

  public DbSet<SystemNote> SystemNotes { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
      if (typeof(AuditedEntity).IsAssignableFrom(entityType.ClrType)) {
        var method = SetGlobalQueryFilterMethod.MakeGenericMethod(entityType.ClrType);
        method.Invoke(this, new object[] { modelBuilder });
      }
    }
  }

  static readonly System.Reflection.MethodInfo SetGlobalQueryFilterMethod = typeof(AppDbContext)
    .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQueryFilter));

  private void SetGlobalQueryFilter<T>(ModelBuilder builder) where T : AuditedEntity {
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
    var entries = ChangeTracker.Entries<AuditedEntity>();
    var utcNow = DateTime.UtcNow;

    foreach (var entry in entries) {
      if (entry.State == EntityState.Added) {
        entry.Entity.CreatedAt = utcNow;
        entry.Entity.IsDeleted = false;
      } else if (entry.State == EntityState.Modified) {
        // Prevent modification of CreatedAt
        entry.Property(x => x.CreatedAt).IsModified = false;
        entry.Property(x => x.CreatedBy).IsModified = false;
        
        entry.Entity.UpdatedAt = utcNow;
      } else if (entry.State == EntityState.Deleted) {
        // Soft delete
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAt = utcNow;
        entry.Entity.UpdatedAt = utcNow; // Also update updated_at? Usually yes or no. Let's do yes.
      }
    }
  }
}
