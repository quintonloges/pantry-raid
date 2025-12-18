using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class IngredientGroupTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string? _adminToken;
  private (string Email, string Password) _adminCredentials;

  public IngredientGroupTests(PantryRaidWebApplicationFactory factory) {
    _factory = factory;
    
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
    
    _adminCredentials = (adminEmail, adminPassword);
  }

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

  private async Task<int> CreateIngredientAsync(string name) {
    // Helper to create ingredient using existing Admin API
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto { Name = name });
    response.EnsureSuccessStatusCode();
    IngredientDto? result = await response.Content.ReadFromJsonAsync<IngredientDto>();
    return result!.Id;
  }

  [Fact]
  public async Task CreateGroup_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    int ing1 = await CreateIngredientAsync("GroupIng1_" + Guid.NewGuid());
    int ing2 = await CreateIngredientAsync("GroupIng2_" + Guid.NewGuid());

    CreateIngredientGroupDto createDto = new CreateIngredientGroupDto {
      Name = "My Group " + Guid.NewGuid(),
      Description = "Test Desc",
      InitialIngredientIds = new List<int> { ing1, ing2 }
    };

    HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredient-groups", createDto);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    
    IngredientGroupDto? created = await response.Content.ReadFromJsonAsync<IngredientGroupDto>();
    Assert.NotNull(created);
    Assert.Equal(createDto.Name, created.Name);
    Assert.Equal(2, created.Items.Count);
    Assert.Equal(ing1, created.Items[0].IngredientId);
    Assert.Equal(0, created.Items[0].OrderIndex);
    Assert.Equal(ing2, created.Items[1].IngredientId);
    Assert.Equal(1, created.Items[1].OrderIndex);
  }

  [Fact]
  public async Task UpdateGroup_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    CreateIngredientGroupDto createDto = new CreateIngredientGroupDto { Name = "Update Me " + Guid.NewGuid() };
    HttpResponseMessage createRes = await _client.PostAsJsonAsync("/api/admin/ingredient-groups", createDto);
    IngredientGroupDto? created = await createRes.Content.ReadFromJsonAsync<IngredientGroupDto>();

    UpdateIngredientGroupDto updateDto = new UpdateIngredientGroupDto { Name = "Updated Name " + Guid.NewGuid(), Description = "New Desc" };
    HttpResponseMessage updateRes = await _client.PutAsJsonAsync($"/api/admin/ingredient-groups/{created!.Id}", updateDto);
    
    Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
    IngredientGroupDto? updated = await updateRes.Content.ReadFromJsonAsync<IngredientGroupDto>();
    Assert.Equal(updateDto.Name, updated!.Name);
    Assert.Equal(updateDto.Description, updated.Description);
  }

  [Fact]
  public async Task SetGroupItems_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    int ing1 = await CreateIngredientAsync("Item1_" + Guid.NewGuid());
    int ing2 = await CreateIngredientAsync("Item2_" + Guid.NewGuid());

    // Create group with ing1
    CreateIngredientGroupDto createDto = new CreateIngredientGroupDto { 
      Name = "Item Group " + Guid.NewGuid(),
      InitialIngredientIds = new List<int> { ing1 }
    };
    HttpResponseMessage createRes = await _client.PostAsJsonAsync("/api/admin/ingredient-groups", createDto);
    IngredientGroupDto? created = await createRes.Content.ReadFromJsonAsync<IngredientGroupDto>();
    Assert.Single(created!.Items);
    Assert.Equal(ing1, created.Items[0].IngredientId);

    // Replace with ing2, ing1 (reversed order)
    SetIngredientGroupItemsDto setDto = new SetIngredientGroupItemsDto { IngredientIds = new List<int> { ing2, ing1 } };
    HttpResponseMessage setRes = await _client.PutAsJsonAsync($"/api/admin/ingredient-groups/{created.Id}/items", setDto);
    Assert.Equal(HttpStatusCode.NoContent, setRes.StatusCode);

    // Verify
    HttpResponseMessage getRes = await _client.GetAsync($"/api/admin/ingredient-groups/{created.Id}");
    IngredientGroupDto? final = await getRes.Content.ReadFromJsonAsync<IngredientGroupDto>();
    
    Assert.Equal(2, final!.Items.Count);
    Assert.Equal(ing2, final.Items[0].IngredientId); // First in list
    Assert.Equal(0, final.Items[0].OrderIndex);
    Assert.Equal(ing1, final.Items[1].IngredientId); // Second in list
    Assert.Equal(1, final.Items[1].OrderIndex);
  }

  [Fact]
  public async Task DeleteGroup_AsAdmin_Success() {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    CreateIngredientGroupDto createDto = new CreateIngredientGroupDto { Name = "Delete Me " + Guid.NewGuid() };
    HttpResponseMessage createRes = await _client.PostAsJsonAsync("/api/admin/ingredient-groups", createDto);
    IngredientGroupDto? created = await createRes.Content.ReadFromJsonAsync<IngredientGroupDto>();

    HttpResponseMessage deleteRes = await _client.DeleteAsync($"/api/admin/ingredient-groups/{created!.Id}");
    Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

    HttpResponseMessage getRes = await _client.GetAsync($"/api/admin/ingredient-groups/{created.Id}");
    Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
  }
}

