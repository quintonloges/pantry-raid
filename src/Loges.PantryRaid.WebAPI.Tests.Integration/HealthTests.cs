using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class HealthTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public HealthTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  [Fact]
  public async Task Get_Health_Returns200AndCorrectJson() {
    // Arrange
    HttpClient client = _factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/api/health");

    // Assert
    response.EnsureSuccessStatusCode(); // Status Code 200-299
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>(new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    Assert.NotNull(content);
    Assert.Equal("ok", content.Status);
  }

  private class HealthResponse {
    public string Status { get; set; } = string.Empty;
  }
}
