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

        // Begin transaction
        using var tx = await _context.Database.BeginTransactionAsync();

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

        await _context.SaveChangesAsync();

        // Upsert Characters
        var episodeCharPairs = new HashSet<(int EpisodeId, int CharacterId)>();

        foreach (var c in apiChars)
        {
            var originId = RickandMortyClient.ExtractIdFromUrl(c.Origin.Url) ?? -1;
            var locationId = RickandMortyClient.ExtractIdFromUrl(c.Location.Url) ?? -1;

            if (!existingLocations.ContainsKey(originId) && originId != -1)
            {
                _context.Locations.Add(new Location
                {
                    Id = originId,
                    Name = c.Origin.Name,
                    Type = "unknown",
                    Dimension = "unknown"
                });
                existingLocations[originId] = await _context.Locations.FindAsync(originId) ?? new Location { Id = originId };
            }
            if (!existingLocations.ContainsKey(locationId) && locationId != -1)
            {
                _context.Locations.Add(new Location
                {
                    Id = locationId,
                    Name = c.Location.Name,
                    Type = "unknown",
                    Dimension = "unknown"
                });
                existingLocations[locationId] = await _context.Locations.FindAsync(locationId) ?? new Location { Id = locationId };
            }

            if (!existingCharacters.TryGetValue(c.Id, out var character))
            {
                character = new Character
                {
                    Id = c.Id,
                };
                _context.Characters.Add(character);
                existingCharacters[c.Id] = character;
            }

            character.Name = c.Name;
            character.Status = c.Status;
            character.Species = c.Species;
            character.Type = c.Type;
            character.Gender = c.Gender;
            character.OriginLocationId = originId;
            character.LocationId = locationId;
        }

        await _context.SaveChangesAsync();

        // Relate EpisodeCharacters Based on the episode payloads
        foreach (var ep in apiEpisodes)
        {
            var characterIds = ep.CharacterUrls
                .Select(RickandMortyClient.ExtractIdFromUrl)
                .Where(id => id.HasValue).Select(id => id!.Value)
                .Distinct();

            foreach (var characterId in characterIds)
            {
                var key = (ep.Id, characterId);
                if (episodeCharPairs.Contains(key)) continue;

                var exists = await _context.EpisodeCharacters.AnyAsync(ec => ec.EpisodeId == ep.Id && ec.CharacterId == characterId);
                if (!exists)
                {
                    _context.EpisodeCharacters.Add(new EpisodeCharacter
                    {
                        EpisodeId = ep.Id,
                        CharacterId = characterId
                    });
                }
            }
        }
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }
}