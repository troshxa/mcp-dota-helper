namespace DotaCli.Services;

public interface IDotaApiService
{
    Task<List<TopHeroDto>?>    GetTopHeroesAsync(string rawId, int limit);
    Task<List<RecentMatchDto>?> GetRecentRankedMatchesAsync(string rawId, int limit);
}
