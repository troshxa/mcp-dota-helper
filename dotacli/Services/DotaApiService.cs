using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DotaCli.Services;

public class DotaApiService : IDotaApiService
{
    private readonly HttpClient              _httpClient;
    private readonly ILogger<DotaApiService> _logger;
    private readonly JsonSerializerOptions   _jsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public DotaApiService(HttpClient httpClient, ILogger<DotaApiService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<List<TopHeroDto>?> GetTopHeroesAsync(string rawId, int limit)
    {
        string accountId = ConvertToAccountId(rawId);
        string url       = $"players/{accountId}/heroes?lobby_type=7";

        try
        {
            var heroNames    = await _httpClient.GetFromJsonAsync<List<HeroInfo>>("heroes", _jsonOptions);
            var playerHeroes = await _httpClient.GetFromJsonAsync<List<PlayerHero>>(url, _jsonOptions);

            if (heroNames == null || playerHeroes == null)
                return null;

            return playerHeroes
                .Where(h => h.games > 0)
                .OrderByDescending(h => h.games)
                .Take(limit)
                .Select(h => new TopHeroDto(
                    HeroName: heroNames.FirstOrDefault(x => x.id == h.hero_id)?.localized_name ?? "Unknown",
                    Games:    h.games,
                    Win:      h.win))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top heroes from OpenDota API.");
            return null;
        }
    }

    public async Task<List<RecentMatchDto>?> GetRecentRankedMatchesAsync(string rawId, int limit)
    {
        string accountId = ConvertToAccountId(rawId);
        string url       = $"players/{accountId}/matches?lobby_type=7&limit={limit}";

        try
        {
            var heroNames = await _httpClient.GetFromJsonAsync<List<HeroInfo>>("heroes", _jsonOptions);
            var matches   = await _httpClient.GetFromJsonAsync<List<RecentMatch>>(url, _jsonOptions);

            if (heroNames == null || matches == null)
                return null;

            return matches.Select(match =>
            {
                string heroName = heroNames.FirstOrDefault(h => h.id == match.hero_id)?.localized_name ?? "Unknown";
                string kda      = $"{match.kills ?? 0} / {match.deaths ?? 0} / {match.assists ?? 0}";

                bool? isWin = null;
                if (match.radiant_win.HasValue)
                {
                    bool isRadiant = match.player_slot < 128;
                    isWin = isRadiant == match.radiant_win.Value;
                }

                return new RecentMatchDto(heroName, kda, isWin);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent matches from OpenDota API.");
            return null;
        }
    }

    private static string ConvertToAccountId(string input)
    {
        if (ulong.TryParse(input, out ulong id) && id > 76561197960265728UL)
            return (id - 76561197960265728UL).ToString();
        return input;
    }
}
