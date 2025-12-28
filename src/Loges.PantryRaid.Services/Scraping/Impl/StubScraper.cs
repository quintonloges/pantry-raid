namespace Loges.PantryRaid.Services.Scraping.Impl;

public class StubScraper : IScraper {
  private const string DOMAIN = "example-recipe-site.com";

  public bool CanHandle(string url) {
    return url.Contains(DOMAIN, StringComparison.OrdinalIgnoreCase);
  }

  public Task<ScrapedRecipe> ScrapeAsync(string url) {
    // Return fixed payload
    ScrapedRecipe result = new ScrapedRecipe(
      Title: "Stub Scraped Recipe",
      SourceUrl: url,
      SourceRecipeId: "stub-123",
      ShortDescription: "This is a recipe scraped by the stub scraper.",
      ImageUrl: "https://via.placeholder.com/150",
      TotalTimeMinutes: 30,
      Servings: 4,
      Ingredients: new List<string> {
        "1 cup Unmapped Ingredient A",
        "2 tbsp Unmapped Ingredient B",
        "Salt to taste"
      }
    );

    return Task.FromResult(result);
  }
}
