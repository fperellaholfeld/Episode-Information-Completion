namespace api.Entities
{

    /// <summary>
    /// Relates characters to the episodes they are featured in.
    /// </summary>
    public sealed class EpisodeCharacter
    {
        public int EpisodeId { get; set; }
        public Episode Episode { get; set; } = default!;

        public int CharacterId { get; set; }
        public Character Character { get; set; } = default!;
    }

    /// <summary>
    /// Relates uploads to the episodes they contain.
    /// </summary>
    public sealed class UploadEpisode
    {
        public Guid UploadId { get; set; }
        public UploadHistory Upload { get; set; } = default!;

        public int EpisodeId { get; set; }
        public Episode Episode { get; set; } = default!;
    }
}