using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DotaCli;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Initialize configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
            .Build();
        string? apiKey = config["OpenDota:ApiKey"];

        // 2. If no arguments are provided, or the user asks for help, show the manual
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintHelp();
            return;
        }

        // 3. Manual argument parsing
        string command = args[0].ToLower(); // The first argument is the command

        if (command == "top-heroes")
        {
            if (!TryGetId(args, out string id)) return;
            int limit = ParseLimitOption(args, defaultLimit: 5);
            await ExecuteTopHeroesLookup(id, limit, apiKey);
        }
        else if (command == "recent-matches")
        {
            if (!TryGetId(args, out string id)) return;
            int limit = ParseLimitOption(args, defaultLimit: 10);
            await ExecuteRecentMatchesLookup(id, limit, apiKey);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Unknown command '{command}'.\n");
            Console.ResetColor();
            PrintHelp();
        }
    }

    // ==========================================
    // ARGUMENT PARSING LOGIC
    // ==========================================
    
    // Check if the user provided an ID
    static bool TryGetId(string[] args, out string id)
    {
        id = string.Empty;
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: You did not specify a player ID.");
            Console.ResetColor();
            Console.WriteLine("Example: dotnet run -- top-heroes 76561199097234516");
            return false;
        }
        id = args[1];
        return true;
    }

    // Look for the --limit or -l flag in the arguments array
    static int ParseLimitOption(string[] args, int defaultLimit)
    {
        for (int i = 2; i < args.Length - 1; i++)
        {
            if (args[i] == "--limit" || args[i] == "-l")
            {
                if (int.TryParse(args[i + 1], out int parsedLimit))
                {
                    return parsedLimit;
                }
            }
        }
        return defaultLimit;
    }

    // Beautiful help output
    static void PrintHelp()
    {
        Console.WriteLine("Dota 2 CLI - Utility for retrieving player statistics");
        Console.WriteLine("Usage: dotnet run -- <command> <player_ID> [options]\n");
        Console.WriteLine("Available commands:");
        Console.WriteLine("  top-heroes <id>      Show the player's best heroes");
        Console.WriteLine("  recent-matches <id>  Show recently played matches\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  -l, --limit <number> Number of records to output (default: 5 for heroes, 10 for matches)");
        Console.WriteLine("  -h, --help           Show this help message");
    }

    // ==========================================
    // API LOGIC
    // ==========================================

    static async Task ExecuteRecentMatchesLookup(string rawId, int limit, string? apiKey)
    {
        string accountId = ConvertSteamId64ToAccountId(rawId);
        string keyParam = !string.IsNullOrEmpty(apiKey) ? (apiKey.StartsWith("?") ? apiKey : $"?api_key={apiKey}") : "";

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "DotaCli-App");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Loading the last {limit} matches for player {accountId}...\n");
        Console.ResetColor();

        try
        {
            var heroesList = await client.GetFromJsonAsync<List<HeroInfo>>("https://api.opendota.com/api/heroes");
            string recentUrl = $"https://api.opendota.com/api/players/{accountId}/recentMatches{keyParam}&lobby_type=7";
            Console.WriteLine($"Fetching data from: {recentUrl}\n");
            var matches = await client.GetFromJsonAsync<List<RecentMatch>>(recentUrl);

            if (heroesList == null || matches == null) return;

            var recentMatches = matches.Take(limit).ToList();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{"Result",-10} | {"Hero",-20} | {"K / D / A",-15}");
            Console.WriteLine(new string('-', 52));
            Console.ResetColor();

            foreach (var match in recentMatches)
            {
                string heroName = heroesList.FirstOrDefault(h => h.id == match.hero_id)?.localized_name ?? "Unknown";
                string kda = $"{match.kills} / {match.deaths} / {match.assists}";

                bool isRadiant = match.player_slot < 128;
                bool isWin = (isRadiant && match.radiant_win) || (!isRadiant && !match.radiant_win);

                if (isWin)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{"Win",-10}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{"Loss",-10}");
                }
                
                Console.ResetColor();
                Console.WriteLine($" | {heroName,-20} | {kda,-15}");
            }
            Console.WriteLine(new string('-', 52));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Error: API request limit exceeded (429). Please wait or check your key.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task ExecuteTopHeroesLookup(string rawId, int limit, string? apiKey)
    {
        string accountId = ConvertSteamId64ToAccountId(rawId);
        string keyParam = !string.IsNullOrEmpty(apiKey) ? (apiKey.StartsWith("?") ? apiKey : $"?api_key={apiKey}") : "";

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "DotaCli-App");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Loading top {limit} heroes for player {accountId}...\n");
        Console.ResetColor();

        try
        {
            var heroesList = await client.GetFromJsonAsync<List<HeroInfo>>("https://api.opendota.com/api/heroes");
            string playerUrl = $"https://api.opendota.com/api/players/{accountId}/heroes{keyParam}&lobby_type=7";
            // Створюємо гнучкі налаштування
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            // Передаємо ці налаштування у запит
            var playerHeroes = await client.GetFromJsonAsync<List<PlayerHero>>(playerUrl, jsonOptions);

            if (heroesList == null || playerHeroes == null) return;

            var topList = playerHeroes.Where(h => h.games > 0).OrderByDescending(h => h.games).Take(limit).ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{"Hero",-20} | {"Games",-6} | {"Wins",-8} | {"Winrate",-7}");
            Console.WriteLine(new string('-', 49));
            Console.ResetColor();

            foreach (var stat in topList)
            {
                string heroName = heroesList.FirstOrDefault(h => h.id == stat.hero_id)?.localized_name ?? "Unknown";
                double winrate = Math.Round((double)stat.win / stat.games * 100, 1);
                Console.WriteLine($"{heroName,-20} | {stat.games,-6} | {stat.win,-8} | {winrate,5}%");
            }
            Console.WriteLine(new string('-', 49));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Error: API request limit exceeded (429). Please wait or check your key.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string ConvertSteamId64ToAccountId(string input)
    {
        if (ulong.TryParse(input, out ulong id) && id > 76561197960265728)
            return (id - 76561197960265728).ToString();
        return input;
    }
}

// === Data Models ===
class HeroInfo { public int id { get; set; } public string localized_name { get; set; } = string.Empty; }
class PlayerHero { public int hero_id { get; set; } public int games { get; set; } public int win { get; set; } }
class RecentMatch { public long match_id { get; set; } public int player_slot { get; set; } public bool radiant_win { get; set; } public int hero_id { get; set; } public int kills { get; set; } public int deaths { get; set; } public int assists { get; set; } }