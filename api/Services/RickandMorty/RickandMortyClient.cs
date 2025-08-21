using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace api.Services.RickandMorty;

public sealed class RickandMortyClient : IRickandMortyClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);


    public RickandMortyClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri("https://rickandmortyapi.com/api/");
        }
    }

    public async Task<List<ApiEpisode>> GetEpisodesAsync(IEnumerable<int> ids)
        => await GetManyAsync<ApiEpisode>("episode", ids);

    public async Task<List<ApiCharacter>> GetCharactersAsync(IEnumerable<int> ids)
        => await GetManyAsync<ApiCharacter>("character", ids);

    public async Task<List<ApiLocation>> GetLocationsAsync(IEnumerable<int> ids)
        => await GetManyAsync<ApiLocation>("location", ids);


    // Need to implement a way to get API response with multiple IDs as array

    /// <summary>
    /// Generic method to fetch multiple entities from the API at once.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    private async Task<List<T>> GetManyAsync<T>(string endpoint, IEnumerable<int> ids)
    {
        var results = new List<T>();
        foreach (var chunk in Chunk(ids, size: 20))
        {
            string path = $"{endpoint}/({string.Join(",", chunk)})";
            using var response = await _http.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Return a single object when there is only 1 ID
            if (json.TrimStart().StartsWith("["))
            {
                var arr = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new();
                results.AddRange(arr);
            }
            else
            {
                var single = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (single is not null)
                {
                    results.Add(single);
                }
            }
        }
        return results;
    }


    private static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var list = new List<T>(size);
        foreach (var i in source.Distinct())
        {
            list.Add(i);
            if (list.Count == size)
            {
                yield return list;
                list = new List<T>(size);
            }
        }
        if (list.Count > 0)
        {
            yield return list;
        }

    }

    public static int? ExtractIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = Regex.Match(url, @".*/(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }
}
