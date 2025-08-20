using api.Data;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetEnrichedArchive([FromRoute] int uploadId)
    {
        var upload = await _context.Uploads.FindAsync(uploadId);
        if (upload == null)
        {
            return NotFound($"Upload with ID {uploadId} not found.");
        }

        // Placeholder for actual enriched archive generation logic
        // For now, just return a message indicating success
        return Ok(new { message = $"Enriched archive for upload ID {uploadId} is being generated." });
    }
        
}