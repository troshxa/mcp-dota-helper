using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;

[McpServerToolType]
public class DotaTools
{
    [McpServerTool(Name = "get_top_heroes")]
    [Description("Get the top most-played ranked heroes for a Dota 2 player")]
    public static Task<string> GetTopHeroes(
        [Description("SteamID64 or Account ID of the player")] string playerId,
        [Description("Number of heroes to return (default: 5)")]  int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Task.FromResult("Error: playerId is required.");

        return RunCliAsync($"top-heroes {playerId} --limit {limit}");
    }

    [McpServerTool(Name = "get_recent_matches")]
    [Description("Get the most recent ranked matches for a Dota 2 player")]
    public static Task<string> GetRecentMatches(
        [Description("SteamID64 or Account ID of the player")] string playerId,
        [Description("Number of matches to return (default: 5)")] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Task.FromResult("Error: playerId is required.");

        return RunCliAsync($"recent-matches {playerId} --limit {limit}");
    }

    private static async Task<string> RunCliAsync(string arguments)
    {
        var cliBinary = GetCliBinaryPath();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = cliBinary,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            }
        };

        process.Start();

        // Read both streams concurrently to avoid deadlocks on large output
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string output = await stdoutTask;
        string error  = await stderrTask;

        return process.ExitCode switch
        {
            0 => output.Trim(),                                        // Success
            1 => $"API error: {error.Trim()}",                        // ApiError
            2 => $"Invalid arguments: {error.Trim()}",                // InvalidArguments
            3 => $"No data found: {error.Trim()}",                    // NoDataFound
            _ => $"CLI error (exit {process.ExitCode}): {error.Trim()}"
        };
    }

    private static string GetCliBinaryPath()
    {
        // Check environment variable first — allows flexible deployment
        var envPath = Environment.GetEnvironmentVariable("DOTACLI_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // Default: look for dotacli.exe in the sibling project's Debug output.
        // Both projects share the same solution root (3 dirs up from bin/Debug/net10.0/).
        string exeName     = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotacli.exe" : "dotacli";
        string solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string defaultPath = Path.Combine(solutionDir, "dotacli", "bin", "Debug", "net10.0", exeName);

        if (File.Exists(defaultPath))
            return defaultPath;

        throw new FileNotFoundException(
            $"dotacli binary not found. " +
            $"Run 'dotnet build' inside the dotacli project, " +
            $"or set the DOTACLI_PATH environment variable.\n" +
            $"Searched: {defaultPath}");
    }
}
