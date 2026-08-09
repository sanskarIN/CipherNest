namespace CipherNest.App.Services;

public interface IPrivacySafeExceptionReporter
{
    void Report(string operation, Exception exception, bool fatal = false);
}
