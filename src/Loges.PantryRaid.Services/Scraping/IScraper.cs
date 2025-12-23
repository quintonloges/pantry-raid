namespace Loges.PantryRaid.Services.Scraping;

public interface IScraper {
  bool CanHandle(string url);
  Task<ScrapedRecipe> ScrapeAsync(string url);
}
