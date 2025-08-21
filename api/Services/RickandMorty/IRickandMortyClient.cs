namespace api.Services.RickandMorty;

public interface IRickandMortyClient
{
    Task<List<ApiEpisode>> GetEpisodesAsync(IEnumerable<int> ids);
    Task<List<ApiCharacter>> GetCharactersAsync(IEnumerable<int> ids);
    Task<List<ApiLocation>> GetLocationsAsync(IEnumerable<int> ids);
}
