using api.Data;
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
        var q = _context.UploadEpisodes
            .Where(ue => ue.UploadId == uploadId)
            .Select(ue => ue.Episode)
            .AsQueryable();

        // Perform any necessary transformations or aggregations on the queryable

        // Search by episode name, episode code, or character
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = $"%{search.Trim()}%";
            q = q.Where(e =>
                EF.Functions.Like(e.Name, term) || // episode name
                EF.Functions.Like(e.EpisodeCode, term) || // episode code
                e.EpisodeCharacters.Any(ec => EF.Functions.Like(ec.Character.Name, term)) // character name
            );
        }

        // Sorting
        (string sortBy, bool descending) = ParseSort(sort);
        q = (sortBy, descending) switch
        {
            ("name", true) => q.OrderByDescending(e => e.Name),
            ("name", false) => q.OrderBy(e => e.Name),
            ("air_date", true) => q.OrderByDescending(e => e.AirDate),
            ("air_date", false) => q.OrderBy(e => e.AirDate),
            ("episode", true) => q.OrderByDescending(e => e.EpisodeCode),
            ("episode", false) => q.OrderBy(e => e.EpisodeCode),
            ("id", true) => q.OrderByDescending(e => e.Id),
            _ => q.OrderBy(e => e.Id)
        };

        // For now, just return a message indicating success
        return Ok(new { message = $"Enriched archive for upload ID {uploadId} is being generated." });
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