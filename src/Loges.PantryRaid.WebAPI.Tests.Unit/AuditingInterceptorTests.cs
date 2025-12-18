using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interceptors;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Unit;

public class AuditingInterceptorTests {
  private readonly Mock<ICurrentUserService> _mockCurrentUserService;
  private readonly AuditingInterceptor _interceptor;
  private readonly DbContextOptions<TestDbContext> _options;

  public AuditingInterceptorTests() {
    _mockCurrentUserService = new Mock<ICurrentUserService>();
    _interceptor = new AuditingInterceptor(_mockCurrentUserService.Object);
    _options = new DbContextOptionsBuilder<TestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .AddInterceptors(_interceptor)
      .Options;
  }

  [Fact]
  public async Task SaveChanges_ShouldSetCreatedAuditProperties_WhenEntityIsAdded() {
    // Arrange
    string userId = "user-123";
    _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

    using TestDbContext context = new TestDbContext(_options);
    TestEntity entity = new TestEntity { Name = "Test" };

    // Act
    context.TestEntities.Add(entity);
    await context.SaveChangesAsync();

    // Assert
    Assert.Equal(userId, entity.CreatedBy);
    Assert.NotEqual(default, entity.CreatedAt);
    // Tolerance for time difference
    Assert.True((DateTime.UtcNow - entity.CreatedAt).TotalSeconds < 1);
    Assert.False(entity.IsDeleted);
    Assert.Null(entity.UpdatedBy);
    Assert.Null(entity.UpdatedAt);
  }

  [Fact]
  public async Task SaveChanges_ShouldSetUpdatedAuditProperties_WhenEntityIsModified() {
    // Arrange
    string userId = "user-123";
    string modifierId = "user-456";
    
    using (TestDbContext setupContext = new TestDbContext(_options)) {
      _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
      setupContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Original" });
      await setupContext.SaveChangesAsync();
    }

    // Act
    using (TestDbContext context = new TestDbContext(_options)) {
      _mockCurrentUserService.Setup(s => s.UserId).Returns(modifierId);
      TestEntity entity = await context.TestEntities.FindAsync(1);
      entity!.Name = "Modified";
      
      // Wait a bit to ensure timestamps differ
      await Task.Delay(10);
      
      await context.SaveChangesAsync();

      // Assert
      Assert.Equal(userId, entity.CreatedBy); // Should not change
      Assert.Equal(modifierId, entity.UpdatedBy);
      Assert.NotEqual(default, entity.UpdatedAt);
      Assert.True(entity.UpdatedAt > entity.CreatedAt);
    }
  }

  [Fact]
  public async Task SaveChanges_ShouldSoftDelete_WhenEntityIsDeleted() {
    // Arrange
    string userId = "user-123";
    string deleterId = "user-789";
    
    using (TestDbContext setupContext = new TestDbContext(_options)) {
      _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
      setupContext.TestEntities.Add(new TestEntity { Id = 1, Name = "ToDelete" });
      await setupContext.SaveChangesAsync();
    }

    // Act
    using (TestDbContext context = new TestDbContext(_options)) {
      _mockCurrentUserService.Setup(s => s.UserId).Returns(deleterId);
      TestEntity entity = await context.TestEntities.FindAsync(1);
      context.TestEntities.Remove(entity!);
      await context.SaveChangesAsync();
      
      // Assert state in memory
      Assert.True(entity!.IsDeleted);
      Assert.Equal(deleterId, entity.DeletedBy);
      Assert.NotEqual(default, entity.DeletedAt);
      Assert.Equal(EntityState.Unchanged, context.Entry(entity).State); // It was marked Modified then saved, so now Unchanged
    }

    // Verify persistence (soft delete means it's still in DB but marked deleted)
    using (TestDbContext verifyContext = new TestDbContext(_options)) {
      // By default TestDbContext doesn't have the global filter unless we add it. 
      // The Interceptor handles the soft delete logic (setting flags).
      // The Query Filter is configured in AppDbContext. 
      // Here we test the Interceptor's job: setting the flags and changing state to Modified.
      
      TestEntity deletedEntity = await verifyContext.TestEntities.FindAsync(1);
      Assert.NotNull(deletedEntity);
      Assert.True(deletedEntity.IsDeleted);
    }
  }

  // Helper classes
  public class TestEntity : AuditedEntity {
    public string? Name { get; set; }
  }

  public class TestDbContext : DbContext {
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestEntity> TestEntities { get; set; }
  }
}

