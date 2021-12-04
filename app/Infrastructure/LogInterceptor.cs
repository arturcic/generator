using app.Commands;
using Serilog.Core;
using Spectre.Console.Cli;

namespace app.Infrastructure;

public class LogInterceptor : ICommandInterceptor
{
    public static readonly LoggingLevelSwitch LogLevel = new();

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is LogCommandSettings logSettings)
        {
            LoggingEnricher.Path = logSettings.LogFile ?? "application.log";
            
            if (logSettings.LogLevel != null)
            {
                LogLevel.MinimumLevel = logSettings.LogLevel.Value;
            }
        }
    }
}