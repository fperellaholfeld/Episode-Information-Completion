namespace api.Models
{
    /// <summary>
    /// Represents a location in Rick and Morty.
    /// </summary>
    public sealed class Location
    {
        public int Id { get; set; }                  // Rick & Morty API id
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Dimension { get; set; } = default!;

        /// <summary>
        /// Characters that originated from this location.
        /// </summary>
        public ICollection<Character> AsOrigin { get; set; } = new List<Character>();

        /// <summary>
        /// Characters that are shown to be in this location during an episode.
        /// </summary>
        public ICollection<Character> AsCurrent { get; set; } = new List<Character>();
    }
}