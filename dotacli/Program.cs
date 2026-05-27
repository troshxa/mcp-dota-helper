using DotaCli.Cli;

namespace DotaCli;

class Program
{
    static async Task<int> Main(string[] args) => await CliRunner.RunAsync(args);
}
