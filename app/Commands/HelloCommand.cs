using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace app.Commands;

public class Settings : LogCommandSettings
{
    [CommandArgument(0, $"[{nameof(Name)}]")] 
    public string Name { get; set; }
}

public partial class HelloCommand : Command<Settings>
{
    public override int Execute(CommandContext context, Settings settings) => Execute(settings);
}

public partial class HelloCommand
{
    private readonly ILogger logger;
    private readonly IAnsiConsole console;
    
    public HelloCommand(IAnsiConsole console, ILogger<HelloCommand> logger)
    {
        this.console = console;
        this.logger = logger;
        this.logger.LogDebug("{command} initialized", nameof(HelloCommand));
    }

    public int Execute(Settings settings)
    {
        logger.LogInformation("Starting my command");
        console.MarkupLine($"Hello, [red]{settings.Name}[/]");
        logger.LogInformation("Completed my command");

        return 0;
    }
}