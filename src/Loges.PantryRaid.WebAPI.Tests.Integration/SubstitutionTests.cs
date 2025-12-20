using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.WebAPI.Controllers.Auth;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Integration {
  public class SubstitutionTests : IClassFixture<PantryRaidWebApplicationFactory> {
    private readonly PantryRaidWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string? _adminToken;
    private (string Email, string Password) _adminCredentials;

    public SubstitutionTests(PantryRaidWebApplicationFactory factory) {
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
      if (_adminToken != null) return _adminToken;

      HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest {
        Email = _adminCredentials.Email,
        Password = _adminCredentials.Password
      });
      response.EnsureSuccessStatusCode();
      LoginResult? result = await response.Content.ReadFromJsonAsync<LoginResult>();
      _adminToken = result!.Token!;
      return _adminToken;
    }

    private async Task<string> GetUserTokenAsync() {
      // Use a fresh client for user registration flow to avoid potential state issues
      using HttpClient userClient = _factory.CreateClient();
      string email = $"user_{Guid.NewGuid()}@example.com";
      string password = "UserPassword123!";

      await userClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest {
        Email = email,
        Password = password
      });

      HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/auth/login", new LoginRequest {
        Email = email,
        Password = password
      });
      response.EnsureSuccessStatusCode();
      LoginResult? result = await response.Content.ReadFromJsonAsync<LoginResult>();
      return result!.Token!;
    }

    private async Task<int> CreateIngredientAsync(string name) {
      IngredientCreateDto dto = new IngredientCreateDto { Name = name };
      HttpResponseMessage response = await _client.PostAsJsonAsync("/api/admin/ingredients", dto);
      response.EnsureSuccessStatusCode();
      IngredientDto? result = await response.Content.ReadFromJsonAsync<IngredientDto>();
      return result!.Id;
    }

    [Fact]
    public async Task Access_AsRegularUser_Forbidden() {
      // Arrange
      string token = await GetUserTokenAsync();
      using HttpClient userClient = _factory.CreateClient();
      userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      // Act & Assert
      // 1. Create Group
      HttpResponseMessage createRes = await userClient.PostAsJsonAsync("/api/admin/substitutions/groups", new CreateSubstitutionGroupRequest { TargetIngredientId = 1 });
      Assert.Equal(HttpStatusCode.Forbidden, createRes.StatusCode);

      // 2. Get All Groups
      HttpResponseMessage getAllRes = await userClient.GetAsync("/api/admin/substitutions/groups");
      Assert.Equal(HttpStatusCode.Forbidden, getAllRes.StatusCode);

      // 3. Delete Group
      HttpResponseMessage deleteRes = await userClient.DeleteAsync("/api/admin/substitutions/groups/1");
      Assert.Equal(HttpStatusCode.Forbidden, deleteRes.StatusCode);
    }

    [Fact]
    public async Task CreateSubstitutionGroup_AsAdmin_Success() {
      string token = await GetAdminTokenAsync();
      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      int ingId = await CreateIngredientAsync("Target Ing " + Guid.NewGuid());
      
      CreateSubstitutionGroupRequest req = new CreateSubstitutionGroupRequest { TargetIngredientId = ingId };
      HttpResponseMessage res = await _client.PostAsJsonAsync("/api/admin/substitutions/groups", req);
      
      Assert.Equal(HttpStatusCode.Created, res.StatusCode);
      SubstitutionGroupDto? group = await res.Content.ReadFromJsonAsync<SubstitutionGroupDto>();
      Assert.NotNull(group);
      Assert.Equal(ingId, group.TargetIngredientId);
      Assert.Empty(group.Options);
    }

    [Fact]
    public async Task CreateSubstitutionOption_WithIngredients_Success() {
      string token = await GetAdminTokenAsync();
      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      // Setup: Target Ingredient, Group, Sub Ingredient
      int targetId = await CreateIngredientAsync("Buttermilk " + Guid.NewGuid());
      int subId1 = await CreateIngredientAsync("Milk " + Guid.NewGuid());
      int subId2 = await CreateIngredientAsync("Vinegar " + Guid.NewGuid());

      CreateSubstitutionGroupRequest groupReq = new CreateSubstitutionGroupRequest { TargetIngredientId = targetId };
      HttpResponseMessage groupRes = await _client.PostAsJsonAsync("/api/admin/substitutions/groups", groupReq);
      SubstitutionGroupDto? group = await groupRes.Content.ReadFromJsonAsync<SubstitutionGroupDto>();

      // Act: Add Option
      CreateSubstitutionOptionRequest optReq = new CreateSubstitutionOptionRequest { 
        SubstitutionGroupId = group!.Id,
        Note = "Mix and let sit",
        IngredientIds = new[] { subId1, subId2 }
      };
      HttpResponseMessage optRes = await _client.PostAsJsonAsync($"/api/admin/substitutions/groups/{group.Id}/options", optReq);
      
      // Assert
      Assert.Equal(HttpStatusCode.OK, optRes.StatusCode);
      SubstitutionOptionDto? option = await optRes.Content.ReadFromJsonAsync<SubstitutionOptionDto>();
      Assert.NotNull(option);
      Assert.Equal("Mix and let sit", option.Note);
      Assert.Equal(2, option.Ingredients.Count());
      Assert.Contains(option.Ingredients, i => i.IngredientId == subId1);
      Assert.Contains(option.Ingredients, i => i.IngredientId == subId2);
    }

    [Fact]
    public async Task UpdateOptionIngredients_ReplacesList() {
      string token = await GetAdminTokenAsync();
      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      // Setup
      int targetId = await CreateIngredientAsync("Target Update " + Guid.NewGuid());
      int sub1 = await CreateIngredientAsync("Sub1 " + Guid.NewGuid());
      int sub2 = await CreateIngredientAsync("Sub2 " + Guid.NewGuid());

      HttpResponseMessage groupRes = await _client.PostAsJsonAsync("/api/admin/substitutions/groups", new CreateSubstitutionGroupRequest { TargetIngredientId = targetId });
      SubstitutionGroupDto? group = await groupRes.Content.ReadFromJsonAsync<SubstitutionGroupDto>();

      HttpResponseMessage optRes = await _client.PostAsJsonAsync($"/api/admin/substitutions/groups/{group!.Id}/options", new CreateSubstitutionOptionRequest  { 
        SubstitutionGroupId = group.Id,
        IngredientIds = new[] { sub1 }
      });
      SubstitutionOptionDto? option = await optRes.Content.ReadFromJsonAsync<SubstitutionOptionDto>();

      // Act: Replace sub1 with sub2
      ReplaceSubstitutionIngredientsRequest replaceReq = new ReplaceSubstitutionIngredientsRequest { IngredientIds = new[] { sub2 } };
      HttpResponseMessage replaceRes = await _client.PutAsJsonAsync($"/api/admin/substitutions/options/{option!.Id}/ingredients", replaceReq);
      Assert.Equal(HttpStatusCode.NoContent, replaceRes.StatusCode);

      // Verify
      SubstitutionGroupDto? groupCheck = await _client.GetFromJsonAsync<SubstitutionGroupDto>($"/api/admin/substitutions/groups/{group.Id}");
      SubstitutionOptionDto? updatedOption = groupCheck!.Options.First(o => o.Id == option.Id);
      Assert.Single(updatedOption.Ingredients);
      Assert.Equal(sub2, updatedOption.Ingredients.First().IngredientId);
    }

    [Fact]
    public async Task DeleteGroup_DeletesOptionsCascade() {
       string token = await GetAdminTokenAsync();
      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      int targetId = await CreateIngredientAsync("Target Delete " + Guid.NewGuid());
      HttpResponseMessage groupRes = await _client.PostAsJsonAsync("/api/admin/substitutions/groups", new CreateSubstitutionGroupRequest { TargetIngredientId = targetId });
      SubstitutionGroupDto? group = await groupRes.Content.ReadFromJsonAsync<SubstitutionGroupDto>();

      HttpResponseMessage optRes = await _client.PostAsJsonAsync($"/api/admin/substitutions/groups/{group!.Id}/options", new CreateSubstitutionOptionRequest { 
        SubstitutionGroupId = group.Id,
        Note = "Temp"
      });
      SubstitutionOptionDto? option = await optRes.Content.ReadFromJsonAsync<SubstitutionOptionDto>();

      // Delete Group
      HttpResponseMessage delRes = await _client.DeleteAsync($"/api/admin/substitutions/groups/{group.Id}");
      Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

      // Verify Group Gone
      HttpResponseMessage getRes = await _client.GetAsync($"/api/admin/substitutions/groups/{group.Id}");
      Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }
  }
}
