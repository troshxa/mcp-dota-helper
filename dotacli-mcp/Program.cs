using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// MCP server — standalone wrapper around the dotacli CLI binary.
// It has no knowledge of Dota API internals; all data comes from subprocess calls.

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<DotaTools>();

// Redirect logs to stderr so stdout stays clean for the JSON-RPC protocol
builder.Logging.ClearProviders();
builder.Logging.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();
