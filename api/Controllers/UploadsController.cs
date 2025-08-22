
using api.Data;
using api.Services.Background;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/uploads")]
[ApiController]
public class UploadHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IJobQueue _jobQueue;

    public UploadHistoryController(ApplicationDbContext context, IWebHostEnvironment env, IJobQueue jobQueue)
    {
        _context = context;
        _env = env;
        _jobQueue = jobQueue;
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
        if (upload is null)
        {
            return NotFound();
        }
        return Ok(upload);
    }

    /// <summary>
    /// Upload the CSV, save to disk, create UploadHistory object in DB, and enqueue background process
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)] // 50 MB limit
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        string extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid file type. Only '.csv' files are allowed.");
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Save the file to the server
        DateTime createdTimestamp = DateTime.UtcNow;
        string newFileName = $"{createdTimestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.csv";
        string filePath = Path.Combine(uploadsDir, newFileName);
        using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
        {
            await file.CopyToAsync(stream, ct);
        }
        var uploadRecord = new Entities.UploadHistory
        {
            FilePath = filePath,
            CreatedTimestamp = createdTimestamp,
            Status = Entities.ProcessingStatus.Pending,
            StartedAt = null,
            FinishedAt = null
        };

        _context.Uploads.Add(uploadRecord);
        await _context.SaveChangesAsync(ct);

        //Enqueue BG Processing
        await _jobQueue.EnqueueAsync(new ProcessUploadCommand(uploadRecord.Id, filePath), ct);

        var statusUrl = Url.ActionLink(
            action: nameof(GetUploadById),
            controller: "UploadHistory",
            values: new { id = uploadRecord.Id },
            protocol: Request.Scheme
        ) ?? $"/api/uploads/{uploadRecord.Id}";

        return Accepted(new
        {
            statusUrl
        });
    }
}