using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loges.PantryRaid.Services.Scraping;

public class ScrapingService : IScrapingService {
  private readonly AppDbContext _context;
  private readonly IEnumerable<IScraper> _scrapers;
  private readonly ILogger<ScrapingService> _logger;

  public ScrapingService(
    AppDbContext context,
    IEnumerable<IScraper> scrapers,
    ILogger<ScrapingService> logger) {
    _context = context;
    _scrapers = scrapers;
    _logger = logger;
  }

  public async Task<ScrapeResultDto> ScrapeRecipeAsync(string url) {
    // 1. Validate URL and Find Scraper
    if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = "Invalid URL." };
    }

    IScraper? scraper = _scrapers.FirstOrDefault(s => s.CanHandle(url));
    if (scraper == null) {
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = "No scraper found for this source." };
    }

    // 2. Determine Source by BaseUrl
    string baseUrl = uri.Host; // Simplification. In real world, might need scheme + host.
    // For MVP, matching part of BaseUrl in DB is probably safest or we rely on exact match if we store "https://domain.com"
    // Let's assume the DB has "https://example-recipe-site.com" or just "example-recipe-site.com"
    // Let's try to match by Contains.
    
    RecipeSource? source = await _context.RecipeSources
      .FirstOrDefaultAsync(s => url.Contains(s.BaseUrl));

    if (source == null) {
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = $"Recipe source not configured for {uri.Host}" };
    }

    if (!source.IsActive) {
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = "Recipe source is inactive." };
    }

    // 3. Idempotency Check
    Recipe? existing = await _context.Recipes
      .FirstOrDefaultAsync(r => r.SourceUrl == url);

    if (existing != null) {
      return new ScrapeResultDto { 
        Status = ScrapeResultStatus.Skipped, 
        Message = "Recipe already exists.", 
        RecipeId = existing.Id 
      };
    }

    // 4. Scrape
    ScrapedRecipe scrapedData;
    try {
      scrapedData = await scraper.ScrapeAsync(url);
    } catch (Exception ex) {
      _logger.LogError(ex, "Scraping failed for {Url}", url);
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = $"Scraping error: {ex.Message}" };
    }

    // 5. Save Recipe
    Recipe recipe = new Recipe {
      Title = scrapedData.Title,
      RecipeSourceId = source.Id,
      SourceUrl = url,
      SourceRecipeId = scrapedData.SourceRecipeId,
      ShortDescription = scrapedData.ShortDescription,
      ImageUrl = scrapedData.ImageUrl,
      TotalTimeMinutes = scrapedData.TotalTimeMinutes,
      Servings = scrapedData.Servings,
      ScrapeStatus = "Success",
      ScrapedAt = DateTime.UtcNow
      // RawHtml? Maybe later.
    };

    // 6. Process Ingredients
    int index = 0;
    foreach (string line in scrapedData.Ingredients) {
      RecipeIngredient recipeIngredient = new RecipeIngredient {
        OriginalText = line,
        OrderIndex = index++,
        IsOptional = false, // Default
        IngredientId = null // Starts unmapped
      };
      recipe.Ingredients.Add(recipeIngredient);
    }

    try {
      _context.Recipes.Add(recipe);
      await _context.SaveChangesAsync(); // Save to get Recipe ID and Ingredient IDs
    } catch (Exception ex) {
       _logger.LogError(ex, "Failed to save recipe for {Url}", url);
      return new ScrapeResultDto { Status = ScrapeResultStatus.Failed, Message = $"Database error: {ex.Message}" };
    }

    // 7. Create Unmapped Ingredients
    // Since all new ingredients are unmapped by default in this MVP stub:
    List<UnmappedIngredient> unmappedItems = recipe.Ingredients.Select(ri => new UnmappedIngredient {
      RecipeId = recipe.Id,
      RecipeSourceId = source.Id,
      OriginalText = ri.OriginalText,
      Status = UnmappedIngredientStatus.New
    }).ToList();

    if (unmappedItems.Any()) {
      _context.UnmappedIngredients.AddRange(unmappedItems);
      await _context.SaveChangesAsync();
    }

    return new ScrapeResultDto {
      Status = ScrapeResultStatus.Success,
      Message = "Recipe scraped and saved.",
      RecipeId = recipe.Id
    };
  }
}
