using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces;

public interface IScrapingService {
  Task<ScrapeResultDto> ScrapeRecipeAsync(string url);
}
