namespace TibiaSmartScreen.Models;

/// <summary>
/// Persistent application settings.
/// </summary>
public class AppSettings
{
    public string LastLayoutName { get; set; } = "Default";
    public int TargetFps { get; set; } = 30;
    public bool ShowPerformanceMetrics { get; set; } = false;
}
