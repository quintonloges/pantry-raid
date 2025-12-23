namespace Loges.PantryRaid.Services.Scraping;

public record ScrapedRecipe(
  string Title,
  string SourceUrl,
  string SourceRecipeId, // Can be slug or ID from source
  string ShortDescription,
  string ImageUrl,
  int TotalTimeMinutes,
  int Servings,
  List<string> Ingredients
);
