using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class AuditTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public AuditTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  [Fact]
  public async Task SaveChanges_SetsAuditProperties_And_SoftDeletes() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Ensure DB is ready (relying on factory init, but clean state helps)
    await context.Database.EnsureCreatedAsync();

    var note = new SystemNote { Content = "Test Note" };

    // Act - Create
    context.SystemNotes.Add(note);
    await context.SaveChangesAsync();

    // Assert - Create
    Assert.NotEqual(default, note.CreatedAt);
    Assert.False(note.IsDeleted);
    Assert.Null(note.DeletedAt);
    var createdId = note.Id;

    // Act - Update
    // Need to fetch fresh or keep context tracking. 
    // We'll use the same context instance for simplicity in this integration test 
    // but typically should use new context to verify persistence.
    
    // Let's modify it
    note.Content = "Updated Content";
    await context.SaveChangesAsync();
    
    // Assert - Update
    Assert.NotNull(note.UpdatedAt);
    Assert.True(note.UpdatedAt >= note.CreatedAt);
    
    // Act - Soft Delete
    context.SystemNotes.Remove(note);
    await context.SaveChangesAsync();

    // Assert - Soft Delete
    Assert.True(note.IsDeleted);
    Assert.NotNull(note.DeletedAt);
    
    // Verify Query Filter
    // Create a new context to ensure we are hitting the DB and filter logic
    using var scope2 = _factory.Services.CreateScope();
    var context2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var deletedNote = await context2.SystemNotes.FirstOrDefaultAsync(n => n.Id == createdId);
    Assert.Null(deletedNote); // Should not find it

    var ignoreFilterNote = await context2.SystemNotes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == createdId);
    Assert.NotNull(ignoreFilterNote);
    Assert.True(ignoreFilterNote.IsDeleted);
  }
}

