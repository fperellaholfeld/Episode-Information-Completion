
using api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;

namespace api.Controllers;

[Route("api/uploads")]
[ApiController]
public class UploadHistoryController : ControllerBase
{
    private readonly ApplicationDBContext _context;
    private readonly IWebHostEnvironment _env;
    public UploadHistoryController(ApplicationDBContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }
    /// <summary>
    /// Get all uploads.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetAllUploads()
    {
        var uploads = _context.Uploads.ToList();
        return Ok(uploads);
    }

    /// <summary>
    /// Get a specific upload by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public IActionResult GetUploadById([FromRoute] int id)
    {
        var upload = _context.Uploads.Find(id);
        if (upload == null)
        {
            return NotFound();
        }
        return Ok(upload);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)] // 50 MB limit
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        string extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid file type.");
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Save the file to the server
        DateTime createdTimestamp = DateTime.UtcNow;
        DateTime started;
        DateTime finished;
        string newFileName = $"{createdTimestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.csv";
        string filePath = Path.Combine(uploadsDir, newFileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            started = DateTime.UtcNow;
            await file.CopyToAsync(stream);
            finished = DateTime.UtcNow;
        }
        var uploadRecord = new Entities.UploadHistory
        {
            FilePath = filePath,
            CreatedTimestamp = createdTimestamp,
            Status = Entities.ProcessingStatus.Pending,
            StartedAt = started,
            FinishedAt = finished

        };

        _context.Uploads.Add(uploadRecord);
        await _context.SaveChangesAsync();

        var statusUrl = Url.ActionLink(
            action: nameof(GetUploadById),
            controller: "UploadHistory",
            values: new { id = uploadRecord.Id }
        );

        return Ok(new { statusUrl });
    }
}