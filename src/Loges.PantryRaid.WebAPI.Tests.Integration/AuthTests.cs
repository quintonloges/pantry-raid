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

  [Fact]
  public async Task ChangePassword_Success() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string oldPassword = "Password123!";
    string newPassword = "NewPassword123!";

    // 1. Register
    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = oldPassword
    });

    // 2. Login
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = oldPassword
    });
    LoginResult? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    string token = loginResult!.Token!;

    // 3. Change Password
    HttpRequestMessage changePwRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
    changePwRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    changePwRequest.Content = JsonContent.Create(new ChangePasswordRequest {
      CurrentPassword = oldPassword,
      NewPassword = newPassword
    });

    HttpResponseMessage changePwResponse = await client.SendAsync(changePwRequest);
    Assert.Equal(HttpStatusCode.OK, changePwResponse.StatusCode);

    // 4. Login with Old Password (should fail)
    HttpResponseMessage oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = oldPassword
    });
    Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

    // 5. Login with New Password (should succeed)
    HttpResponseMessage newLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = newPassword
    });
    Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteAccount_SoftDelete_PreventsLogin() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register
    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });

    // 2. Login
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    LoginResult? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    string token = loginResult!.Token!;

    // 3. Delete Account
    HttpRequestMessage deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account");
    deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    HttpResponseMessage deleteResponse = await client.SendAsync(deleteRequest);
    Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

    // 4. Try Login again (should fail)
    HttpResponseMessage reloginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    Assert.Equal(HttpStatusCode.Unauthorized, reloginResponse.StatusCode);

    // 5. Try /me with old token (should fail because user is not found)
    // Note: The token itself is valid signature-wise, but the /me endpoint checks if user exists in DB.
    HttpRequestMessage meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
    meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    HttpResponseMessage meResponse = await client.SendAsync(meRequest);
    Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
  }
}

public class LoginResult {
  public string? Token { get; set; }
  public DateTime Expiration { get; set; }
}

public class MeResult {
  public string? Email { get; set; }
}
