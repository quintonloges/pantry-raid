using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class AdminTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;

  public AdminTests(PantryRaidWebApplicationFactory factory) {
      _factory = factory;
  }

  [Fact]
  public async Task NormalUser_CannotAccess_AdminPing() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    // 1. Register normal user
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

    // 3. Access Admin Endpoint
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/ping");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage response = await client.SendAsync(request);
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task BootstrapAdmin_CanAccess_AdminPing() {
    string adminEmail = $"admin_{Guid.NewGuid()}@example.com";
    string adminPassword = "AdminPassword123!";

    // Use WithWebHostBuilder to inject env vars for this specific test client
    HttpClient client = _factory.WithWebHostBuilder(builder => {
      builder.ConfigureAppConfiguration((context, config) => {
        config.AddInMemoryCollection(new Dictionary<string, string?> {
          { "ADMIN_EMAIL", adminEmail },
          { "ADMIN_PASSWORD", adminPassword }
        });
      });
    }).CreateClient();

    // 1. Login as Admin (seeding happens on startup)
    HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = adminEmail,
      Password = adminPassword
    });
    
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    LoginResult? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    string token = loginResult!.Token!;

    // 2. Access Admin Endpoint
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/ping");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    HttpResponseMessage response = await client.SendAsync(request);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    AdminPingResponse? content = await response.Content.ReadFromJsonAsync<AdminPingResponse>();
    Assert.Equal("ok", content?.admin);
  }

  public class AdminPingResponse {
    public string? admin { get; set; }
  }
}

