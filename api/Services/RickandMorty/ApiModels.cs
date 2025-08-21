using System.Text.Json.Serialization;

namespace api.Services.RickandMorty;

public sealed class ApiEpisode
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("air_date")]
    public string AirDate { get; init; } = string.Empty;

    [JsonPropertyName("episode")]
    public string EpisodeCode { get; init; } = string.Empty;

    [JsonPropertyName("characters")]
    public List<string> CharacterUrls { get; init; } = new();
}

public sealed class ApiRef
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}

public sealed class ApiCharacter
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
    public ApiRef Origin { get; init; } = new();

    [JsonPropertyName("location")]
    public ApiRef Location { get; init; } = new();
}

public sealed class ApiLocation
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
