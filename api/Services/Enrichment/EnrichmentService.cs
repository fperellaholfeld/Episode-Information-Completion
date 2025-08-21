using api.Data;
using api.Entities;
using api.Services.RickandMorty;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Enrichment;

public interface IEnrichmentService
{
    Task EnrichUploadAsync(int uploadId, IEnumerable<int> episodeIds);
}
public sealed class EnrichmentService : IEnrichmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IRickandMortyClient _client;
    private readonly ILogger<EnrichmentService> _logger;

    public EnrichmentService(ApplicationDbContext context, IRickandMortyClient client, ILogger<EnrichmentService> logger)
    {
        _context = context;
        _client = client;
        _logger = logger;
    }

    public async Task EnrichUploadAsync(int uploadId, IEnumerable<int> episodeIds)
    {
        var eps = episodeIds.Distinct().ToArray();
        if (eps.Length == 0) return;

        // Fetch Episodes
        var apiEpisodes = await _client.GetEpisodesAsync(eps);

        // Collect Character IDs and Fetch
        var charIds = apiEpisodes
            .SelectMany(e => e.CharacterUrls)
            .Select(RickandMortyClient.ExtractIdFromUrl)
            .Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var apiChars = charIds.Length > 0
            ? await _client.GetCharactersAsync(charIds)
            : new List<ApiCharacter>();

        // Collect the Location IDs from each character and fetch the locations
        var locIds = apiChars
            .SelectMany(c => new[] { RickandMortyClient.ExtractIdFromUrl(c.Origin.Url), RickandMortyClient.ExtractIdFromUrl(c.Location.Url) })
            .Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var apiLocs = locIds.Length > 0
            ? await _client.GetLocationsAsync(locIds)
            : new List<ApiLocation>();

       

    }
}