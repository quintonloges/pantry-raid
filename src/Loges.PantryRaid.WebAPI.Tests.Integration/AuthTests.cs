using Loges.PantryRaid.WebAPI.Controllers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class AuthTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public AuthTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
  }

  [Fact]
  public async Task Register_Login_And_AccessProtectedEndpoint_Success() {
    var client = _factory.CreateClient();
    var email = $"user_{Guid.NewGuid()}@example.com";
    var password = "Password123!";

    // 1. Register
    var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });
    
    Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

    // 2. Login
    var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    
    var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    Assert.NotNull(loginResult);
    Assert.NotNull(loginResult.Token);

    // 3. Access Protected Endpoint
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
    
    var meResponse = await client.SendAsync(request);
    Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    
    var meResult = await meResponse.Content.ReadFromJsonAsync<MeResult>();
    Assert.Equal(email, meResult?.Email);
  }

  [Fact]
  public async Task Login_WithInvalidPassword_ReturnsUnauthorized() {
    var client = _factory.CreateClient();
    var email = $"user_{Guid.NewGuid()}@example.com";
    var password = "Password123!";

    // Register
    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });

    // Login with wrong password
    var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = "WrongPassword!"
    });

    Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
  }
}

public class LoginResult {
  public string? Token { get; set; }
  public DateTime Expiration { get; set; }
}

public class MeResult {
  public string? Email { get; set; }
}

