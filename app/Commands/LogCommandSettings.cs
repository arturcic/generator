using System.ComponentModel;
using System.Globalization;
using Serilog.Events;
using Spectre.Console.Cli;

namespace app.Commands;

public class LogCommandSettings : CommandSettings
{
    [CommandOption("--logFile")]
    [Description("Path and file name for logging")]
    public string? LogFile { get; set; }

    [CommandOption("--logLevel")]
    [Description("Minimum level for logging")]
    [TypeConverter(typeof(VerbosityConverter))]
    [DefaultValue(LogEventLevel.Information)]
    public LogEventLevel? LogLevel { get; set; }
}

public sealed class VerbosityConverter : TypeConverter
{
    private readonly Dictionary<string, LogEventLevel> lookup;

    public VerbosityConverter()
    {
        lookup = new Dictionary<string, LogEventLevel>(StringComparer.OrdinalIgnoreCase)
        {
            {"d", LogEventLevel.Debug},
            {"debug", LogEventLevel.Debug},
            {"v", LogEventLevel.Verbose},
            {"verbose", LogEventLevel.Verbose},
            {"i", LogEventLevel.Information},
            {"info", LogEventLevel.Information},
            {"w", LogEventLevel.Warning},
            {"warn", LogEventLevel.Warning},
            {"e", LogEventLevel.Error},
            {"error", LogEventLevel.Error},
            {"f", LogEventLevel.Fatal},
            {"fatal", LogEventLevel.Fatal}
        };
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        switch (value)
        {
            case LogEventLevel logEventLevel:
                return logEventLevel;
            case string stringValue:
                {
                    var result = lookup.TryGetValue(stringValue, out var verbosity);
                    if (!result)
                    {
                        const string format = "The value '{0}' is not a valid verbosity.";
                        var message = string.Format(CultureInfo.InvariantCulture, format, value);
                        throw new InvalidOperationException(message);
                    }
                    return verbosity;
                }
            default:
                throw new NotSupportedException("Can't convert value to verbosity.");
        }
    }
}