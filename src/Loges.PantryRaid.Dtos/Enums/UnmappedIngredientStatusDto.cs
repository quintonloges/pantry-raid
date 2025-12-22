using System.Text.Json.Serialization;

namespace Loges.PantryRaid.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnmappedIngredientStatusDto {
  New,
  Suggested,
  Resolved
}
