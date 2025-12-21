using Loges.PantryRaid.Dtos;

namespace Loges.PantryRaid.Services.Interfaces {
  public interface ISubstitutionEvaluator {
    EvaluationResult Evaluate(
      IEnumerable<int> requiredIngredientIds,
      IEnumerable<int> userIngredientIds,
      IEnumerable<SubstitutionGroupDto> rules);
  }

  public class EvaluationResult {
    public Dictionary<int, IngredientMatch> Matches { get; set; } = new();
    public HashSet<int> MissingIngredientIds { get; set; } = new();
  }

  public class IngredientMatch {
    public int IngredientId { get; set; }
    public IngredientMatchType Type { get; set; }
    public SubstitutionPath? Substitution { get; set; }
  }

  public enum IngredientMatchType {
    Exact,
    Substitution
  }

  public class SubstitutionPath {
    public int OptionId { get; set; } // For deterministic tie-breaking
    public int TargetIngredientId { get; set; }
    public string? Note { get; set; }
    public List<IngredientMatch> Sources { get; set; } = new();
    
    // Helper for cost calculation
    public int Depth => 1 + (Sources.Any() ? Sources.Max(s => s.Substitution?.Depth ?? 0) : 0);
  }
}

