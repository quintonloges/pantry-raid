using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Models.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace Loges.PantryRaid.EFCore;

public class AppDbContext : IdentityDbContext<AppUser> {
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
  }

  public DbSet<SystemNote> SystemNotes { get; set; }
  public DbSet<Ingredient> Ingredients { get; set; }
  public DbSet<IngredientGroup> IngredientGroups { get; set; }
  public DbSet<IngredientGroupItem> IngredientGroupItems { get; set; }
  public DbSet<UserIngredient> UserIngredients { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<UserIngredient>(entity => {
      entity.HasKey(e => new { e.UserId, e.IngredientId });
    });

    modelBuilder.Entity<Ingredient>(entity => {
      entity.HasIndex(e => e.Slug).IsUnique();
      entity.Property(e => e.Aliases)
        .HasConversion(
          v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
          v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
        )
        .Metadata.SetValueComparer(new ValueComparer<List<string>>(
          (c1, c2) => c1!.SequenceEqual(c2!),
          c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
          c => c.ToList()
        ));
    });

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
}
