
namespace api.Entities
{
    /// <summary>
    /// Represents a single episode of Rick and Morty.
    /// </summary>
    public sealed class Episode
    {
        public int Id { get; set; } // ID from API
        public string Name { get; set; } = default!; // Name of the Episode
        public string AirDate { get; set; } = default!;
        public string EpisodeCode { get; set; } = default!; // maps to JSON "episode", just can't use same name as class

        public ICollection<EpisodeCharacter> EpisodeCharacters { get; set; } = new List<EpisodeCharacter>();
        public ICollection<UploadEpisode> Uploads { get; set; } = new List<UploadEpisode>();
    }
}