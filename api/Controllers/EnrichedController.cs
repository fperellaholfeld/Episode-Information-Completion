using api.Data;
using api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[Route("api/enriched")]
[ApiController]
public sealed class EnrichedController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public EnrichedController(ApplicationDBContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get the enriched archive from the given upload
    /// </summary>
    [HttpGet("{uploadId:int}")]
    public async Task<IActionResult> GetEnrichedArchive(
        [FromRoute] int uploadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null
    )
    {
        var upload = await _context.Uploads.FindAsync(uploadId);
        if (upload == null)
        {
            return NotFound($"Upload with ID {uploadId} not found.");
        }

        //query episodes linked to the upload
         var episodesBaseQuery = _context.Episodes
        .Where(e => _context.UploadEpisodes.Any(ue => ue.UploadId == uploadId && ue.EpisodeId == e.Id));

        // Perform any necessary transformations or aggregations on the queryable

        // Search by episode name, episode code, or character
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = $"%{search.Trim()}%";
            episodesBaseQuery = episodesBaseQuery.Where(e =>
                EF.Functions.Like(e.Name, term) || // episode name
                EF.Functions.Like(e.EpisodeCode, term) || // episode code
                e.EpisodeCharacters.Any(ec => EF.Functions.Like(ec.Character.Name, term)) // character name
            );
        }

        // Sorting
        (string sortBy, bool descending) = ParseSort(sort);
        episodesBaseQuery = (sortBy, descending) switch
        {
            ("name", true) => episodesBaseQuery.OrderByDescending(e => e.Name),
            ("name", false) => episodesBaseQuery.OrderBy(e => e.Name),
            ("air_date", true) => episodesBaseQuery.OrderByDescending(e => e.AirDate),
            ("air_date", false) => episodesBaseQuery.OrderBy(e => e.AirDate),
            ("episode", true) => episodesBaseQuery.OrderByDescending(e => e.EpisodeCode),
            ("episode", false) => episodesBaseQuery.OrderBy(e => e.EpisodeCode),
            ("id", true) => episodesBaseQuery.OrderByDescending(e => e.Id),
            _ => episodesBaseQuery.OrderBy(e => e.Id)
        };

        // Set pagination limits and offset
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;
        int skip = (page - 1) * pageSize;

        // Materialize the paged episodes with their characters and locations
        var episodes = await episodesBaseQuery
            .Include(e => e.EpisodeCharacters)
                .ThenInclude(ec => ec.Character)
                    .ThenInclude(c => c.Origin)
            .Include(e => e.EpisodeCharacters)
                .ThenInclude(ec => ec.Character)
                    .ThenInclude(c => c.Location)
            .AsSplitQuery()
            .AsNoTracking()
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // Project to the schema shape
        var episodeDtos = episodes.Select(e => new Dtos.EpisodeDto
        {
            Id = e.Id,
            Name = e.Name,
            AirDate = e.AirDate,
            Episode = e.EpisodeCode,
            Characters = e.EpisodeCharacters
                .Select(ec => ec.Character)
                .DistinctBy(c => c.Id)
                .Select(c => new Dtos.CharacterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Status = c.Status,
                    Species = c.Species,
                    Type = c.Type,
                    Gender = c.Gender,
                    Origin = new Dtos.LocationDto
                    {
                        Id = c.Origin.Id,
                        Name = c.Origin.Name,
                        Type = c.Origin.Type,
                        Dimension = c.Origin.Dimension
                    },
                    Location = new Dtos.LocationDto
                    {
                        Id = c.Location.Id,
                        Name = c.Location.Name,
                        Type = c.Location.Type,
                        Dimension = c.Location.Dimension
                    }
                }).ToList()
        }).ToList();

        // TODO: See if this should be done by page or entire upload query
        var distinctCharacters = episodes.SelectMany(e => e.EpisodeCharacters.Select(ec => ec.Character))
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        int totalLocations = distinctCharacters
            .SelectMany(c => new[] { c.OriginLocationId, c.LocationId })
            .Distinct()
            .Count();

        var response = new EnrichedArchiveDto
        {
            Episodes = episodeDtos,
            TotalLocations = totalLocations,
            TotalFemaleCharacters = distinctCharacters.Count(c => c.Gender == "Female"),
            TotalMaleCharacters = distinctCharacters.Count(c => c.Gender == "Male"),
            TotalGenderlessCharacters = distinctCharacters.Count(c => c.Gender == "Genderless"),
            TotalGenderUnknownCharacters = distinctCharacters.Count(c => c.Gender == "unknown"),
            UploadedFilePath = upload.FilePath
        };

        // For now, just return a message indicating success
        return Ok(response);
    }

    private static (string sortBy, bool descending) ParseSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return ("Id", false); // default sort
        }

        string s = sort.Trim().ToLowerInvariant();
        bool descending = s.StartsWith("-") || s.EndsWith(":desc");
        s = s.Replace("-", "").Replace(":desc", "");
        return s switch
        {
            "name" or "air_date" or "episode" or "id" => (s, descending),
            _ => ("Id", false)
        };
    }

}