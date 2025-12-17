using Loges.PantryRaid.WebAPI.Controllers;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
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
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register
    HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });
    
    Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

    // 2. Login
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    
    LoginResult? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    Assert.NotNull(loginResult);
    Assert.NotNull(loginResult.Token);

    // 3. Access Protected Endpoint
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
    
    HttpResponseMessage meResponse = await client.SendAsync(request);
    Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    
    MeResult? meResult = await meResponse.Content.ReadFromJsonAsync<MeResult>();
    Assert.Equal(email, meResult?.Email);
  }

  [Fact]
  public async Task Login_WithInvalidPassword_ReturnsUnauthorized() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // Register
    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });

    // Login with wrong password
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
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

