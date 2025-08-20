
using System.Text.Json.Serialization;

namespace api.Dtos;

public sealed class LocationDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("dimension")]
    public string Dimension { get; init; } = string.Empty;
}
