using api.Data;
using api.Entities;
using api.Services.RickandMorty;
using api.Services.Csv;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Enrichment;

public interface IEnrichmentService
{
    Task EnrichUploadAsync(int uploadId, List<CsvRow> rows);
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

    public async Task EnrichUploadAsync(int uploadId, List<CsvRow> rows)
    {
        if (rows.Count == 0) return;

    var episodeIds = rows.Select(r => r.EpisodeId).Where(id => id > 0).Distinct().ToArray();
    var characterIdsCsv = rows.Select(r => r.CharacterId).Where(id => id > 0).Distinct().ToArray();
    var locationIdsCsv = rows.Select(r => r.LocationId).Where(id => id > 0).Distinct().ToArray();

        // Fetch Episodes
        var apiEpisodes = await _client.GetEpisodesAsync(episodeIds);

        // Collect Character IDs from the episodes themselves
        var charIdsFromEpisodes = apiEpisodes
            .SelectMany(e => e.CharacterUrls)
            .Select(RickandMortyClient.ExtractIdFromUrl)
            .Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct()
            .ToArray();
        
        // Combine the ids to make sure we have full character coverage for an episode
    var charIds = characterIdsCsv
            .Union(charIdsFromEpisodes)
            .Distinct()
            .ToArray();

        var apiChars = charIds.Length > 0
            ? await _client.GetCharactersAsync(charIds)
            : new();

        // Collect the Location IDs from each character and unite with the character in the csv
        var locIdsFromChars = apiChars
            .SelectMany(c => new[] { RickandMortyClient.ExtractIdFromUrl(c.Origin.Url), RickandMortyClient.ExtractIdFromUrl(c.Location.Url) })
            .Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct()
            .ToArray();
    var locIds = locIdsFromChars
            .Union(locationIdsCsv)
            .Distinct()
            .ToArray();
        var apiLocs = locIds.Length > 0
            ? await _client.GetLocationsAsync(locIds)
            : new List<ApiLocation>();

        // Begin transaction
        using var tx = await _context.Database.BeginTransactionAsync();

        // Load Existing Entities
        var existingEpisodes = await _context.Episodes.Where(e => episodeIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id);
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
            const int UnknownLocationId = 0;
            var originId = RickandMortyClient.ExtractIdFromUrl(c.Origin.Url) ?? UnknownLocationId;
            var locationId = RickandMortyClient.ExtractIdFromUrl(c.Location.Url) ?? UnknownLocationId;

            // Ensure placeholder exists exactly once if referenced
            if ((originId == UnknownLocationId || locationId == UnknownLocationId) && !existingLocations.ContainsKey(UnknownLocationId))
            {
                var existingUnknown = await _context.Locations.FindAsync(UnknownLocationId);
                if (existingUnknown == null)
                {
                    existingUnknown = new Location
                    {
                        Id = UnknownLocationId,
                        Name = "unknown",
                        Type = "unknown",
                        Dimension = "unknown"
                    };
                    _context.Locations.Add(existingUnknown);
                }
                existingLocations[UnknownLocationId] = existingUnknown;
            }

            // Add origin/location shells if not already tracked (will be updated later if API supplies details elsewhere)
            if (originId != UnknownLocationId && !existingLocations.ContainsKey(originId))
            {
                var originLoc = new Location
                {
                    Id = originId,
                    Name = c.Origin.Name,
                    Type = "unknown",
                    Dimension = "unknown"
                };
                _context.Locations.Add(originLoc);
                existingLocations[originId] = originLoc;
            }
            if (locationId != UnknownLocationId && !existingLocations.ContainsKey(locationId))
            {
                var currentLoc = new Location
                {
                    Id = locationId,
                    Name = c.Location.Name,
                    Type = "unknown",
                    Dimension = "unknown"
                };
                _context.Locations.Add(currentLoc);
                existingLocations[locationId] = currentLoc;
            }

            if (!existingCharacters.TryGetValue(c.Id, out var character))
            {
                character = new Character { Id = c.Id };
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