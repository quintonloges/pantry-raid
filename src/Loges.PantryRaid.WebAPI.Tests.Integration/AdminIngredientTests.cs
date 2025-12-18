using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class AdminIngredientTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string? _adminToken;

  public AdminIngredientTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
    
    // Configure client with Admin credentials injected
    string adminEmail = $"admin_{Guid.NewGuid()}@example.com";
    string adminPassword = "AdminPassword123!";
    
    _client = _factory.WithWebHostBuilder(builder => {
      builder.ConfigureAppConfiguration((context, config) => {
        config.AddInMemoryCollection(new Dictionary<string, string?> {
          { "ADMIN_EMAIL", adminEmail },
          { "ADMIN_PASSWORD", adminPassword }
        });
      });
    }).CreateClient();
    
    // We can't await in constructor, so we'll lazy-load the token in a helper method or init in tests.
    // Storing credentials for use in GetAdminTokenAsync.
    _adminCredentials = (adminEmail, adminPassword);
  }

  private (string Email, string Password) _adminCredentials;

  private async Task<string> GetAdminTokenAsync() {
    if (_adminToken != null) {
      return _adminToken;
    }

    HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = _adminCredentials.Email,
      Password = _adminCredentials.Password
    });

    loginResponse.EnsureSuccessStatusCode();
    LoginResult? result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
    _adminToken = result!.Token!;
    return _adminToken;
  }

  private async Task<string> GetUserTokenAsync() {
    HttpClient client = _factory.CreateClient();
    string email = $"user_{Guid.NewGuid()}@example.com";
    string password = "Password123!";

    await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
      Email = email,
      Password = password
    });

    HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
      Email = email,
      Password = password
    });
    
    LoginResult? result = await response.Content.ReadFromJsonAsync<LoginResult>();
    return result!.Token!;
  }

  [Fact]
  public async Task CreateIngredient_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    IngredientCreateDto dto = new IngredientCreateDto {
      Name = "New Spice " + Guid.NewGuid(),
      Category = "Spices",
      Notes = "Spicy"
    };

    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", dto);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    IngredientDto? result = await response.Content.ReadFromJsonAsync<IngredientDto>();
    Assert.NotNull(result);
    Assert.Equal(dto.Name, result.Name);
    Assert.True(result.Id > 0);
  }

  [Fact]
  public async Task CreateIngredient_AsUser_Forbidden() {
    string token = await GetUserTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    IngredientCreateDto dto = new IngredientCreateDto { Name = "Forbidden Fruit" };
    HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/ingredients", dto);
    
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateIngredient_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Create first
    IngredientCreateDto createDto = new IngredientCreateDto { Name = "Old Name " + Guid.NewGuid() };
    HttpResponseMessage createRes = await _client.PostAsJsonAsync("/api/admin/ingredients", createDto);
    IngredientDto? created = await createRes.Content.ReadFromJsonAsync<IngredientDto>();

    // Update
    IngredientUpdateDto updateDto = new IngredientUpdateDto { 
      Name = "New Name " + Guid.NewGuid(),
      Category = "Updated Cat" 
    };
    HttpResponseMessage updateRes = await _client.PutAsJsonAsync($"/api/admin/ingredients/{created!.Id}", updateDto);
    
    Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
    IngredientDto? updated = await updateRes.Content.ReadFromJsonAsync<IngredientDto>();
    Assert.Equal(updateDto.Name, updated!.Name);
    Assert.Equal("Updated Cat", updated.Category);
  }

  [Fact]
  public async Task DeleteIngredient_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Create
    IngredientCreateDto createDto = new IngredientCreateDto { Name = "To Delete " + Guid.NewGuid() };
    HttpResponseMessage createRes = await _client.PostAsJsonAsync("/api/admin/ingredients", createDto);
    IngredientDto? created = await createRes.Content.ReadFromJsonAsync<IngredientDto>();

    // Delete
    HttpResponseMessage deleteRes = await _client.DeleteAsync($"/api/admin/ingredients/{created!.Id}");
    Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

    // Verify it's gone from Reference API
    // We need a separate client or clear headers, but reuse client is fine as admin can also read reference
    List<IngredientDto>? listRes = await _client.GetFromJsonAsync<List<IngredientDto>>("/api/reference/ingredients?query=To Delete");
    Assert.DoesNotContain(listRes!, i => i.Id == created.Id);
  }

  [Fact]
  public async Task CreateIngredient_DuplicateName_Conflict() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    string name = "Duplicate " + Guid.NewGuid();
    IngredientCreateDto dto = new IngredientCreateDto { Name = name };

    // First create
    await _client.PostAsJsonAsync("/api/admin/ingredients", dto);

    // Second create
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", dto);
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
  }
}

