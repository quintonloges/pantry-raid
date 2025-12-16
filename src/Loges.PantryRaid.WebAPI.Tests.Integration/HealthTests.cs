using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>> {
  private readonly WebApplicationFactory<Program> _factory;

  public HealthTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Get_Health_Returns200AndCorrectJson() {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/health");

    // Assert
    response.EnsureSuccessStatusCode(); // Status Code 200-299
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    var content = await response.Content.ReadFromJsonAsync<HealthResponse>(new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    Assert.NotNull(content);
    Assert.Equal("ok", content.Status);
  }

  private class HealthResponse {
    public string Status { get; set; } = string.Empty;
  }
}

