namespace DotaCli.Services;

// DTO — clean objects returned from service to callers
public record TopHeroDto(string HeroName, int Games, int Win);
public record RecentMatchDto(string HeroName, string Kda, bool? IsWin);

// Raw JSON models that mirror the OpenDota API response structure
internal class HeroInfo
{
    public int    id             { get; set; }
    public string localized_name { get; set; } = string.Empty;
}

internal class PlayerHero
{
    public int hero_id { get; set; }
    public int games   { get; set; }
    public int win     { get; set; }
}

internal class RecentMatch
{
    public long  match_id    { get; set; }
    public int   player_slot { get; set; }
    public bool? radiant_win { get; set; }
    public int   hero_id     { get; set; }
    public int?  kills       { get; set; }
    public int?  deaths      { get; set; }
    public int?  assists     { get; set; }
}
