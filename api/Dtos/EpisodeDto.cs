
using System.Text.Json.Serialization;

namespace api.Dtos;

public sealed class EpisodeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("air_date")]
    public string AirDate { get; set; } = string.Empty;

    [JsonPropertyName("episode")]
    public string Episode { get; set; } = string.Empty;

    [JsonPropertyName("characters")]
    public List<CharacterDto> Characters { get; set; } = new();
}
