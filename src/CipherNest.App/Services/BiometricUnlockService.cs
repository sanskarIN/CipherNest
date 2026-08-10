using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace CipherNest.App.Services;

public sealed class BiometricUnlockService : IBiometricUnlockService
{
    private const string SecondarySecretKey = "ciphernest.biometric.secondary.v1";

    public bool IsSupported
    {
        get
        {
#if ANDROID
            return OperatingSystem.IsAndroidVersionAtLeast(28);
#elif IOS || MACCATALYST
            return true;
#else
            return false;
#endif
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        // BiometricPrompt is available from API 28. Avoid preflighting with
        // BiometricManager because that API was introduced later. Enrollment,
        // lockout, hardware availability, and policy errors are reported by the
        // prompt itself and fall back to the master passphrase.
        return Task.FromResult(OperatingSystem.IsAndroidVersionAtLeast(28) && Platform.CurrentActivity is not null);
#elif IOS || MACCATALYST
        using var context = new LocalAuthentication.LAContext();
        return Task.FromResult(context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _));
#else
        return Task.FromResult(false);
#endif
    }

    public async Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(28)) return false;
        var activity = Platform.CurrentActivity;
        if (activity is null) return false;

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var signal = new Android.OS.CancellationSignal();
        using var registration = cancellationToken.Register(signal.Cancel);
        var callback = new AndroidCallback(completion);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var builder = new Android.Hardware.Biometrics.BiometricPrompt.Builder(activity)
                .SetTitle("Unlock CipherNest")
                .SetSubtitle(reason)
                .SetNegativeButton("Use master passphrase", activity.MainExecutor, new NegativeButtonListener(completion));
            builder.Build().Authenticate(signal, activity.MainExecutor, callback);
        });

        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
#elif IOS || MACCATALYST
        using var context = new LocalAuthentication.LAContext();
        if (!context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _)) return false;
        using var registration = cancellationToken.Register(context.Invalidate);
        var result = await context.EvaluatePolicyAsync(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Item1;
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    public async Task StoreSecondarySecretAsync(string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        cancellationToken.ThrowIfCancellationRequested();
        await SecureStorage.Default.SetAsync(SecondarySecretKey, secret).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ReadSecondarySecretAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await SecureStorage.Default.GetAsync(SecondarySecretKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    public Task ClearSecondarySecretAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SecureStorage.Default.Remove(SecondarySecretKey);
        return Task.CompletedTask;
    }

#if ANDROID
    [Android.Runtime.Preserve(AllMembers = true)]
    private sealed class AndroidCallback(TaskCompletionSource<bool> completion) : Android.Hardware.Biometrics.BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(Android.Hardware.Biometrics.BiometricPrompt.AuthenticationResult? result) => completion.TrySetResult(true);
        public override void OnAuthenticationFailed() { }
        public override void OnAuthenticationError([Android.Runtime.GeneratedEnum] Android.Hardware.Biometrics.BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString) => completion.TrySetResult(false);
    }

    [Android.Runtime.Preserve(AllMembers = true)]
    private sealed class NegativeButtonListener(TaskCompletionSource<bool> completion) : Java.Lang.Object, Android.Content.IDialogInterfaceOnClickListener
    {
        public void OnClick(Android.Content.IDialogInterface? dialog, int which) => completion.TrySetResult(false);
    }
#endif
}
