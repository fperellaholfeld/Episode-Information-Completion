using Microsoft.EntityFrameworkCore;

namespace api.Entities
{

    /// <summary>
    /// Status of the upload process
    /// </summary>
    public enum ProcessingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    /// <summary>
    /// Represents the history of a file upload.
    /// </summary>
    [Index(nameof(CreatedTimestamp))]
    public sealed class UploadHistory
    {
        public int Id { get; set; }

        public string FilePath { get; set; } = default!;

        public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

        /// <summary>
        /// Which episodes were requested by this upload
        /// </summary>
        public ICollection<UploadEpisode> UploadEpisodes { get; set; } = new List<UploadEpisode>();
    }
}