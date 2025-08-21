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

        // Load Existing Entities
        var existingEpisodes = await _context.Episodes.Where(e => eps.Contains(e.Id)).ToDictionaryAsync(e => e.Id);
        var existingCharacters = await _context.Characters.Where(c => charIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var existingLocations = await _context.Locations.Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id);

        // Upsert locations
        foreach (var loc in apiLocs)
        {
            if (!existingLocations.TryGetValue(loc.Id, out var location))
            {
                location = new Location
                {
                    Id = loc.Id,
                    Name = loc.Name
                };
                _context.Locations.Add(location);
                existingLocations[loc.Id] = location;
            }

            location.Name = loc.Name;
            location.Type = loc.Type;
            location.Dimension = loc.Dimension;
        }

        await _context.SaveChangesAsync();

        //upsert Episodes
        foreach (var ep in apiEpisodes)
        {
            if (!existingEpisodes.TryGetValue(ep.Id, out var episode))
            {
                episode = new Episode
                {
                    Id = ep.Id,
                    Name = ep.Name,
                    AirDate = ep.AirDate,
                    EpisodeCode = ep.EpisodeCode
                };
                _context.Episodes.Add(episode);
                existingEpisodes[ep.Id] = episode;
            }

            episode.Name = ep.Name;
            episode.AirDate = ep.AirDate;
            episode.EpisodeCode = ep.EpisodeCode;

            // relate to upload
            if (!await _context.UploadEpisodes.AnyAsync(x => x.UploadId == uploadId && x.EpisodeId == episode.Id))
            {
                _context.UploadEpisodes.Add(new UploadEpisode { UploadId = uploadId, EpisodeId = episode.Id });
            }
        }

    }
}