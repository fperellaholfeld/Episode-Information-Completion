using CsvHelper.Configuration;

namespace api.Services.Csv;

/// <summary>
/// Represents a row in the CSV file.
/// </summary>
public sealed class CsvRow
{
    public int EpisodeId { get; set; }
    public int CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int LocationId { get; set; }
}

/// <summary>
/// Maps the CSV columns to the CsvRow properties.
/// </summary>
public sealed class CsvEpisodeRowMap : ClassMap<CsvRow>
{
    public CsvEpisodeRowMap()
    {
        Map(m => m.EpisodeId).Name("episode_id");
        Map(m => m.CharacterId).Name("character_id");
        Map(m => m.CharacterName).Name("character_name");
        Map(m => m.LocationId).Name("location_id");
    }

}

