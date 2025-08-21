using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace api.Services.Csv;

public static class CsvEpisodeRowParser
{
    public static async Task<List<CsvRow>> ParseAsync(string path)
    {
        using var reader = new StreamReader(path);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HeaderValidated = null,
            IgnoreBlankLines = true,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, cfg);
        csv.Context.RegisterClassMap<CsvEpisodeRowMap>();

        var rows = new List<CsvRow>();
        await foreach (var record in csv.GetRecordsAsync<CsvRow>())
        {
            rows.Add(record);
        }
        return rows;
    }

}
