
using System.Text.Json.Serialization;
namespace api.Dtos;

public sealed class EnrichedArchiveDto
{
    [JsonPropertyName("episodes")]
    public List<EpisodeDto> Episodes { get; init; } = new();

    [JsonPropertyName("totalLocations")]
    public int TotalLocations { get; init; }

    [JsonPropertyName("totalFemaleCharacters")]
    public int TotalFemaleCharacters { get; init; }

    [JsonPropertyName("totalMaleCharacters")]
    public int TotalMaleCharacters { get; init; }

    [JsonPropertyName("totalGenderlessCharacters")]
    public int TotalGenderlessCharacters { get; init; }

    [JsonPropertyName("totalGenderUnknownCharacters")]
    public int totalGenderUnknownCharacters { get; init; }

    [JsonPropertyName("uploadedFilePath")]
    public string UploadedFilePath { get; init; } = string.Empty;
}