namespace CipherNest.UiTests;

public sealed class DocumentationCoverageSourceTests
{
    private static readonly string[][] RequiredDocumentation =
    [
        ["docs", "README.md"],
        ["docs", "QUICK_START.md"],
        ["docs", "FEATURE_MATRIX.md"],
        ["docs", "UI_REFERENCE.md"],
        ["docs", "CONFIGURATION_REFERENCE.md"],
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
        ["docs", "formats", "VAULT_HEADER.md"],
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
        ["docs", "verification", "CSV_IMPORT_HARDENING_2026_08_15.md"],
        ["docs", "verification", "SETTINGS_JSON_HARDENING_2026_08_15.md"],
        ["docs", "verification", "BACKUP_HEADER_HARDENING_2026_08_15.md"],
        ["docs", "verification", "VAULT_HEADER_HARDENING_2026_08_15.md"],
        ["docs", "verification", "ATTACHMENT_METADATA_HARDENING_2026_08_15.md"],
        ["docs", "verification", "FINAL_REPOSITORY_HARDENING_2026_08_15.md"],
        ["docs", "verification", "VERIFIED_MAIN_BASELINE_2026_08_15.md"],
        ["docs", "verification", "REPOSITORY_AUDIT_2026_08_16.md"],
        ["docs", "verification", "COMPLETE_DOCUMENTATION_2026_08_16.md"],
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

        foreach (var expected in new[]
                 {
                     "docs/README.md",
                     "docs/QUICK_START.md",
                     "docs/FEATURE_MATRIX.md",
                     "docs/UI_REFERENCE.md",
                     "docs/CONFIGURATION_REFERENCE.md",
                     "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
                     "docs/FAQ.md",
                     "docs/USER_GUIDE.md",
                     "docs/DEVELOPER_GUIDE.md",
                     "docs/security/THREAT_MODEL.md",
                     "docs/security/CRYPTOGRAPHIC_DESIGN.md",
                     "docs/security/TOTP.md",
                     "docs/formats/ENCRYPTED_BACKUP.md",
                     "docs/releases/RELEASE_PROCESS.md",
                     "docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md"
                 })
        {
            Assert.Contains(expected, readme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DocumentationHub_LinksAllMajorDocumentationAreas()
    {
        var hub = File.ReadAllText(PathAt("docs", "README.md"));

        foreach (var expected in new[]
                 {
                     "QUICK_START.md",
                     "FEATURE_MATRIX.md",
                     "UI_REFERENCE.md",
                     "CONFIGURATION_REFERENCE.md",
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
                     "formats/VAULT_HEADER.md",
                     "formats/ATTACHMENTS.md",
                     "formats/ENCRYPTED_BACKUP.md",
                     "formats/CSV_TRANSFER.md",
                     "TESTING_GUIDE.md",
                     "ACCESSIBILITY.md",
                     "verification/DOCUMENTATION_SUITE_2026_08_12.md",
                     "verification/VERIFIED_MAIN_BASELINE_2026_08_15.md",
                     "verification/REPOSITORY_AUDIT_2026_08_16.md",
                     "verification/COMPLETE_DOCUMENTATION_2026_08_16.md",
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
        var complete = File.ReadAllText(PathAt("docs", "COMPLETE_PROJECT_DOCUMENTATION.md"));

        Assert.Contains("independent professional security audit", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("has **not** completed an independent professional security audit", security, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("independent professional", cryptoDesign, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("independent professional", totp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("has **not** completed an independent professional security audit", complete, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConsolidatedDocumentation_KeepsSecurityAndReleaseDisclaimers()
    {
        var complete = File.ReadAllText(PathAt("docs", "COMPLETE_PROJECT_DOCUMENTATION.md"));
        var faq = File.ReadAllText(PathAt("docs", "FAQ.md"));
        var completeVerification = File.ReadAllText(PathAt("docs", "verification", "COMPLETE_DOCUMENTATION_2026_08_16.md"));
        var finalVerification = File.ReadAllText(PathAt("docs", "verification", "FINAL_REPOSITORY_HARDENING_2026_08_15.md"));
        var verifiedBaseline = File.ReadAllText(PathAt("docs", "verification", "VERIFIED_MAIN_BASELINE_2026_08_15.md"));
        var repositoryAudit = File.ReadAllText(PathAt("docs", "verification", "REPOSITORY_AUDIT_2026_08_16.md"));

        Assert.Contains("cannot be deterministically erased", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("555 passed", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("System/English/Hindi", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Buy Me a Coffee", complete, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not** completed an independent professional security audit", faq, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("555 total passed", faq, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reviewed `hi-IN` resource-backed catalog", faq, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("555 passed", completeVerification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("final documentation head", completeVerification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("554 passed", finalVerification, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("554 passed", verifiedBaseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("later commit becomes a new candidate", verifiedBaseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BMC/support coverage after this pass", repositoryAudit, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remaining validation work", repositoryAudit, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("intentionally deferred or unclaimed features", repositoryAudit, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompleteDocumentationSuite_CoversCurrentProductSurfaces()
    {
        var quickStart = File.ReadAllText(PathAt("docs", "QUICK_START.md"));
        var features = File.ReadAllText(PathAt("docs", "FEATURE_MATRIX.md"));
        var ui = File.ReadAllText(PathAt("docs", "UI_REFERENCE.md"));
        var configuration = File.ReadAllText(PathAt("docs", "CONFIGURATION_REFERENCE.md"));
        var complete = File.ReadAllText(PathAt("docs", "COMPLETE_PROJECT_DOCUMENTATION.md"));

        foreach (var expected in new[] { "encrypted backup", "TOTP", "Buy Me a Coffee", "555 passed" })
            Assert.Contains(expected, quickStart, StringComparison.OrdinalIgnoreCase);

        foreach (var expected in new[]
                 {
                     "Time-Based One-Time Password",
                     "Windows Hello",
                     "CipherNestEnableFundingLink=false",
                     "Reviewed Hindi",
                     "555 passed"
                 })
        {
            Assert.Contains(expected, features, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Startup Page",
                     "Onboarding Page",
                     "Unlock Page",
                     "Vault Page",
                     "Item Editor Page",
                     "Settings Page",
                     "Transfer Page",
                     "About Page",
                     "Developer Page",
                     "☕ Support"
                 })
        {
            Assert.Contains(expected, ui, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "in.sanskar.ciphernest",
                     "CipherNestTargetFrameworks",
                     "CipherNestEnableFundingLink",
                     "System / English / Hindi",
                     "10.0.10",
                     "555 passed"
                 })
        {
            Assert.Contains(expected, configuration, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "# 14. Cryptographic design",
                     "# 18. SQLite schema, migrations, and replacement",
                     "# 25. Encrypted attachments",
                     "# 26. Encrypted backup and restore",
                     "# 31. Accessibility",
                     "# 32. Localization",
                     "# 34. Branding and Buy Me a Coffee support",
                     "# 38. Hosted CI and CodeQL baseline",
                     "# 48. Known limitations and external validation gates",
                     "# 51. Canonical documentation map"
                 })
        {
            Assert.Contains(expected, complete, StringComparison.OrdinalIgnoreCase);
        }
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
