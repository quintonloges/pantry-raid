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
    HttpClient client = _factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/api/db/ping");

    // Assert
    response.EnsureSuccessStatusCode();
    DbPingResponse? content = await response.Content.ReadFromJsonAsync<DbPingResponse>();
    Assert.NotNull(content);
    Assert.Equal("ok", content.db);
  }

  private class DbPingResponse {
    public string db { get; set; } = string.Empty;
  }
}
