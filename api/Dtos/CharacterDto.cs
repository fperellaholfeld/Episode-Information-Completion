
using System.Text.Json.Serialization;

namespace api.Dtos;

public sealed class CharacterDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("species")]
    public string Species { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; init; } = string.Empty;

    [JsonPropertyName("origin")]
    public LocationDto Origin { get; init; } = new();

    [JsonPropertyName("location")]
    public LocationDto Location { get; init; } = new();
}