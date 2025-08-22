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
        var cleanIds = ids.Where(i => i > 0).Distinct().ToArray();
        var results = new List<T>();
        if (cleanIds.Length == 0) return results;

        foreach (var chunk in Chunk(cleanIds, size: 20))
        {
            // Rick & Morty API expects /episode/1,2,3 (NO parentheses)
            string path = $"{endpoint}/{string.Join(",", chunk)}";
            HttpResponseMessage? response = null;
            string? body = null;
            try
            {
                response = await _http.GetAsync(path);
                if (!response.IsSuccessStatusCode)
                {
                    body = await response.Content.ReadAsStringAsync();
                    // 404 means some IDs not found; skip this chunk (could refine per ID)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    throw new HttpRequestException($"RickAndMorty API call failed (status {(int)response.StatusCode}) for '{path}'. Body: {body}");
                }

                body = await response.Content.ReadAsStringAsync();
                var trimmed = body.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<T>>(body, _jsonOptions) ?? new();
                    results.AddRange(arr);
                }
                else
                {
                    var single = JsonSerializer.Deserialize<T>(body, _jsonOptions);
                    if (single is not null)
                    {
                        results.Add(single);
                    }
                }
            }
            finally
            {
                response?.Dispose();
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
