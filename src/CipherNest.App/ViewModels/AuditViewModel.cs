using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class AuditViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly ISecurityAuditService _audit;
    public ObservableCollection<SecurityAuditFinding> Findings { get; } = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string summary = "Run a local audit to find weak, reused, or overdue secrets.";

    public AuditViewModel(IVaultService vault, ISecurityAuditService audit)
    {
        _vault = vault;
        _audit = audit;
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
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");
}
