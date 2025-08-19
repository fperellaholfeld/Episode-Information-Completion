
namespace api.Models
{

    /// <Summary> Represents an enriched archive of the incomplete episode data found in an uploaded CSV file. </Summary>
    public class EnrichedArchives
    {
        public int id { get; set; }

        public required Episode[] episodes { get; set; } = Array.Empty<Episode>();

        public required int totalLocations { get; set; }

        public required int totalFemaleCharacters { get; set; }

        public required int totalMaleCharacters { get; set; }

        public required int totalGenderlessCharacters { get; set; }
        public required int totalGenderUnknownCharacters { get; set; }

        public required string uploadedFilePath { get; set; }
    }

    /// <summary>
    /// Represents an episode from Rick and Morty from the API
    /// </summary>
    public class Episode
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string air_date { get; set; }
        public required string episode { get; set; }

        public required Character[] characters { get; set; }
    }

    /// <summary>
    /// Represents a character from Rick and Morty from the API
    /// </summary>
    public class Character
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string status { get; set; }
        public required string species { get; set; }
        public required string type { get; set; }
        public required string gender { get; set; }
    }

    public class Origin
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string type { get; set; }
        public required string dimension { get; set; }
    }

    public class Location
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string type { get; set; }
        public required string dimension { get; set; }
    }
}