using Microsoft.Extensions.Logging;

namespace CipherNest.App.Services;

public sealed class PrivacySafeExceptionReporter(ILogger<PrivacySafeExceptionReporter> logger) : IPrivacySafeExceptionReporter
{
    private static readonly EventId NonFatalEvent = new(2001, "UnhandledNonFatal");
    private static readonly EventId FatalEvent = new(2002, "UnhandledFatal");

    public void Report(string operation, Exception exception, bool fatal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(exception);
        var safeOperation = SanitizeOperation(operation);
        var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        var hResult = exception.HResult;

        if (fatal)
            logger.LogCritical(FatalEvent, "{Operation} failed with {ExceptionType}; HResult={HResult}. Exception message and stack were intentionally omitted.", safeOperation, exceptionType, hResult);
        else
            logger.LogError(NonFatalEvent, "{Operation} failed with {ExceptionType}; HResult={HResult}. Exception message and stack were intentionally omitted.", safeOperation, exceptionType, hResult);
    }

    private static string SanitizeOperation(string operation)
    {
        var value = operation.Trim();
        if (value.Length > 80) value = value[..80];
        return new string(value.Select(static ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_').ToArray());
    }
}
