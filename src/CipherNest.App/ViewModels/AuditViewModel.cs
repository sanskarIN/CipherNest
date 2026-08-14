using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class AuditViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly ISecurityAuditService _audit;
    private readonly IPrivacySafeExceptionReporter _exceptions;
    public ObservableCollection<SecurityAuditFinding> Findings { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; } = "Run a local audit to find weak, reused, or overdue secrets.";

    public AuditViewModel(IVaultService vault, ISecurityAuditService audit, IPrivacySafeExceptionReporter exceptions)
    {
        _vault = vault;
        _audit = audit;
        _exceptions = exceptions;
    }

    [RelayCommand]
    public async Task RunAsync()
    {
        if (!_vault.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//unlock");
            return;
        }
        IsBusy = true;
        try
        {
            var items = await _vault.GetItemsAsync();
            var findings = _audit.Analyze(items, DateTimeOffset.UtcNow);
            Findings.Clear();
            foreach (var finding in findings) Findings.Add(finding);
            Summary = findings.Count == 0 ? "No findings were detected by the local checks." : $"{findings.Count} local security finding(s). Review them before changing credentials.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Audit.Run", ex);
            Findings.Clear();
            Summary = "The local security audit could not be completed safely. Lock and unlock the vault, then retry.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");
}
