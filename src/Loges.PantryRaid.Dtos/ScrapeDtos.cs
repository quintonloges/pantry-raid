namespace Loges.PantryRaid.Dtos;

public class ScrapeRequestDto {
  public string Url { get; set; } = string.Empty;
}

public enum ScrapeResultStatus {
  Success,
  Skipped,
  Failed
}

public class ScrapeResultDto {
  public ScrapeResultStatus Status { get; set; }
  public string Message { get; set; } = string.Empty;
  public int? RecipeId { get; set; }
}
