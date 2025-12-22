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
  public DbSet<RecipeSource> RecipeSources { get; set; }
  public DbSet<Recipe> Recipes { get; set; }
  public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
  public DbSet<Cuisine> Cuisines { get; set; }
  public DbSet<Protein> Proteins { get; set; }
  public DbSet<DietaryTag> DietaryTags { get; set; }
  public DbSet<RecipeCuisine> RecipeCuisines { get; set; }
  public DbSet<RecipeProtein> RecipeProteins { get; set; }
  public DbSet<RecipeDietaryTag> RecipeDietaryTags { get; set; }
  public DbSet<SubstitutionGroup> SubstitutionGroups { get; set; }
  public DbSet<SubstitutionOption> SubstitutionOptions { get; set; }
  public DbSet<SubstitutionOptionIngredient> SubstitutionOptionIngredients { get; set; }
  public DbSet<UnmappedIngredient> UnmappedIngredients { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<UserIngredient>(entity => {
      entity.HasKey(e => new { e.UserId, e.IngredientId });
    });

    modelBuilder.Entity<RecipeSource>(entity => {
      entity.HasIndex(e => e.Name).IsUnique();
    });

    modelBuilder.Entity<Recipe>(entity => {
      entity.HasIndex(e => e.SourceUrl);
      entity.HasIndex(e => new { e.RecipeSourceId, e.SourceRecipeId });
      
      entity.HasOne(e => e.RecipeSource)
        .WithMany()
        .HasForeignKey(e => e.RecipeSourceId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<RecipeIngredient>(entity => {
      entity.HasOne(e => e.Recipe)
        .WithMany(r => r.Ingredients)
        .HasForeignKey(e => e.RecipeId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(e => e.Ingredient)
        .WithMany()
        .HasForeignKey(e => e.IngredientId)
        .OnDelete(DeleteBehavior.SetNull);
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

    modelBuilder.Entity<RecipeCuisine>(entity => {
      entity.HasKey(e => new { e.RecipeId, e.CuisineId });
      entity.HasOne(e => e.Recipe)
        .WithMany(r => r.RecipeCuisines)
        .HasForeignKey(e => e.RecipeId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Cuisine)
        .WithMany(c => c.RecipeCuisines)
        .HasForeignKey(e => e.CuisineId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<RecipeProtein>(entity => {
      entity.HasKey(e => new { e.RecipeId, e.ProteinId });
      entity.HasOne(e => e.Recipe)
        .WithMany(r => r.RecipeProteins)
        .HasForeignKey(e => e.RecipeId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Protein)
        .WithMany(p => p.RecipeProteins)
        .HasForeignKey(e => e.ProteinId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<RecipeDietaryTag>(entity => {
      entity.HasKey(e => new { e.RecipeId, e.DietaryTagId });
      entity.HasOne(e => e.Recipe)
        .WithMany(r => r.RecipeDietaryTags)
        .HasForeignKey(e => e.RecipeId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.DietaryTag)
        .WithMany(d => d.RecipeDietaryTags)
        .HasForeignKey(e => e.DietaryTagId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<SubstitutionGroup>(entity => {
      entity.HasOne(e => e.TargetIngredient)
        .WithMany()
        .HasForeignKey(e => e.TargetIngredientId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<SubstitutionOption>(entity => {
      entity.HasOne(e => e.SubstitutionGroup)
        .WithMany(g => g.Options)
        .HasForeignKey(e => e.SubstitutionGroupId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<SubstitutionOptionIngredient>(entity => {
      entity.HasOne(e => e.SubstitutionOption)
        .WithMany(o => o.Ingredients)
        .HasForeignKey(e => e.SubstitutionOptionId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(e => e.Ingredient)
        .WithMany()
        .HasForeignKey(e => e.IngredientId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<UnmappedIngredient>(entity => {
      entity.Property(e => e.Status)
        .HasConversion<string>();

      entity.HasOne(e => e.Recipe)
        .WithMany()
        .HasForeignKey(e => e.RecipeId)
        .OnDelete(DeleteBehavior.Restrict);

      entity.HasOne(e => e.RecipeSource)
        .WithMany()
        .HasForeignKey(e => e.RecipeSourceId)
        .OnDelete(DeleteBehavior.Restrict);

      entity.HasOne(e => e.SuggestedIngredient)
        .WithMany()
        .HasForeignKey(e => e.SuggestedIngredientId)
        .OnDelete(DeleteBehavior.Restrict);

      entity.HasOne(e => e.ResolvedIngredient)
        .WithMany()
        .HasForeignKey(e => e.ResolvedIngredientId)
        .OnDelete(DeleteBehavior.Restrict);
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
