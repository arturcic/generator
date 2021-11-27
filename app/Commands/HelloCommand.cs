using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace app.Commands;

public class HelloCommand : Command<HelloCommand.HelloSettings>
{
    private ILogger<HelloCommand> _logger;
    private IAnsiConsole _console;

    public HelloCommand(IAnsiConsole console, ILogger<HelloCommand> logger)
    {
        _console = console;
        _logger = logger;
        _logger.LogDebug("{0} initialized", nameof(HelloCommand));
    }

    public class HelloSettings : LogCommandSettings
    {
        [CommandArgument(0, "[Name]")]
        public string Name { get; set; }
    }


    public override int Execute(CommandContext context, HelloSettings helloSettings)
    {
        _logger.LogInformation("Starting my command");
        _console.MarkupLine($"Hello, [blue]{helloSettings.Name}[/]");
        _logger.LogInformation("Completed my command");

        return 0;
    }
}