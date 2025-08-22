// Services/Csv/CsvEpisodeRowParser.cs
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace api.Services.Csv;

public static class CsvEpisodeRowParser
{
    private static readonly string[] ExpectedHeaders = new[] { "episode_id", "character_id", "character_name", "location_id" };

    public static async Task<List<CsvRow>> ParseAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(path)) return new List<CsvRow>();

        // Read entire file once (small CSV assumption). If large, refactor to streaming with seekable stream.
        var allLines = await File.ReadAllLinesAsync(path, ct);
        if (allLines.Length == 0) return new List<CsvRow>();

        // Detect if first non-empty line is a header
        var firstContentLine = allLines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;
        var parts = firstContentLine.Split(',').Select(p => p.Trim().Trim('"').ToLowerInvariant()).ToArray();
        bool hasHeader = parts.Intersect(ExpectedHeaders).Count() >= 2; // at least two matches to be confident

        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader,
            IgnoreBlankLines = true,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Replace("_", "")
        };

        // Normalize property names similarly (EpisodeId -> episodeid) so headers map.
        using var reader = new StringReader(string.Join('\n', allLines));
        using var csv = new CsvReader(reader, cfg);

        // If there is a header, read it so CsvHelper establishes field indexes.
        var records = new List<CsvRow>();
        if (hasHeader)
        {
            if (!await csv.ReadAsync()) return records; // only header present
            csv.ReadHeader();
        }

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var record = csv.GetRecord<CsvRow>();
                records.Add(record);
            }
            catch
            {
                
            }
        }

        return records;
    }
}


