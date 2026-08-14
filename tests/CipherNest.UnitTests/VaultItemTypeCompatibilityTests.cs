using System.Text.Json;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class VaultItemTypeCompatibilityTests
{
    [Fact]
    public void PersistedNumericValues_RemainBackwardCompatible()
    {
        Assert.Equal(0, (int)VaultItemType.Login);
        Assert.Equal(1, (int)VaultItemType.SecureNote);
        Assert.Equal(2, (int)VaultItemType.Identity);
        Assert.Equal(3, (int)VaultItemType.PaymentCardReference);
        Assert.Equal(4, (int)VaultItemType.WifiCredential);
        Assert.Equal(5, (int)VaultItemType.SoftwareLicense);
        Assert.Equal(6, (int)VaultItemType.ServerSshReference);
        Assert.Equal(7, (int)VaultItemType.Document);
        Assert.Equal(8, (int)VaultItemType.Custom);
        Assert.Equal(9, (int)VaultItemType.OneTimePassword);

        Assert.Equal(0, (int)TotpAlgorithm.Sha1);
        Assert.Equal(1, (int)TotpAlgorithm.Sha256);
        Assert.Equal(2, (int)TotpAlgorithm.Sha512);
    }

    [Fact]
    public void LegacyNumericCustomPayload_StillDeserializesAsCustom()
    {
        var id = Guid.NewGuid();
        var json = $$"""
        {
          "id": "{{id}}",
          "type": 8,
          "title": "Legacy custom",
          "username": "",
          "secret": "",
          "url": "",
          "notes": "",
          "collection": "",
          "tags": [],
          "isFavorite": false,
          "customFields": [],
          "attachments": [],
          "createdUtc": "2026-08-01T00:00:00+00:00",
          "modifiedUtc": "2026-08-01T00:00:00+00:00",
          "requiresReauthentication": false
        }
        """;

        var item = JsonSerializer.Deserialize<VaultItem>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(item);
        Assert.Equal(VaultItemType.Custom, item.Type);
        Assert.Equal(TotpAlgorithm.Sha1, item.TotpAlgorithm);
        Assert.Equal(6, item.TotpDigits);
        Assert.Equal(30, item.TotpPeriodSeconds);
    }
}
