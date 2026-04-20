using System;
using System.Collections.Generic;

namespace TibiaSmartScreen.Models;

/// <summary>
/// Represents a saved layout containing multiple screen regions.
/// </summary>
public class AppLayout
{
    public string Name { get; set; } = "Default";
    public List<ScreenRegion> Regions { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
