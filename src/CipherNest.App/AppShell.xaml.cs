using CipherNest.App.Views;

namespace CipherNest.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ItemEditorPage), typeof(ItemEditorPage));
    }
}
