using System.Threading.Channels;
using api.Data;
using api.Entities;
using api.Services.Csv;
using api.Services.Enrichment;
using api.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SQLitePCL;

namespace api.Services;

public sealed class UploadProcessingService : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UploadProcessingService> _log;

    public UploadProcessingService(IJobQueue jobQueue, IServiceProvider serviceProvider, ILogger<UploadProcessingService> log)
    {
        _log = log;
        _jobQueue = jobQueue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in _jobQueue.DequeueAsync(stoppingToken))
        {
            await ProcessOneAsync(command, stoppingToken);
        }
        _log.LogInformation("Upload Processing Service is stopping.");
    }

    private async Task ProcessOneAsync(ProcessUploadCommand command, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var enrichment = scope.ServiceProvider.GetRequiredService<IEnrichmentService>();

            //load + validate upload record
            var upload = await context.Uploads.FirstOrDefaultAsync(u => u.Id == command.UploadId, ct);
            if (upload is null)
            {
                _log.LogWarning("Upload with ID {UploadId} not found. Skipping processing.", command.UploadId);
                return;
            }

            // Process the upload
            upload.Status = ProcessingStatus.InProgress;
            upload.StartedAt = DateTime.UtcNow;
            upload.FinishedAt = null;
            await context.SaveChangesAsync(ct);

            if (!File.Exists(command.FilePath))
            {
                _log.LogWarning("File at path {FilePath} not found. Marking upload as failed.", command.FilePath);
                upload.Status = ProcessingStatus.Failed;
                upload.FinishedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
                return;
            }

            // Parse CSV RRows
            var rows = await CsvEpisodeRowParser.ParseAsync(command.FilePath);
            _log.LogInformation("Parsed {RowCount} rows from CSV file at {FilePath}.", rows.Count, command.FilePath);

            //Enrich and persist to DB
            await enrichment.EnrichUploadAsync(command.UploadId, rows);

            //Success
            upload.Status = ProcessingStatus.Completed;
            upload.FinishedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Service stopping — do not mark as failed
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error processing upload with ID {UploadId}.", command.UploadId);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var upload = await db.Uploads.FirstOrDefaultAsync(u => u.Id == command.UploadId, ct);
                if (upload != null)
                {
                    upload.Status = ProcessingStatus.Failed;
                    upload.FinishedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }
            catch
            {
                // swallow secondary errors
            }
        }
    }
}