using System.Threading.Channels;

namespace api.Services.Background;

/// <summary>
/// Represents a command to process an uploaded file.
/// </summary>
public readonly record struct ProcessUploadCommand(int UploadId, string FilePath);

/// <summary>
/// Async Job Queue for processing uploaded files.
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// Enqueues a new upload processing job.
    /// </summary>
    ValueTask EnqueueAsync(ProcessUploadCommand command, CancellationToken ct);

    /// <summary>
    /// Dequeues an upload processing job.
    /// </summary>
    IAsyncEnumerable<ProcessUploadCommand> DequeueAsync(CancellationToken ct);
}


public sealed class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<ProcessUploadCommand> _channel;

    public InMemoryJobQueue(int capacity = 100)
    {
        var opts = new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        };
        _channel = Channel.CreateBounded<ProcessUploadCommand>(opts);
    }

    public ValueTask EnqueueAsync(ProcessUploadCommand command, CancellationToken ct)
    {
        return _channel.Writer.WriteAsync(command, ct);
    }

    public IAsyncEnumerable<ProcessUploadCommand> DequeueAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
