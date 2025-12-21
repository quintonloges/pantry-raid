using Loges.PantryRaid.Dtos;
using Loges.PantryRaid.EFCore;
using Loges.PantryRaid.Models;
using Loges.PantryRaid.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loges.PantryRaid.Services;

public class RecipeService : IRecipeService {
  private readonly AppDbContext _context;
  private readonly ISubstitutionEvaluator _substitutionEvaluator;
  private readonly ISubstitutionService _substitutionService;

  public RecipeService(
    AppDbContext context,
    ISubstitutionEvaluator substitutionEvaluator,
    ISubstitutionService substitutionService) {
    _context = context;
    _substitutionEvaluator = substitutionEvaluator;
    _substitutionService = substitutionService;
  }

  public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request) {
    IQueryable<Recipe> query = _context.Recipes.AsQueryable();

    // 1. Metadata Filters
    if (request.Filters.ProteinId.HasValue) {
      query = query.Where(r => r.RecipeProteins.Any(rp => rp.ProteinId == request.Filters.ProteinId.Value));
    }

    if (request.Filters.CuisineIds != null && request.Filters.CuisineIds.Any()) {
      query = query.Where(r => r.RecipeCuisines.Any(rc => request.Filters.CuisineIds.Contains(rc.CuisineId)));
    }

    if (request.Filters.DietaryTagIds != null && request.Filters.DietaryTagIds.Any()) {
      foreach (int tagId in request.Filters.DietaryTagIds) {
        query = query.Where(r => r.RecipeDietaryTags.Any(rd => rd.DietaryTagId == tagId));
      }
    }

    if (request.Filters.SourceIds != null && request.Filters.SourceIds.Any()) {
      query = query.Where(r => request.Filters.SourceIds.Contains(r.RecipeSourceId));
    }

    if (request.Filters.MustIncludeIngredientIds != null && request.Filters.MustIncludeIngredientIds.Any()) {
      foreach (int ingId in request.Filters.MustIncludeIngredientIds) {
        query = query.Where(r => r.Ingredients.Any(ri => ri.IngredientId == ingId));
      }
    }

    // 2. Missing Count Logic
    List<int> userIngredientIds = request.IngredientIds ?? new List<int>();

    // Filter in DB: Missing <= 3 (Required ingredients only)
    query = query.Where(r => r.Ingredients
      .Count(ri => !ri.IsOptional && (ri.IngredientId == null || !userIngredientIds.Contains(ri.IngredientId.Value))) <= 3);

    // 3. Fetch Data
    List<Recipe> candidates = await query
      .AsNoTracking()
      .Include(r => r.Ingredients).ThenInclude(ri => ri.Ingredient)
      .Include(r => r.RecipeCuisines).ThenInclude(rc => rc.Cuisine)
      .Include(r => r.RecipeProteins).ThenInclude(rp => rp.Protein)
      .Include(r => r.RecipeDietaryTags).ThenInclude(rd => rd.DietaryTag)
      .ToListAsync();

    // 4. In-Memory Processing
    IEnumerable<SubstitutionGroupDto> substitutionRules = Enumerable.Empty<SubstitutionGroupDto>();
    if (request.AllowSubstitutions) {
      substitutionRules = await _substitutionService.GetAllGroupsAsync();
    }

    List<IGrouping<int, RecipeMatch>> grouped = candidates
      .Select(recipe => {
        List<RecipeIngredient> required = recipe.Ingredients.Where(ri => !ri.IsOptional).ToList();
        
        List<RecipeIngredient> missing = required
          .Where(ri => ri.IngredientId == null || !userIngredientIds.Contains(ri.IngredientId.Value))
          .ToList();
        
        List<RecipeIngredient> have = recipe.Ingredients
          .Where(ri => ri.IngredientId.HasValue && userIngredientIds.Contains(ri.IngredientId.Value))
          .ToList();

        List<string> substitutionNotes = new List<string>();
        int missingCount = missing.Count;

        if (request.AllowSubstitutions && missingCount > 0 && substitutionRules.Any()) {
          // Get IDs of missing ingredients that are mappable (have an IngredientId)
          List<int> missingIds = missing
            .Where(ri => ri.IngredientId.HasValue)
            .Select(ri => ri.IngredientId!.Value)
            .ToList();

          if (missingIds.Any()) {
            EvaluationResult evalResult = _substitutionEvaluator.Evaluate(missingIds, userIngredientIds, substitutionRules);
            
            // Identify which were satisfied by substitution
            List<KeyValuePair<int, IngredientMatch>> substitutedMatches = evalResult.Matches
              .Where(m => m.Value.Type == IngredientMatchType.Substitution)
              .ToList();

            // Recalculate missing based on evaluation result
            // The evaluator tells us exactly what is still missing from the set we gave it
            int satisfiedCount = evalResult.Matches.Count; // Exact + Substitution
            
            // Note: The evaluator returns matches for Exact (already filtered out above? No.)
            // Wait, I passed `missingIds` to Evaluate. So any "Exact" match returned by Evaluate
            // implies I actually had it but thought it was missing?
            // `missing` list above is filtered by `!userIngredientIds.Contains`.
            // So `Evaluate` should NOT return Exact matches unless there's a bug or I passed something I have.
            // Assuming `Evaluate` only finds Substitutions for these inputs.
            
            missingCount -= substitutedMatches.Count;

            // Collect notes
            foreach (var match in substitutedMatches) {
              RecipeIngredient? targetRi = missing.FirstOrDefault(ri => ri.IngredientId == match.Key);
              if (targetRi == null){
                continue;
              }

              string targetName = targetRi.Ingredient?.Name ?? "Unknown";
              
              // Format note
              // Need to traverse the path to build a readable string?
              // The spec says: "* Substitute with <ingredient>"
              // The evaluator returns a SubstitutionPath.
              // Let's format it simply: "Substitute [Target] with [Source1, Source2...]"
              
              if (match.Value.Substitution != null) {
                SubstitutionPath path = match.Value.Substitution;
                SubstitutionGroupDto? group = substitutionRules.FirstOrDefault(g => g.TargetIngredientId == match.Key);
                SubstitutionOptionDto? option = group?.Options.FirstOrDefault(o => o.Id == path.OptionId);

                if (option != null) {
                  List<string> sourceNames = option.Ingredients
                    .Select(i => i.IngredientName)
                    .ToList();

                  string sourcesStr = string.Join(", ", sourceNames);
                  string note = $"Substitute {targetName} with {sourcesStr}";
                  if (!string.IsNullOrEmpty(path.Note)) {
                    note += $" ({path.Note})";
                  }
                  substitutionNotes.Add(note);
                  
                  // Remove from missing list so it doesn't appear in "Missing Ingredients"
                  missing.Remove(targetRi);
                }
              }
            }
          }
        }
        
        return new RecipeMatch(
          recipe,
          missing.Count,
          missing.Select(ri => ri.Ingredient?.Name ?? ri.OriginalText).OrderBy(n => n).ToList(),
          have.Select(ri => ri.Ingredient?.Name ?? ri.OriginalText).OrderBy(n => n).ToList(),
          substitutionNotes
        );
      })
      .GroupBy(x => x.MissingCount)
      .ToList();

    List<RecipeGroupDto> results = new List<RecipeGroupDto>();
    for (int i = 0; i <= 3; i++) {
      IGrouping<int, RecipeMatch>? group = grouped.FirstOrDefault(g => g.Key == i);
      List<SearchResultRecipeDto> recipeDtos = group?
        .OrderBy(x => x.Recipe.Title)
        .Select(x => new SearchResultRecipeDto {
          Recipe = MapToDto(x.Recipe),
          HaveIngredients = x.HaveIngredients,
          MissingIngredients = x.MissingIngredients,
          SubstitutionNotes = x.SubstitutionNotes
        })
        .ToList() ?? new List<SearchResultRecipeDto>();

      results.Add(new RecipeGroupDto {
        MissingCount = i,
        Recipes = recipeDtos
      });
    }

    return new SearchResponseDto {
      Results = results,
      Cursor = null
    };
  }

  private record RecipeMatch(Recipe Recipe, int MissingCount, List<string> MissingIngredients, List<string> HaveIngredients, List<string> SubstitutionNotes);

  public async Task<RecipeDto> CreateAsync(CreateRecipeDto dto) {
    Recipe recipe = new Recipe {
      Title = dto.Title,
      RecipeSourceId = dto.RecipeSourceId,
      SourceUrl = dto.SourceUrl,
      SourceRecipeId = dto.SourceRecipeId,
      ShortDescription = dto.ShortDescription,
      ImageUrl = dto.ImageUrl,
      TotalTimeMinutes = dto.TotalTimeMinutes,
      Servings = dto.Servings,
      ScrapeStatus = "Manual"
    };

    int index = 0;
    foreach (CreateRecipeIngredientDto ingredientDto in dto.Ingredients) {
      RecipeIngredient recipeIngredient = new RecipeIngredient {
        IngredientId = ingredientDto.IngredientId,
        OriginalText = ingredientDto.OriginalText,
        Quantity = ingredientDto.Quantity,
        Unit = ingredientDto.Unit,
        IsOptional = ingredientDto.IsOptional,
        OrderIndex = index++
      };
      recipe.Ingredients.Add(recipeIngredient);
    }

    _context.Recipes.Add(recipe);
    await _context.SaveChangesAsync();
    
    return MapToDto(recipe);
  }

  public async Task<RecipeDto?> GetByIdAsync(int id) {
    Recipe? recipe = await _context.Recipes
      .Include(r => r.Ingredients)
      .Include(r => r.RecipeCuisines).ThenInclude(rc => rc.Cuisine)
      .Include(r => r.RecipeProteins).ThenInclude(rp => rp.Protein)
      .Include(r => r.RecipeDietaryTags).ThenInclude(rd => rd.DietaryTag)
      .FirstOrDefaultAsync(r => r.Id == id);
      
    if (recipe == null) {
      return null;
    }
    return MapToDto(recipe);
  }

  public async Task SetTagsAsync(int recipeId, SetRecipeTagsDto dto) {
    Recipe? recipe = await _context.Recipes
      .Include(r => r.RecipeCuisines)
      .Include(r => r.RecipeProteins)
      .Include(r => r.RecipeDietaryTags)
      .FirstOrDefaultAsync(r => r.Id == recipeId);

    if (recipe == null) {
      throw new ArgumentException("Recipe not found", nameof(recipeId));
    }

    // Clear existing
    _context.RecipeCuisines.RemoveRange(recipe.RecipeCuisines);
    _context.RecipeProteins.RemoveRange(recipe.RecipeProteins);
    _context.RecipeDietaryTags.RemoveRange(recipe.RecipeDietaryTags);

    // Add new
    foreach (int id in dto.CuisineIds.Distinct()) {
      _context.RecipeCuisines.Add(new RecipeCuisine { RecipeId = recipeId, CuisineId = id });
    }
    foreach (int id in dto.ProteinIds.Distinct()) {
      _context.RecipeProteins.Add(new RecipeProtein { RecipeId = recipeId, ProteinId = id });
    }
    foreach (int id in dto.DietaryTagIds.Distinct()) {
      _context.RecipeDietaryTags.Add(new RecipeDietaryTag { RecipeId = recipeId, DietaryTagId = id });
    }

    await _context.SaveChangesAsync();
  }

  private static RecipeDto MapToDto(Recipe recipe) {
    return new RecipeDto {
      Id = recipe.Id,
      Title = recipe.Title,
      RecipeSourceId = recipe.RecipeSourceId,
      SourceUrl = recipe.SourceUrl,
      ShortDescription = recipe.ShortDescription,
      ImageUrl = recipe.ImageUrl,
      TotalTimeMinutes = recipe.TotalTimeMinutes,
      Servings = recipe.Servings,
      ScrapeStatus = recipe.ScrapeStatus,
      Ingredients = recipe.Ingredients.OrderBy(i => i.OrderIndex).Select(i => new RecipeIngredientDto {
        Id = i.Id,
        IngredientId = i.IngredientId,
        OriginalText = i.OriginalText,
        Quantity = i.Quantity,
        Unit = i.Unit,
        IsOptional = i.IsOptional
      }).ToList(),
      Cuisines = recipe.RecipeCuisines.Select(rc => new CuisineDto { Id = rc.Cuisine.Id, Name = rc.Cuisine.Name }).ToList(),
      Proteins = recipe.RecipeProteins.Select(rp => new ProteinDto { Id = rp.Protein.Id, Name = rp.Protein.Name }).ToList(),
      DietaryTags = recipe.RecipeDietaryTags.Select(rd => new DietaryTagDto { Id = rd.DietaryTag.Id, Name = rd.DietaryTag.Name }).ToList()
    };
  }
}
