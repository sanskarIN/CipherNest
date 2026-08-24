using System.Collections.ObjectModel;
using System.Globalization;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public sealed record AuditFindingPresentation(string Kind, string Message, string Severity);

public partial class AuditViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly ISecurityAuditService _audit;
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private readonly ILocalizationService _localization;

    public ObservableCollection<AuditFindingPresentation> Findings { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; }

    public AuditViewModel(
        IVaultService vault,
        ISecurityAuditService audit,
        IPrivacySafeExceptionReporter exceptions,
        ILocalizationService localization)
    {
        _vault = vault;
        _audit = audit;
        _exceptions = exceptions;
        _localization = localization;
        Summary = AuditText("AuditInitialSummary");
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
            foreach (var finding in findings)
            {
                Findings.Add(ToPresentation(finding));
            }

            Summary = findings.Count == 0
                ? AuditText("AuditNoFindingsSummary")
                : string.Format(
                    CultureInfo.CurrentUICulture,
                    AuditText("AuditFindingsSummaryFormat"),
                    findings.Count);
        }
        catch (Exception ex)
        {
            _exceptions.Report("Audit.Run", ex);
            Findings.Clear();
            Summary = AuditText("AuditFailureSummary");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private AuditFindingPresentation ToPresentation(SecurityAuditFinding finding)
    {
        var kindKey = finding.Kind switch
        {
            SecurityFindingKind.MissingTitle => "AuditKindMissingTitle",
            SecurityFindingKind.WeakSecret => "AuditKindWeakSecret",
            SecurityFindingKind.ExpiredReview => "AuditKindExpiredReview",
            SecurityFindingKind.ReusedSecret => "AuditKindReusedSecret",
            SecurityFindingKind.DuplicateEntry => "AuditKindDuplicateEntry",
            _ => string.Empty
        };

        var messageKey = finding.Kind switch
        {
            SecurityFindingKind.MissingTitle => "AuditMessageMissingTitle",
            SecurityFindingKind.WeakSecret => "AuditMessageWeakSecret",
            SecurityFindingKind.ExpiredReview => "AuditMessageExpiredReview",
            SecurityFindingKind.ReusedSecret => "AuditMessageReusedSecret",
            SecurityFindingKind.DuplicateEntry => "AuditMessageDuplicateEntry",
            _ => string.Empty
        };

        var kind = kindKey.Length == 0 ? finding.Kind.ToString() : AuditText(kindKey);
        var message = messageKey.Length == 0 ? finding.Message : AuditText(messageKey);
        var severity = string.Format(
            CultureInfo.CurrentUICulture,
            AuditText("AuditSeverityFormat"),
            finding.Severity);

        return new(kind, message, severity);
    }

    private string AuditText(string key) => _localization.Get(key);
}
