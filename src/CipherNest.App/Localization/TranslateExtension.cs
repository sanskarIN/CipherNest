using CipherNest.App.Services;
using Microsoft.Maui.Controls.Xaml;

namespace CipherNest.App.Localization;

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new XamlParseException("A localization resource key is required.");
        }

        return ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(Key);
    }
}
