using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>Proveedor de logs que guarda los mensajes ya formateados para revisarlos en las pruebas.</summary>
public sealed class LogsEnMemoria : ILoggerProvider
{
    public ConcurrentQueue<string> Mensajes { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Registrador(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Registrador(LogsEnMemoria proveedor, string categoria) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            proveedor.Mensajes.Enqueue($"{logLevel} {categoria}: {formatter(state, exception)} {exception}");
        }
    }
}
