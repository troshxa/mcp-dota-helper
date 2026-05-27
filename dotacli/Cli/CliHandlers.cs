using DotaCli.Services;

namespace DotaCli.Cli;

public static class CliHandlers
{
    public static async Task<int> TopHeroesAsync(IDotaApiService api, string id, int limit)
    {
        // Skip decorative output when called as a subprocess (stdout is redirected)
        if (!Console.IsOutputRedirected)
            WriteColor(ConsoleColor.Cyan, $"Loading top {limit} heroes for player {id}...\n");

        var heroes = await api.GetTopHeroesAsync(id, limit);

        if (heroes == null)
        {
            Console.Error.WriteLine("Error: failed to fetch data from the OpenDota API.");
            return ExitCodes.ApiError;
        }

        if (heroes.Count == 0)
        {
            Console.Error.WriteLine($"No ranked hero data found for player {id}.");
            return ExitCodes.NoDataFound;
        }

        WriteColor(ConsoleColor.Green, $"{"Hero",-20} | {"Games",-6} | {"Wins",-8} | {"Winrate",-7}");
        Console.WriteLine(new string('-', 49));

        foreach (var stat in heroes)
        {
            double winrate = Math.Round((double)stat.Win / stat.Games * 100, 1);
            Console.WriteLine($"{stat.HeroName,-20} | {stat.Games,-6} | {stat.Win,-8} | {winrate,5}%");
        }

        Console.WriteLine(new string('-', 49));
        return ExitCodes.Success;
    }

    public static async Task<int> RecentMatchesAsync(IDotaApiService api, string id, int limit)
    {
        if (!Console.IsOutputRedirected)
            WriteColor(ConsoleColor.Cyan, $"Loading the last {limit} RANKED matches for player {id}...\n");

        var matches = await api.GetRecentRankedMatchesAsync(id, limit);

        if (matches == null)
        {
            Console.Error.WriteLine("Error: failed to fetch data from the OpenDota API.");
            return ExitCodes.ApiError;
        }

        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"No ranked matches found for player {id}.");
            return ExitCodes.NoDataFound;
        }

        WriteColor(ConsoleColor.White, $"{"Result",-10} | {"Hero",-20} | {"K / D / A",-15}");
        Console.WriteLine(new string('-', 52));

        foreach (var match in matches)
        {
            ConsoleColor color = match.IsWin == true  ? ConsoleColor.Green
                               : match.IsWin == false ? ConsoleColor.Red
                               :                        ConsoleColor.Gray;

            string result = match.IsWin == true ? "Win" : match.IsWin == false ? "Loss" : "Unknown";
            WriteColor(color, $"{result,-10}", newLine: false);
            Console.WriteLine($" | {match.HeroName,-20} | {match.Kda,-15}");
        }

        Console.WriteLine(new string('-', 52));
        return ExitCodes.Success;
    }

    private static void WriteColor(ConsoleColor color, string text, bool newLine = true)
    {
        if (!Console.IsOutputRedirected) Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text);
        else         Console.Write(text);
        if (!Console.IsOutputRedirected) Console.ResetColor();
    }
}
