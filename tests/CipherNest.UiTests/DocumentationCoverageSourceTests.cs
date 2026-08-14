namespace CipherNest.UiTests;

public sealed class DocumentationCoverageSourceTests
{
    private static readonly string[][] RequiredDocumentation =
    [
        ["docs", "README.md"],
        ["docs", "COMPLETE_PROJECT_DOCUMENTATION.md"],
        ["docs", "FAQ.md"],
        ["docs", "USER_GUIDE.md"],
        ["docs", "DEVELOPER_GUIDE.md"],
        ["docs", "MAINTAINER_GUIDE.md"],
        ["docs", "DOCUMENTATION_MAINTENANCE.md"],
        ["docs", "API_REFERENCE.md"],
        ["docs", "LIMITS_AND_DEFAULTS.md"],
        ["docs", "PROJECT_GLOSSARY.md"],
        ["docs", "ACCESSIBILITY.md"],
        ["docs", "TESTING_GUIDE.md"],
        ["docs", "architecture", "ARCHITECTURE.md"],
        ["docs", "architecture", "DATABASE.md"],
        ["docs", "architecture", "LOCALIZATION.md"],
        ["docs", "architecture", "DATA_FLOW.md"],
        ["docs", "architecture", "DEPENDENCY_MAP.md"],
        ["docs", "architecture", "SESSION_AND_CONCURRENCY.md"],
        ["docs", "security", "THREAT_MODEL.md"],
        ["docs", "security", "CRYPTOGRAPHIC_DESIGN.md"],
        ["docs", "security", "TOTP.md"],
        ["docs", "security", "BIOMETRIC_UNLOCK.md"],
        ["docs", "security", "SECURE_NOTES.md"],
        ["docs", "security", "PASSPHRASE_GENERATOR.md"],
        ["docs", "security", "SESSION_SECURITY.md"],
        ["docs", "security", "DATA_LIFECYCLE.md"],
        ["docs", "privacy", "DIAGNOSTICS.md"],
        ["docs", "formats", "VAULT_RECORDS.md"],
        ["docs", "formats", "ATTACHMENTS.md"],
        ["docs", "formats", "ENCRYPTED_BACKUP.md"],
        ["docs", "formats", "CSV_TRANSFER.md"],
        ["docs", "verification", "CI_GATES.md"],
        ["docs", "verification", "SECURITY_HARDENING_2026_08_11.md"],
        ["docs", "verification", "DOCUMENTATION_SUITE_2026_08_12.md"],
        ["docs", "verification", "SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md"],
        ["docs", "verification", "HOSTED_CI_EVIDENCE_2026_08_13.md"],
        ["docs", "verification", "CURRENT_HEAD_2026_08_13.md"],
        ["docs", "verification", "POST_BASELINE_CHECKLIST_2026_08_13.md"],
        ["docs", "verification", "DOCUMENTATION_CONSOLIDATION_2026_08_14.md"],
        ["docs", "verification", "TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md"],
        ["docs", "operations", "BACKUP_RECOVERY_RUNBOOK.md"],
        ["docs", "operations", "SECURITY_RESPONSE.md"],
        ["docs", "releases", "PACKAGING.md"],
        ["docs", "releases", "REPRODUCIBLE_BUILDS.md"],
        ["docs", "releases", "STORE_LISTING_GUIDE.md"],
        ["docs", "releases", "RELEASE_PROCESS.md"],
        ["docs", "branding", "ASSETS.md"],
        ["docs", "TEST_PLAN.md"],
        ["docs", "RELEASE_CHECKLIST.md"],
        ["docs", "NEXT_STEPS.md"],
        ["docs", "TROUBLESHOOTING.md"],
        ["README.md"],
        ["CONTRIBUTING.md"],
        ["SECURITY.md"],
        ["PRIVACY.md"],
        ["SUPPORT.md"],
        ["TERMS.md"],
        ["THIRD_PARTY_NOTICES.md"],
        ["PROJECT_STATUS.md"],
        ["CHANGELOG.md"],
        ["what_changed.md"]
    ];

    [Fact]
    public void RequiredDocumentationFiles_ArePresentAndNonEmpty()
    {
        foreach (var segments in RequiredDocumentation)
        {
            var path = PathAt(segments);
            Assert.True(File.Exists(path), $"Required documentation file is missing: {string.Join("/", segments)}");
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(path)));
        }
    }

    [Fact]
    public void RootReadme_LinksCanonicalDocumentationEntryPoints()
    {
        var readme = File.ReadAllText(PathAt("README.md"));

        Assert.Contains("docs/README.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/COMPLETE_PROJECT_DOCUMENTATION.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/FAQ.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/USER_GUIDE.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/DEVELOPER_GUIDE.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/security/THREAT_MODEL.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/security/CRYPTOGRAPHIC_DESIGN.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/security/TOTP.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/formats/ENCRYPTED_BACKUP.md", readme, StringComparison.Ordinal);
        Assert.Contains("docs/releases/RELEASE_PROCESS.md", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentationHub_LinksAllMajorDocumentationAreas()
    {
        var hub = File.ReadAllText(PathAt("docs", "README.md"));

        foreach (var expected in new[]
                 {
                     "COMPLETE_PROJECT_DOCUMENTATION.md",
                     "FAQ.md",
                     "USER_GUIDE.md",
                     "DEVELOPER_GUIDE.md",
                     "MAINTAINER_GUIDE.md",
                     "DOCUMENTATION_MAINTENANCE.md",
                     "architecture/DATA_FLOW.md",
                     "architecture/SESSION_AND_CONCURRENCY.md",
                     "security/TOTP.md",
                     "security/SESSION_SECURITY.md",
                     "security/DATA_LIFECYCLE.md",
                     "formats/VAULT_RECORDS.md",
                     "formats/ATTACHMENTS.md",
                     "formats/ENCRYPTED_BACKUP.md",
                     "formats/CSV_TRANSFER.md",
                     "TESTING_GUIDE.md",
                     "ACCESSIBILITY.md",
                     "verification/DOCUMENTATION_SUITE_2026_08_12.md",
                     "verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md",
                     "verification/HOSTED_CI_EVIDENCE_2026_08_13.md",
                     "verification/CURRENT_HEAD_2026_08_13.md",
                     "verification/POST_BASELINE_CHECKLIST_2026_08_13.md",
                     "verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md",
                     "verification/TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md",
                     "operations/BACKUP_RECOVERY_RUNBOOK.md",
                     "operations/SECURITY_RESPONSE.md",
                     "releases/RELEASE_PROCESS.md"
                 })
        {
            Assert.Contains(expected, hub, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SecurityEntryPoints_KeepIndependentAuditDisclaimer()
    {
        var readme = File.ReadAllText(PathAt("README.md"));
        var security = File.ReadAllText(PathAt("SECURITY.md"));
        var cryptoDesign = File.ReadAllText(PathAt("docs", "security", "CRYPTOGRAPHIC_DESIGN.md"));
        var totp = File.ReadAllText(PathAt("docs", "security", "TOTP.md"));

        Assert.Contains("has not yet undergone an independent professional security audit", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("has **not** completed an independent professional security audit", security, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not** completed an independent professional", cryptoDesign, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not** completed an independent professional", totp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConsolidatedDocumentation_KeepsSecurityAndReleaseDisclaimers()
    {
        var complete = File.ReadAllText(PathAt("docs", "COMPLETE_PROJECT_DOCUMENTATION.md"));
        var faq = File.ReadAllText(PathAt("docs", "FAQ.md"));
        var verification = File.ReadAllText(PathAt("docs", "verification", "DOCUMENTATION_CONSOLIDATION_2026_08_14.md"));
        var totpVerification = File.ReadAllText(PathAt("docs", "verification", "TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md"));

        Assert.Contains("not** completed an independent professional security audit", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be deterministically erased", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("historical evidence", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not** completed an independent professional security audit", faq, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot deterministically erase", faq, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("final direct-commit head", verification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("independent professional security audit", verification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exact current-head candidate", totpVerification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not provide cryptographic factor separation", totpVerification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not claim every remaining UI literal is translated", totpVerification, StringComparison.OrdinalIgnoreCase);
    }

    private static string PathAt(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx"))) directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");
        var path = directory.FullName;
        foreach (var segment in segments) path = Path.Combine(path, segment);
        return path;
    }
}
