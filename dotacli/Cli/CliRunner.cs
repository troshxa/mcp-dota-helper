using System.CommandLine;
using DotaCli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotaCli.Cli;

public static class CliRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddHttpClient<IDotaApiService, DotaApiService>(client =>
                {
                    client.BaseAddress = new Uri("https://api.opendota.com/api/");
                    client.DefaultRequestHeaders.Add("User-Agent", "DotaCli-App");
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();

        // Captured by the action lambda that runs — only one command executes per invocation
        int commandExitCode = ExitCodes.Success;

        // --- top-heroes ---
        var idArg1    = new Argument<string>("id")      { Description = "SteamID64 or Account ID of the player" };
        var limitOpt1 = new Option<int>("--limit")      { Description = "Number of heroes to show", DefaultValueFactory = _ => 5 };
        var topHeroesCmd = new Command("top-heroes", "Show the player's most-played ranked heroes");
        topHeroesCmd.Add(idArg1);
        topHeroesCmd.Add(limitOpt1);
        topHeroesCmd.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var api = host.Services.GetRequiredService<IDotaApiService>();
            commandExitCode = await CliHandlers.TopHeroesAsync(api, result.GetValue(idArg1)!, result.GetValue(limitOpt1));
        });

        // --- recent-matches ---
        var idArg2    = new Argument<string>("id")      { Description = "SteamID64 or Account ID of the player" };
        var limitOpt2 = new Option<int>("--limit")      { Description = "Number of matches to show", DefaultValueFactory = _ => 5 };
        var recentMatchesCmd = new Command("recent-matches", "Show recently played ranked matches");
        recentMatchesCmd.Add(idArg2);
        recentMatchesCmd.Add(limitOpt2);
        recentMatchesCmd.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var api = host.Services.GetRequiredService<IDotaApiService>();
            commandExitCode = await CliHandlers.RecentMatchesAsync(api, result.GetValue(idArg2)!, result.GetValue(limitOpt2));
        });

        var root = new RootCommand("Dota 2 CLI — utility for retrieving player statistics");
        root.Add(topHeroesCmd);
        root.Add(recentMatchesCmd);

        // InvokeAsync returns non-zero only for parse errors (unknown command, missing arg, etc.)
        int parseExitCode = await root.Parse(args).InvokeAsync();
        return parseExitCode != 0 ? ExitCodes.InvalidArguments : commandExitCode;
    }
}
