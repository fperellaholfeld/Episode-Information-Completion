using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace api.Services.Csv;

/// <summary>
/// Represents a row in the CSV file.
/// </summary>
public sealed class CsvRow
{
    [Index(0)]
    public int EpisodeId { get; set; }
    [Index(1)]
    public int CharacterId { get; set; }
    [Index(2)]
    public string CharacterName { get; set; } = string.Empty;
    [Index(3)]
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

