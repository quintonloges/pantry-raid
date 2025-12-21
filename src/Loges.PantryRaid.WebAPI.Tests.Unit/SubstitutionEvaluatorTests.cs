using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.Services;
using Loges.PantryRaid.Services.Interfaces;
using Xunit;

namespace Loges.PantryRaid.WebAPI.Tests.Unit {
  public class SubstitutionEvaluatorTests {
    private readonly SubstitutionEvaluator _evaluator;

    public SubstitutionEvaluatorTests() {
      _evaluator = new SubstitutionEvaluator();
    }

    [Fact]
    public void Evaluate_ExactMatch_ReturnsMatch() {
      int[] required = new[] { 1, 2 };
      int[] user = new[] { 1, 2, 3 };
      IEnumerable<SubstitutionGroupDto> rules = Enumerable.Empty<SubstitutionGroupDto>();

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Empty(result.MissingIngredientIds);
      Assert.Equal(2, result.Matches.Count);
      Assert.Equal(IngredientMatchType.Exact, result.Matches[1].Type);
      Assert.Equal(IngredientMatchType.Exact, result.Matches[2].Type);
    }

    [Fact]
    public void Evaluate_SimpleSubstitution_ReturnsSubstitutionMatch() {
      // Buttermilk (10) <- Milk (1) + Lemon Juice (2)
      int[] required = new[] { 10 };
      int[] user = new[] { 1, 2 };
      SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 10,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 100,
              Ingredients = new[] {
                new SubstitutionOptionIngredientDto { IngredientId = 1 },
                new SubstitutionOptionIngredientDto { IngredientId = 2 }
              }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Empty(result.MissingIngredientIds);
      Assert.True(result.Matches.ContainsKey(10));
      IngredientMatch match = result.Matches[10];
      Assert.Equal(IngredientMatchType.Substitution, match.Type);
      Assert.NotNull(match.Substitution);
      Assert.Equal(10, match.Substitution.TargetIngredientId);
      Assert.Equal(2, match.Substitution.Sources.Count);
      Assert.Contains(match.Substitution.Sources, s => s.IngredientId == 1 && s.Type == IngredientMatchType.Exact);
      Assert.Contains(match.Substitution.Sources, s => s.IngredientId == 2 && s.Type == IngredientMatchType.Exact);
    }

    [Fact]
    public void Evaluate_ChainedSubstitution_ReturnsMatch() {
      // C (3) <- B (2)
      // B (2) <- A (1)
      // Have: A (1)
      // Need: C (3)
      int[] required = new[] { 3 };
      int[] user = new[] { 1 };
      SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 3,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 300,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 2 } }
            }
          }
        },
        new SubstitutionGroupDto {
          TargetIngredientId = 2,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 200,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 1 } }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Empty(result.MissingIngredientIds);
      IngredientMatch match = result.Matches[3];
      Assert.Equal(IngredientMatchType.Substitution, match.Type);
      Assert.NotNull(match.Substitution);
      
      IngredientMatch sourceB = match.Substitution.Sources.Single();
      Assert.Equal(2, sourceB.IngredientId);
      Assert.Equal(IngredientMatchType.Substitution, sourceB.Type);
      
      IngredientMatch sourceA = sourceB.Substitution!.Sources.Single();
      Assert.Equal(1, sourceA.IngredientId);
      Assert.Equal(IngredientMatchType.Exact, sourceA.Type);
    }

    [Fact]
    public void Evaluate_CycleDetection_PreventsInfiniteLoop() {
      // A (1) <- B (2)
      // B (2) <- A (1)
      // Have: Nothing
      // Need: A (1)
      int[] required = new[] { 1 };
      int[] user = Array.Empty<int>();
      SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 1,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 100,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 2 } }
            }
          }
        },
        new SubstitutionGroupDto {
          TargetIngredientId = 2,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 200,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 1 } }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Single(result.MissingIngredientIds);
      Assert.Equal(1, result.MissingIngredientIds.First());
      Assert.False(result.Matches.ContainsKey(1));
    }

    [Fact]
    public void Evaluate_PreferShortestChain_ReturnsBestOption() {
      // Target (10)
      // Option 1: Via A (1) (Cost 1)
      // Option 2: Via B (2) -> C (3) (Cost 2)
      // Have: A (1), C (3)
      int[] required = new[] { 10 };
      int[] user = new[] { 1, 3 };
      SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 10,
          Options = new[] {
            new SubstitutionOptionDto { // Longer path option
              Id = 101, 
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 2 } }
            },
            new SubstitutionOptionDto { // Shorter path option
              Id = 102,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 1 } }
            }
          }
        },
        new SubstitutionGroupDto {
          TargetIngredientId = 2,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 200,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 3 } }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Empty(result.MissingIngredientIds);
      IngredientMatch match = result.Matches[10];
      Assert.Equal(IngredientMatchType.Substitution, match.Type);
      
      // Should pick Option 102 (Via A) because depth is 1, vs Option 101 (Via B->C) which is depth 2
      Assert.Equal(102, match.Substitution!.OptionId);
      Assert.Equal(1, match.Substitution.Sources.Single().IngredientId);
    }

    [Fact]
    public void Evaluate_DeterministicSelection_ReturnsLowestIdWhenDepthsEqual() {
      // Target (10)
      // Option 101: Via A (1)
      // Option 102: Via B (2)
      // Have: A (1), B (2)
      int[] required = new[] { 10 };
      int[] user = new[] { 1, 2 };
      SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 10,
          Options = new[] {
            new SubstitutionOptionDto { 
              Id = 101, 
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 1 } }
            },
            new SubstitutionOptionDto { 
              Id = 102,
              Ingredients = new[] { new SubstitutionOptionIngredientDto { IngredientId = 2 } }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Empty(result.MissingIngredientIds);
      IngredientMatch match = result.Matches[10];
      Assert.Equal(101, match.Substitution!.OptionId);
    }
    
    [Fact]
    public void Evaluate_MissingIngredient_ReturnsMissing() {
      int[] required = new[] { 1 };
      int[] user = Array.Empty<int>();
      IEnumerable<SubstitutionGroupDto> rules = Enumerable.Empty<SubstitutionGroupDto>();

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Single(result.MissingIngredientIds);
      Assert.Equal(1, result.MissingIngredientIds.First());
      Assert.False(result.Matches.ContainsKey(1));
    }

    [Fact]
    public void Evaluate_PartialSubstitutionFailure_ReturnsMissing() {
       // Target (10) <- A (1) + B (2)
       // Have: A (1) only
       int[] required = new[] { 10 };
       int[] user = new[] { 1 };
       SubstitutionGroupDto[] rules = new[] {
        new SubstitutionGroupDto {
          TargetIngredientId = 10,
          Options = new[] {
            new SubstitutionOptionDto {
              Id = 100,
              Ingredients = new[] {
                new SubstitutionOptionIngredientDto { IngredientId = 1 },
                new SubstitutionOptionIngredientDto { IngredientId = 2 }
              }
            }
          }
        }
      };

      EvaluationResult result = _evaluator.Evaluate(required, user, rules);

      Assert.Single(result.MissingIngredientIds);
      Assert.Equal(10, result.MissingIngredientIds.First());
    }
  }
}
