using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.WebAPI.Data;

public class DbSeeder : IDbSeeder {
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly UserManager<AppUser> _userManager;
  private readonly AppDbContext _context;
  private readonly IConfiguration _configuration;

  public DbSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    AppDbContext context,
    IConfiguration configuration) {
    _roleManager = roleManager;
    _userManager = userManager;
    _context = context;
    _configuration = configuration;
  }

  public async Task SeedAsync() {
    await SeedAuthAsync();
    await SeedDomainDataAsync();
  }

  private async Task SeedAuthAsync() {
    // 1. Seed Roles
    string[] roleNames = { "Admin", "User" };
    foreach (string roleName in roleNames) {
      if (!await _roleManager.RoleExistsAsync(roleName)) {
        await _roleManager.CreateAsync(new IdentityRole(roleName));
      }
    }

    // 2. Seed Admin User
    string? adminEmail = _configuration["ADMIN_EMAIL"] ?? "admin@pantryraid.local";
    string? adminPassword = _configuration["ADMIN_PASSWORD"] ?? "Admin123!";

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword)) {
      AppUser? adminUser = await _userManager.FindByEmailAsync(adminEmail);

      if (adminUser == null) {
        adminUser = new AppUser {
          UserName = adminEmail,
          Email = adminEmail,
          EmailConfirmed = true // Assume confirmed if seeded
        };

        IdentityResult createResult = await _userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded) {
          await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
      } else {
        // Ensure existing admin user has Admin role
        if (!await _userManager.IsInRoleAsync(adminUser, "Admin")) {
          await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
      }
    }
  }

  private async Task SeedDomainDataAsync() {
    if (await _context.Ingredients.AnyAsync()) {
      // Assume data is already seeded if ingredients exist
      return;
    }

    // --- 1. Reference Data ---
    List<Cuisine> cuisines = new List<Cuisine> {
      new() { Name = "Italian" },
      new() { Name = "Mexican" },
      new() { Name = "Asian" },
      new() { Name = "American" },
      new() { Name = "Mediterranean" }
    };
    await _context.Cuisines.AddRangeAsync(cuisines);

    List<Protein> proteins = new List<Protein> {
      new() { Name = "Chicken" },
      new() { Name = "Beef" },
      new() { Name = "Pork" },
      new() { Name = "Fish" },
      new() { Name = "Vegetarian" }
    };
    await _context.Proteins.AddRangeAsync(proteins);

    List<DietaryTag> dietaryTags = new List<DietaryTag> {
      new() { Name = "Gluten-Free" },
      new() { Name = "Dairy-Free" },
      new() { Name = "Vegan" },
      new() { Name = "Vegetarian" },
      new() { Name = "Low-Carb" }
    };
    await _context.DietaryTags.AddRangeAsync(dietaryTags);

    await _context.SaveChangesAsync(); // Save to get IDs

    // --- 2. Ingredients ---
    // Creating a dictionary to easily reference them later
    Dictionary<string, Ingredient> ingredients = new Dictionary<string, Ingredient>();

    string[] ingredientNames = new[] {
      // Essentials
      "Salt", "Black Pepper", "Olive Oil", "Vegetable Oil", "Garlic", "Onion", "Butter", "Flour", "Sugar",
      // Produce
      "Lemon", "Tomato", "Potato", "Carrot", "Bell Pepper", "Spinach",
      // Proteins
      "Chicken Breast", "Ground Beef", "Eggs", "Bacon",
      // Pantry
      "Rice", "Pasta", "Tomato Sauce", "Soy Sauce", "Chicken Broth", "Canned Beans", "Breadcrumbs",
      // Dairy
      "Milk", "Parmesan Cheese", "Cheddar Cheese", "Heavy Cream"
    };

    foreach (string name in ingredientNames) {
      Ingredient ingredient = new Ingredient {
        Name = name,
        Slug = name.ToLower().Replace(" ", "-"),
        Category = "General" // Simplified category
      };
      ingredients[name] = ingredient;
      await _context.Ingredients.AddAsync(ingredient);
    }

    await _context.SaveChangesAsync();

    // --- 3. Ingredient Groups ---
    IngredientGroup essentialsGroup = new IngredientGroup { Name = "Kitchen Essentials", Description = "Basic items found in most kitchens" };
    await _context.IngredientGroups.AddAsync(essentialsGroup);
    await _context.SaveChangesAsync();

    string[] essentials = new[] { "Salt", "Black Pepper", "Olive Oil", "Vegetable Oil", "Garlic", "Onion", "Flour", "Sugar" };
    foreach (string name in essentials) {
      if (ingredients.TryGetValue(name, out Ingredient? ing)) {
        await _context.IngredientGroupItems.AddAsync(new IngredientGroupItem {
          IngredientGroupId = essentialsGroup.Id,
          IngredientId = ing.Id
        });
      }
    }
    await _context.SaveChangesAsync();

    // --- 4. Recipe Sources ---
    RecipeSource source = new RecipeSource {
      Name = "Pantry Raid Originals",
      BaseUrl = "https://pantryraid.local",
      IsActive = true,
      ScraperKey = "Internal"
    };
    await _context.RecipeSources.AddAsync(source);
    await _context.SaveChangesAsync();

    // --- 5. Recipes ---

    // Recipe 1: Garlic Butter Pasta
    Recipe pastaRecipe = new Recipe {
      Title = "Simple Garlic Butter Pasta",
      RecipeSourceId = source.Id,
      SourceUrl = "https://pantryraid.local/recipes/garlic-butter-pasta",
      SourceRecipeId = "garlic-butter-pasta",
      ShortDescription = "A quick and easy pasta dish perfect for busy weeknights.",
      TotalTimeMinutes = 15,
      Servings = 2,
      ImageUrl = "https://placehold.co/600x400?text=Garlic+Pasta",
      ScrapedAt = DateTime.UtcNow,
      ScrapeStatus = "Success"
    };
    await _context.Recipes.AddAsync(pastaRecipe);
    await _context.SaveChangesAsync();

    // Link Metadata
    await _context.RecipeCuisines.AddAsync(new RecipeCuisine { RecipeId = pastaRecipe.Id, CuisineId = cuisines.First(c => c.Name == "Italian").Id });
    await _context.RecipeProteins.AddAsync(new RecipeProtein { RecipeId = pastaRecipe.Id, ProteinId = proteins.First(c => c.Name == "Vegetarian").Id });

    // Link Ingredients
    (string, double?, string, int)[] pastaIngredients = new[] {
        ("Pasta", (double?)8.0, "oz", 1),
        ("Butter", (double?)2.0, "tbsp", 2),
        ("Garlic", (double?)2.0, "cloves, minced", 3),
        ("Salt", (double?)null, "to taste", 4), // Explicit cast needed here as null is not double?
        ("Black Pepper", (double?)null, "to taste", 5),
        ("Parmesan Cheese", (double?)0.25, "cup, grated", 6)
    };

    // Need to handle nulls in the tuple array, which C# might infer as nullable double only if one element is null.
    // The previous code had (double?)null so type inference should be fine, but let's be explicit in loop.

    foreach ((string name, double? qty, string unit, int order) in pastaIngredients) {
      if (ingredients.TryGetValue(name, out Ingredient? ing)) {
        await _context.RecipeIngredients.AddAsync(new RecipeIngredient {
          RecipeId = pastaRecipe.Id,
          IngredientId = ing.Id,
          Quantity = qty,
          Unit = unit,
          OriginalText = $"{qty} {unit} {name}",
          OrderIndex = order
        });
      }
    }

    // Recipe 2: Chicken Fried Rice
    Recipe riceRecipe = new Recipe {
      Title = "Classic Chicken Fried Rice",
      RecipeSourceId = source.Id,
      SourceUrl = "https://pantryraid.local/recipes/chicken-fried-rice",
      SourceRecipeId = "chicken-fried-rice",
      ShortDescription = "Better than takeout fried rice using leftover ingredients.",
      TotalTimeMinutes = 20,
      Servings = 4,
      ImageUrl = "https://placehold.co/600x400?text=Fried+Rice",
      ScrapedAt = DateTime.UtcNow,
      ScrapeStatus = "Success"
    };
    await _context.Recipes.AddAsync(riceRecipe);
    await _context.SaveChangesAsync();

    // Link Metadata
    await _context.RecipeCuisines.AddAsync(new RecipeCuisine { RecipeId = riceRecipe.Id, CuisineId = cuisines.First(c => c.Name == "Asian").Id });
    await _context.RecipeProteins.AddAsync(new RecipeProtein { RecipeId = riceRecipe.Id, ProteinId = proteins.First(c => c.Name == "Chicken").Id });

    // Link Ingredients
    (string, double?, string, int)[] riceIngredients = new[] {
        ("Rice", (double?)2.0, "cups, cooked", 1),
        ("Chicken Breast", (double?)1.0, "cup, diced", 2),
        ("Eggs", (double?)2.0, "large", 3),
        ("Vegetable Oil", (double?)2.0, "tbsp", 4),
        ("Soy Sauce", (double?)3.0, "tbsp", 5),
        ("Garlic", (double?)1.0, "tsp, minced", 6),
        ("Onion", (double?)1.0, "small, diced", 7)
    };

    foreach ((string name, double? qty, string unit, int order) in riceIngredients) {
      if (ingredients.TryGetValue(name, out Ingredient? ing)) {
        await _context.RecipeIngredients.AddAsync(new RecipeIngredient {
          RecipeId = riceRecipe.Id,
          IngredientId = ing.Id,
          Quantity = qty,
          Unit = unit,
          OriginalText = $"{qty} {unit} {name}",
          OrderIndex = order
        });
      }
    }

    // --- 6. Substitutions ---
    // Example: Vegetable Oil -> Olive Oil
    if (ingredients.TryGetValue("Vegetable Oil", out Ingredient? vegOil) && ingredients.TryGetValue("Olive Oil", out Ingredient? oliveOil)) {
      SubstitutionGroup subGroup = new SubstitutionGroup {
        TargetIngredientId = vegOil.Id
      };
      await _context.SubstitutionGroups.AddAsync(subGroup);
      await _context.SaveChangesAsync();

      SubstitutionOption subOption = new SubstitutionOption {
        SubstitutionGroupId = subGroup.Id,
        Note = "Olive Oil Substitute"
      };
      await _context.SubstitutionOptions.AddAsync(subOption);
      await _context.SaveChangesAsync();

      await _context.SubstitutionOptionIngredients.AddAsync(new SubstitutionOptionIngredient {
        SubstitutionOptionId = subOption.Id,
        IngredientId = oliveOil.Id
        // QuantityMultiplier not supported in MVP
      });
    }

    await _context.SaveChangesAsync();
  }
}
