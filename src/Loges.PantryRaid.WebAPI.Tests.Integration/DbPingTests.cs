using System.Net.Http.Json;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class DbPingTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public DbPingTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  [Fact]
  public async Task Get_DbPing_Returns200AndOk() {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/db/ping");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<DbPingResponse>();
    Assert.NotNull(content);
    Assert.Equal("ok", content.db);
  }

  private class DbPingResponse {
    public string db { get; set; } = string.Empty;
  }
}
