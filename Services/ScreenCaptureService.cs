using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using TibiaSmartScreen.Models;

namespace TibiaSmartScreen.Services;

/// <summary>
/// High-performance screen capture service using GDI+ CopyFromScreen.
/// Reuses internal Bitmap allocations to reduce GC pressure.
/// Target: 30–60 FPS with minimal CPU overhead.
/// </summary>
public sealed class ScreenCaptureService : IDisposable
{
    private Bitmap? _reusableBitmap;
    private int _lastWidth;
    private int _lastHeight;
    private bool _disposed;

    /// <summary>
    /// Captures the specified screen region and returns a frozen WPF BitmapSource.
    /// Returns null on error or if the region has zero size.
    /// </summary>
    public BitmapSource? Capture(ScreenRegion region)
    {
        if (_disposed) return null;
        if (region.CaptureWidth <= 0 || region.CaptureHeight <= 0) return null;

        try
        {
            // Reuse the bitmap when the capture size hasn't changed
            if (_reusableBitmap == null
                || _lastWidth != region.CaptureWidth
                || _lastHeight != region.CaptureHeight)
            {
                _reusableBitmap?.Dispose();
                _reusableBitmap = new Bitmap(
                    region.CaptureWidth,
                    region.CaptureHeight,
                    PixelFormat.Format32bppArgb);
                _lastWidth = region.CaptureWidth;
                _lastHeight = region.CaptureHeight;
            }

            using var g = Graphics.FromImage(_reusableBitmap);
            g.CopyFromScreen(
                region.CaptureX,
                region.CaptureY,
                0, 0,
                new Size(region.CaptureWidth, region.CaptureHeight),
                CopyPixelOperation.SourceCopy);

            return ConvertToWpfBitmap(_reusableBitmap);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Capture] {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a GDI+ Bitmap to a frozen WPF BitmapSource without extra heap copies.
    /// </summary>
    private static BitmapSource ConvertToWpfBitmap(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var bs = BitmapSource.Create(
                data.Width, data.Height,
                96, 96,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                data.Scan0,
                data.Stride * data.Height,
                data.Stride);
            bs.Freeze(); // Allows cross-thread access and prevents further modifications
            return bs;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reusableBitmap?.Dispose();
            _disposed = true;
        }
    }
}
