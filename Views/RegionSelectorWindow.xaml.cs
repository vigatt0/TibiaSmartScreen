using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPoint = System.Windows.Point;
using TibiaSmartScreen.Models;

namespace TibiaSmartScreen.Views;

/// <summary>
/// Fullscreen transparent window for selecting a screen region by mouse drag.
/// Result is available via <see cref="SelectedRegion"/> after the window closes.
/// </summary>
public partial class RegionSelectorWindow : Window
{
    private WPoint _startPoint;
    private bool _isDragging;

    public ScreenRegion? SelectedRegion { get; private set; }

    public RegionSelectorWindow()
    {
        InitializeComponent();

        // Initially hide the selection visuals
        SetSelectionVisible(false);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { SelectedRegion = null; Close(); }
        };

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    private void SetSelectionVisible(bool visible)
    {
        var vis = visible ? Visibility.Visible : Visibility.Collapsed;
        SelectionRect.Visibility = vis;
        OverlayTop.Visibility    = vis;
        OverlayBottom.Visibility = vis;
        OverlayLeft.Visibility   = vis;
        OverlayRight.Visibility  = vis;
        DimLabel.Visibility      = vis;
        DarkOverlay.Visibility   = visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);
        _isDragging = true;
        SetSelectionVisible(true);
        Mouse.Capture(this);
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;
        UpdateVisuals(e.GetPosition(SelectionCanvas));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        Mouse.Capture(null);

        var end = e.GetPosition(SelectionCanvas);
        double x = Math.Min(_startPoint.X, end.X);
        double y = Math.Min(_startPoint.Y, end.Y);
        double w = Math.Abs(end.X - _startPoint.X);
        double h = Math.Abs(end.Y - _startPoint.Y);

        if (w < 10 || h < 10)
        {
            SelectedRegion = null;
            Close();
            return;
        }

        // Convert WPF logical coordinates → physical screen pixels (DPI-aware)
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        double scaleX = transform?.M11 ?? 1.0;
        double scaleY = transform?.M22 ?? 1.0;

        SelectedRegion = new ScreenRegion
        {
            CaptureX      = (int)(x * scaleX),
            CaptureY      = (int)(y * scaleY),
            CaptureWidth  = (int)(w * scaleX),
            CaptureHeight = (int)(h * scaleY),
            OverlayLeft   = x,
            OverlayTop    = y + 40,
            OverlayWidth  = Math.Max(w, 200),
            OverlayHeight = Math.Max(h, 150),
            Name          = $"Region {DateTime.Now:HH:mm:ss}"
        };

        Close();
    }

    private void UpdateVisuals(WPoint current)
    {
        double x = Math.Min(_startPoint.X, current.X);
        double y = Math.Min(_startPoint.Y, current.Y);
        double w = Math.Abs(current.X - _startPoint.X);
        double h = Math.Abs(current.Y - _startPoint.Y);
        double cw = SelectionCanvas.ActualWidth;
        double ch = SelectionCanvas.ActualHeight;

        // Selection rectangle
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect,  y);
        SelectionRect.Width  = Math.Max(w, 1);
        SelectionRect.Height = Math.Max(h, 1);

        // Top dark mask
        Canvas.SetLeft(OverlayTop, 0); Canvas.SetTop(OverlayTop, 0);
        OverlayTop.Width = cw; OverlayTop.Height = y;

        // Bottom dark mask
        Canvas.SetLeft(OverlayBottom, 0); Canvas.SetTop(OverlayBottom, y + h);
        OverlayBottom.Width = cw; OverlayBottom.Height = Math.Max(ch - y - h, 0);

        // Left dark mask
        Canvas.SetLeft(OverlayLeft, 0); Canvas.SetTop(OverlayLeft, y);
        OverlayLeft.Width = x; OverlayLeft.Height = h;

        // Right dark mask
        Canvas.SetLeft(OverlayRight, x + w); Canvas.SetTop(OverlayRight, y);
        OverlayRight.Width = Math.Max(cw - x - w, 0); OverlayRight.Height = h;

        // Dimension label — position near selection, avoid edge overflow
        DimText.Text = $"{(int)w} × {(int)h} px";
        double lx = x + w + 8;
        double ly = y + h + 8;
        if (lx + 90 > cw) lx = x - 98;
        if (ly + 28 > ch) ly = y - 36;
        Canvas.SetLeft(DimLabel, Math.Max(0, lx));
        Canvas.SetTop(DimLabel,  Math.Max(0, ly));
    }
}
