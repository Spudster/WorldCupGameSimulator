using System.Text;
using Spectre.Console;
using WorldCup.Cli;
using WorldCup.Data;
using WorldCup.Data.Providers;

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch
{
    // Some redirected consoles don't allow changing the encoding; ignore.
}

try
{
    // Teams/squads/bracket come from the bundled seed; results & fixtures are pulled live on startup
    // when a football-data.org API key is configured (otherwise the bundled real data is used).
    var provider = new SeedTeamDataProvider();
    var live = new LiveResultsService(new SimpleHttpClientFactory());
    var session = new Session(provider, $"Data source: {provider.Name}", live);

    if (args.Contains("--smoke"))
    {
        Smoke.Run(session);
        return 0;
    }

    new App(session).Run();
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red3]Fatal error:[/] {Markup.Escape(ex.Message)}");
    return 1;
}

/// <summary>Minimal <see cref="IHttpClientFactory"/> so the live provider works without a DI host.</summary>
internal sealed class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
