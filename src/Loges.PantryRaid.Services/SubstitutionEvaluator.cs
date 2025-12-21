using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services.Interfaces;

namespace Loges.PantryRaid.Services {
  public class SubstitutionEvaluator : ISubstitutionEvaluator {
    public EvaluationResult Evaluate(
      IEnumerable<int> requiredIngredientIds,
      IEnumerable<int> userIngredientIds,
      IEnumerable<SubstitutionGroupDto> rules) {
      
      EvaluationResult result = new EvaluationResult();
      HashSet<int> userIngredients = new HashSet<int>(userIngredientIds);
      Dictionary<int, SubstitutionGroupDto> ruleMap = rules.GroupBy(r => r.TargetIngredientId)
        .ToDictionary(g => g.Key, g => g.First()); 
        // Assumption: One group per target ingredient as per spec

      foreach (int requiredId in requiredIngredientIds) {
        // Short circuit for exact matches
        if (userIngredients.Contains(requiredId)) {
          result.Matches[requiredId] = new IngredientMatch {
            IngredientId = requiredId,
            Type = IngredientMatchType.Exact
          };
          continue;
        }

        // Attempt substitution
        IngredientMatch? match = Resolve(requiredId, userIngredients, ruleMap, new HashSet<int>());
        
        if (match != null) {
          result.Matches[requiredId] = match;
        } else {
          result.MissingIngredientIds.Add(requiredId);
        }
      }

      return result;
    }

    private IngredientMatch? Resolve(
      int targetId, 
      HashSet<int> userIngredients, 
      Dictionary<int, SubstitutionGroupDto> ruleMap,
      HashSet<int> visited) {
      
      // 1. Exact Match Check (Base Case)
      if (userIngredients.Contains(targetId)) {
        return new IngredientMatch {
          IngredientId = targetId,
          Type = IngredientMatchType.Exact
        };
      }

      // 2. Cycle Detection
      if (visited.Contains(targetId)) {
        return null;
      }

      // 3. Find Rules
      if (!ruleMap.TryGetValue(targetId, out SubstitutionGroupDto? group)) {
        return null;
      }

      // 4. Evaluate Options
      // We want the "best" option. Criteria:
      // - Must be fully satisfied
      // - Prefer shorter chains (Depth)
      // - Deterministic tie-breaker (Option ID)
      
      visited.Add(targetId);

      SubstitutionPath? bestPath = null;

      // Sort options by ID to ensure processing order if we rely on "first found" logic,
      // or just use ID comparison in IsBetter.
      foreach (SubstitutionOptionDto option in group.Options) {
        List<IngredientMatch> optionMatches = new List<IngredientMatch>();
        bool possible = true;

        foreach (SubstitutionOptionIngredientDto requiredSubIngredient in option.Ingredients) {
          IngredientMatch? subMatch = Resolve(requiredSubIngredient.IngredientId, userIngredients, ruleMap, visited);
          if (subMatch == null) {
            possible = false;
            break;
          }
          optionMatches.Add(subMatch);
        }

        if (possible) {
          SubstitutionPath currentPath = new SubstitutionPath {
            OptionId = option.Id,
            TargetIngredientId = targetId,
            Note = option.Note,
            Sources = optionMatches
          };

          if (bestPath == null || IsBetter(currentPath, bestPath)) {
            bestPath = currentPath;
          }
        }
      }

      visited.Remove(targetId); // Backtrack

      if (bestPath != null) {
        return new IngredientMatch {
          IngredientId = targetId,
          Type = IngredientMatchType.Substitution,
          Substitution = bestPath
        };
      }

      return null;
    }

    private bool IsBetter(SubstitutionPath current, SubstitutionPath best) {
      // Lower depth is better
      if (current.Depth < best.Depth) {
        return true;
      }
      if (current.Depth > best.Depth) {
        return false;
      }

      // Tie-breaker: Determinism using OptionId (lower ID wins)
      return current.OptionId < best.OptionId;
    }
  }
}
