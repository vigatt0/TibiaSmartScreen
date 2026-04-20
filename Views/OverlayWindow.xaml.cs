using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WPoint = System.Windows.Point;
using WRect  = System.Windows.Rect;
using TibiaSmartScreen.Infrastructure;
using TibiaSmartScreen.ViewModels;

namespace TibiaSmartScreen.Views;

/// <summary>
/// Borderless, transparent overlay window that mirrors a screen region.
/// Supports drag, resize (8 handles), lock/click-through, and opacity control.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _vm;

    // ── Drag state ───────────────────────────────────────────────
    private bool _isDragging;
    private WPoint _dragOffset;

    // ── Resize state ─────────────────────────────────────────────
    private bool _isResizing;
    private string? _resizeDir;
    private WPoint _resizeStart;
    private WRect _resizeStartBounds;

    public OverlayWindow(OverlayViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        DataContext = vm;

        // Apply saved position/size
        Left   = vm.Region.OverlayLeft;
        Top    = vm.Region.OverlayTop;
        Width  = vm.Region.OverlayWidth;
        Height = vm.Region.OverlayHeight;
        Opacity = vm.Opacity;

        TitleText.Text = vm.Region.Name;

        // Wire frame updates
        vm.PropertyChanged += Vm_PropertyChanged;
        vm.LockChanged      += (_, locked) => ApplyLockState(locked);
        vm.CloseRequested   += (_, _) => Close();

        // Apply initial lock state after handle is created
        SourceInitialized += (_, _) => ApplyLockState(vm.IsLocked);
    }

    // ── Window handle helpers ─────────────────────────────────────

    private IntPtr Hwnd => new WindowInteropHelper(this).Handle;

    private void ApplyLockState(bool locked)
    {
        if (Hwnd == IntPtr.Zero) return;
        if (locked)
            Win32Api.EnableClickThrough(Hwnd);
        else
            Win32Api.DisableClickThrough(Hwnd);

        LockBtn.Content = locked ? "🔒" : "🔓";
        LockBtn.ToolTip = locked ? "Locked — click to unlock" : "Unlocked — click to lock";
        TitleBar.Cursor = locked ? Cursors.Arrow : Cursors.SizeAll;
    }

    // ── Property change handler ───────────────────────────────────

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OverlayViewModel.CapturedImage):
                CaptureImage.Source = _vm.CapturedImage;
                break;
            case nameof(OverlayViewModel.Fps):
                FpsLabel.Text = _vm.Fps > 0 ? $"{_vm.Fps} fps" : string.Empty;
                break;
            case nameof(OverlayViewModel.Opacity):
                Opacity = _vm.Opacity;
                break;
        }
    }

    // ── Title-bar drag ────────────────────────────────────────────

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.IsLocked || e.ChangedButton != MouseButton.Left) return;
        _isDragging = true;
        _dragOffset = e.GetPosition(this);
        Mouse.Capture(TitleBar);
        TitleBar.MouseMove  += TitleBar_MouseMove;
        TitleBar.MouseUp    += TitleBar_MouseUp;
    }

    private void TitleBar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;
        var screen = PointToScreen(e.GetPosition(this));
        Left = screen.X - _dragOffset.X;
        Top  = screen.Y - _dragOffset.Y;
        SaveBounds();
    }

    private void TitleBar_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        Mouse.Capture(null);
        TitleBar.MouseMove -= TitleBar_MouseMove;
        TitleBar.MouseUp   -= TitleBar_MouseUp;
        SaveBounds();
    }

    // ── Resize ────────────────────────────────────────────────────

    private void Resize_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.IsLocked || e.ChangedButton != MouseButton.Left) return;
        if (sender is not System.Windows.Shapes.Rectangle rect) return;

        _isResizing      = true;
        _resizeDir       = rect.Tag as string;
        _resizeStart     = PointToScreen(e.GetPosition(this));
        _resizeStartBounds = new WRect(Left, Top, Width, Height);

        Mouse.Capture(rect);
        rect.MouseMove += Resize_MouseMove;
        rect.MouseUp   += Resize_MouseUp;
        e.Handled = true;
    }

    private void Resize_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isResizing) return;

        var cur = PointToScreen(e.GetPosition(this));
        double dx = cur.X - _resizeStart.X;
        double dy = cur.Y - _resizeStart.Y;

        double l = _resizeStartBounds.Left;
        double t = _resizeStartBounds.Top;
        double w = _resizeStartBounds.Width;
        double h = _resizeStartBounds.Height;

        switch (_resizeDir)
        {
            case "E":  w = Math.Max(MinWidth,  w + dx); break;
            case "W":  l += dx; w = Math.Max(MinWidth,  w - dx); break;
            case "S":  h = Math.Max(MinHeight, h + dy); break;
            case "N":  t += dy; h = Math.Max(MinHeight, h - dy); break;
            case "SE": w = Math.Max(MinWidth,  w + dx); h = Math.Max(MinHeight, h + dy); break;
            case "SW": l += dx; w = Math.Max(MinWidth,  w - dx); h = Math.Max(MinHeight, h + dy); break;
            case "NE": t += dy; h = Math.Max(MinHeight, h - dy); w = Math.Max(MinWidth, w + dx); break;
            case "NW": l += dx; w = Math.Max(MinWidth,  w - dx); t += dy; h = Math.Max(MinHeight, h - dy); break;
        }

        Left = l; Top = t; Width = w; Height = h;
    }

    private void Resize_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isResizing = false;
        Mouse.Capture(null);
        if (sender is System.Windows.Shapes.Rectangle rect)
        {
            rect.MouseMove -= Resize_MouseMove;
            rect.MouseUp   -= Resize_MouseUp;
        }
        SaveBounds();
    }

    // ── Button handlers ───────────────────────────────────────────

    private void LockBtn_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsLocked = !_vm.IsLocked;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        _vm.RequestClose();
    }

    // ── Persistence ───────────────────────────────────────────────

    private void SaveBounds()
    {
        _vm.Region.OverlayLeft   = Left;
        _vm.Region.OverlayTop    = Top;
        _vm.Region.OverlayWidth  = Width;
        _vm.Region.OverlayHeight = Height;
    }

    protected override void OnClosed(EventArgs e)
    {
        SaveBounds();
        _vm.Dispose();
        base.OnClosed(e);
    }
}
