using CipherNest.App.Services;

namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel
{
    partial void OnLargerInterfaceChanged(bool value) => AccessibilityPreferenceApplicator.Apply(value, ReducedMotion);
    partial void OnReducedMotionChanged(bool value) => AccessibilityPreferenceApplicator.Apply(LargerInterface, value);
}
