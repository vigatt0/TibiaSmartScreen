using System;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TibiaSmartScreen.Models;
using TibiaSmartScreen.Services;

namespace TibiaSmartScreen.ViewModels;

/// <summary>
/// ViewModel for a single overlay window.
/// Owns the capture timer and the ScreenCaptureService for its region.
/// </summary>
public class OverlayViewModel : ViewModelBase, IDisposable
{
    private readonly ScreenCaptureService _captureService = new();
    private readonly DispatcherTimer _timer;
    private BitmapSource? _capturedImage;
    private bool _isLocked;
    private double _opacity;
    private bool _disposed;
    private int _frameCount;
    private double _fps;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;

    public ScreenRegion Region { get; }

    public BitmapSource? CapturedImage
    {
        get => _capturedImage;
        private set => SetField(ref _capturedImage, value);
    }

    /// <summary>
    /// When true the overlay is in click-through mode (locked).
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetField(ref _isLocked, value))
            {
                Region.IsLocked = value;
                LockChanged?.Invoke(this, value);
                OnPropertyChanged(nameof(LockIcon));
                OnPropertyChanged(nameof(LockTooltip));
            }
        }
    }

    public string LockIcon => IsLocked ? "🔒" : "🔓";
    public string LockTooltip => IsLocked ? "Locked (click-through). Click to unlock." : "Unlocked. Click to lock.";

    public double Opacity
    {
        get => _opacity;
        set
        {
            if (SetField(ref _opacity, Math.Clamp(value, 0.05, 1.0)))
                Region.Opacity = value;
        }
    }

    /// <summary>Current measured FPS for this overlay.</summary>
    public double Fps
    {
        get => _fps;
        private set => SetField(ref _fps, value);
    }

    public event EventHandler<bool>? LockChanged;
    public event EventHandler? CloseRequested;

    public OverlayViewModel(ScreenRegion region, int targetFps = 30)
    {
        Region = region ?? throw new ArgumentNullException(nameof(region));
        _isLocked = region.IsLocked;
        _opacity = region.Opacity;

        // Clamp interval: 16 ms = ~60 fps max, 1000 ms = 1 fps min
        int intervalMs = Math.Clamp(1000 / Math.Max(1, targetFps), 16, 1000);
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(intervalMs)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_disposed) return;

        var frame = _captureService.Capture(Region);
        if (frame != null)
            CapturedImage = frame;

        // Track FPS
        _frameCount++;
        var now = DateTime.UtcNow;
        double elapsed = (now - _lastFpsUpdate).TotalSeconds;
        if (elapsed >= 1.0)
        {
            Fps = Math.Round(_frameCount / elapsed, 1);
            _frameCount = 0;
            _lastFpsUpdate = now;
        }
    }

    public void SetTargetFps(int fps)
    {
        int intervalMs = Math.Clamp(1000 / Math.Max(1, fps), 16, 1000);
        _timer.Interval = TimeSpan.FromMilliseconds(intervalMs);
    }

    public void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_disposed) return;
        _timer.Stop();
        _captureService.Dispose();
        _disposed = true;
    }
}
