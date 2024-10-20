using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Tkmm.Core.Logging;

public class DesktopLogger : ILogger
{
    private static readonly string _targetLogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");

    private readonly string _group;
    private readonly StreamWriter _writer;

    public DesktopLogger(string group)
    {
        _group = group;

        FileStream fs = File.Create(_targetLogFile);
        _writer = new StreamWriter(fs);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string log = $"""
            [{eventId.Id,2}: {logLevel,-12}] [{DateTime.UtcNow:s}]
                 {_group} - {formatter(state, exception).HideUsername()}
            """;

#if DEBUG
        Debug.WriteLine(log);
#endif

        _writer.WriteLine(log);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }
}