using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Loges.PantryRaid.WebAPI.Tests.Integration;

public class UserIngredientsTests : IClassFixture<PantryRaidWebApplicationFactory> {
  private readonly PantryRaidWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private string? _adminToken;

  public UserIngredientsTests(PantryRaidWebApplicationFactory factory) {
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

  private async Task<string> CreateUserAndGetTokenAsync() {
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

  private async Task<List<IngredientDto>> CreateIngredientsAsync(int count) {
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    List<IngredientDto> ingredients = new List<IngredientDto>();
    for (int i = 0; i < count; i++) {
      string name = $"Ingredient_{Guid.NewGuid().ToString().Substring(0, 8)}";
      HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto { Name = name });
      response.EnsureSuccessStatusCode();
      ingredients.Add((await response.Content.ReadFromJsonAsync<IngredientDto>())!);
    }
    return ingredients;
  }

  [Fact]
  public async Task GetUserIngredients_EmptyInitially() {
    string userToken = await CreateUserAndGetTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

    HttpResponseMessage response = await client.GetAsync("/api/user/ingredients");
    response.EnsureSuccessStatusCode();
    List<IngredientDto>? ingredients = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
    Assert.Empty(ingredients!);
  }

  [Fact]
  public async Task ReplaceUserIngredients_Success() {
    // Setup ingredients
    List<IngredientDto> newIngredients = await CreateIngredientsAsync(3);
    List<int> ids = newIngredients.Select(i => i.Id).ToList();

    string userToken = await CreateUserAndGetTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

    // Replace
    HttpResponseMessage replaceResponse = await client.PutAsJsonAsync("/api/user/ingredients", ids);
    Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);

    // Verify
    HttpResponseMessage getResponse = await client.GetAsync("/api/user/ingredients");
    List<IngredientDto>? userIngredients = await getResponse.Content.ReadFromJsonAsync<List<IngredientDto>>();
    
    Assert.Equal(3, userIngredients!.Count);
    Assert.All(userIngredients, i => Assert.Contains(i.Id, ids));
  }

  [Fact]
  public async Task ReplaceUserIngredients_SortedByName() {
    // Setup ingredients: Z, A, M
    string token = await GetAdminTokenAsync();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    IngredientDto i1 = (await (await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto { Name = "Zucchini" })).Content.ReadFromJsonAsync<IngredientDto>())!;
    IngredientDto i2 = (await (await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto { Name = "Apple" })).Content.ReadFromJsonAsync<IngredientDto>())!;
    IngredientDto i3 = (await (await _client.PostAsJsonAsync("/api/admin/ingredients", new IngredientCreateDto { Name = "Mango" })).Content.ReadFromJsonAsync<IngredientDto>())!;

    List<int> ids = new List<int> { i1.Id, i2.Id, i3.Id };

    string userToken = await CreateUserAndGetTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

    await client.PutAsJsonAsync("/api/user/ingredients", ids);

    HttpResponseMessage getResponse = await client.GetAsync("/api/user/ingredients");
    List<IngredientDto>? userIngredients = await getResponse.Content.ReadFromJsonAsync<List<IngredientDto>>();

    Assert.Equal("Apple", userIngredients![0].Name);
    Assert.Equal("Mango", userIngredients[1].Name);
    Assert.Equal("Zucchini", userIngredients[2].Name);
  }

  [Fact]
  public async Task ReplaceUserIngredients_MaxLimit() {
    // We don't need to create actual ingredients in the DB because the service
    // checks the count of IDs *before* hitting the database for insertion.
    // We just need 101 unique integers.
    List<int> ids = Enumerable.Range(1, 101).ToList();

    string userToken = await CreateUserAndGetTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

    HttpResponseMessage response = await client.PutAsJsonAsync("/api/user/ingredients", ids);
    
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Contains("100 ingredients", error.GetProperty("message").GetString());
  }

  [Fact]
  public async Task ReplaceUserIngredients_IdempotentAndDistinct() {
    List<IngredientDto> newIngredients = await CreateIngredientsAsync(2);
    List<int> ids = newIngredients.Select(i => i.Id).ToList();
    // Add duplicates
    ids.Add(ids[0]);
    ids.Add(ids[1]);

    string userToken = await CreateUserAndGetTokenAsync();
    HttpClient client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

    await client.PutAsJsonAsync("/api/user/ingredients", ids);

    HttpResponseMessage getResponse = await client.GetAsync("/api/user/ingredients");
    List<IngredientDto>? userIngredients = await getResponse.Content.ReadFromJsonAsync<List<IngredientDto>>();

    Assert.Equal(2, userIngredients!.Count);
  }
}

