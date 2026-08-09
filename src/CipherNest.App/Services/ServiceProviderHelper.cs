using Microsoft.Extensions.DependencyInjection;

namespace CipherNest.App.Services;

public static class ServiceProviderHelper
{
    public static T GetRequiredService<T>() where T : notnull
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("Application services are not available.");
        return services.GetRequiredService<T>();
    }
}
