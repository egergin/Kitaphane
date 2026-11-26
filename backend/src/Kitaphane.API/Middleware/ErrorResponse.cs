using System.Text.Json.Serialization;

namespace Kitaphane.API.Middleware;

/// <summary>
/// The single error format used by every API error response.
/// </summary>
public sealed record ErrorResponse(
    string Message,
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IDictionary<string, string[]>? Errors = null);
