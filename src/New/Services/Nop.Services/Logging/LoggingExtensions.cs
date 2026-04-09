using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Convenience extension methods for INopLogger — shorthand for each log level.
/// </summary>
public static class LoggingExtensions
{
    public static void Debug(this INopLogger logger, string message, Exception? exception = null, Customer? customer = null) =>
        FilteredLog(logger, LogLevel.Debug, message, exception, customer);

    public static void Information(this INopLogger logger, string message, Exception? exception = null, Customer? customer = null) =>
        FilteredLog(logger, LogLevel.Information, message, exception, customer);

    public static void Warning(this INopLogger logger, string message, Exception? exception = null, Customer? customer = null) =>
        FilteredLog(logger, LogLevel.Warning, message, exception, customer);

    public static void Error(this INopLogger logger, string message, Exception? exception = null, Customer? customer = null) =>
        FilteredLog(logger, LogLevel.Error, message, exception, customer);

    public static void Fatal(this INopLogger logger, string message, Exception? exception = null, Customer? customer = null) =>
        FilteredLog(logger, LogLevel.Fatal, message, exception, customer);

    private static void FilteredLog(INopLogger logger, LogLevel level, string message, Exception? exception, Customer? customer)
    {
        if (exception is ThreadAbortException)
            return;

        if (logger.IsEnabled(level))
        {
            var fullMessage = exception?.ToString() ?? string.Empty;
            logger.InsertLog(level, message, fullMessage, customer);
        }
    }
}
