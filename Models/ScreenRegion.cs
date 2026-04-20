using System;
using System.Text.Json.Serialization;

namespace TibiaSmartScreen.Models;

/// <summary>
/// Represents a captured screen region with its overlay window configuration.
/// </summary>
public class ScreenRegion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Region";

    // Source capture bounds in physical screen pixels
    public int CaptureX { get; set; }
    public int CaptureY { get; set; }
    public int CaptureWidth { get; set; }
    public int CaptureHeight { get; set; }

    // Overlay window position in WPF logical units
    public double OverlayLeft { get; set; }
    public double OverlayTop { get; set; }
    public double OverlayWidth { get; set; } = 400;
    public double OverlayHeight { get; set; } = 300;

    public double Opacity { get; set; } = 0.95;
    public bool IsLocked { get; set; } = false;
    public bool IsVisible { get; set; } = true;

    [JsonIgnore]
    public bool IsSelected { get; set; } = false;
}
