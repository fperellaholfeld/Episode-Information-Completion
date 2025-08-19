namespace api.Entities
{
    /// <summary>
    /// Represents a character in Rick and Morty
    /// </summary>
    public sealed class Character
    {
        public int Id { get; set; }                 // Rick & Morty API id
        public string Name { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string Species { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Gender { get; set; } = default!; // "Female","Male","Genderless","unknown"

        public int OriginLocationId { get; set; }
        public Location Origin { get; set; } = default!;

        public int LocationId { get; set; }         // current location
        public Location Location { get; set; } = default!;

        public ICollection<EpisodeCharacter> EpisodeCharacters { get; set; } = new List<EpisodeCharacter>();
    }
}