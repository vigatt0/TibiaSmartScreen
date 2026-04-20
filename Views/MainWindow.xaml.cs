using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TibiaSmartScreen.Infrastructure;
using TibiaSmartScreen.Models;
using TibiaSmartScreen.ViewModels;

namespace TibiaSmartScreen.Views;

/// <summary>
/// Main control-panel window.
/// Owns the overlay manager (dictionary of overlays keyed by region ID)
/// and handles global hotkeys.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    // Maps region ID → its overlay window
    private readonly Dictionary<string, OverlayWindow> _overlays = new();

    // Hotkey IDs
    private const int HK_ADD    = 1;
    private const int HK_LOCK   = 2;
    private const int HK_HIDE   = 3;

    private bool _allHidden;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Wire events from ViewModel
        _vm.RequestAddRegion    += StartRegionSelection;
        _vm.RegionAdded         += OnRegionAdded;
        _vm.RegionRemoved       += OnRegionRemoved;
        _vm.AllLockedChanged    += OnAllLockedChanged;
        _vm.GlobalOpacityChanged += OnGlobalOpacityChanged;
        _vm.FpsChanged          += OnFpsChanged;

        SourceInitialized += OnSourceInitialized;
        Closed            += OnMainClosed;

        // Restore any persisted regions
        foreach (var region in _vm.Regions)
            OpenOverlay(region);
    }

    // ── Source initialised — register hotkeys ────────────────────

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var src  = HwndSource.FromHwnd(hwnd);
        src?.AddHook(WndProc);

        Win32Api.RegisterHotKey(hwnd, HK_ADD,  Win32Api.MOD_NOREPEAT, 0x78); // F9
        Win32Api.RegisterHotKey(hwnd, HK_LOCK, Win32Api.MOD_NOREPEAT, 0x79); // F10
        Win32Api.RegisterHotKey(hwnd, HK_HIDE, Win32Api.MOD_NOREPEAT, 0x7A); // F11
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case HK_ADD:  StartRegionSelection(); handled = true; break;
                case HK_LOCK: _vm.ToggleLockCommand.Execute(null); handled = true; break;
                case HK_HIDE: ToggleAllVisibility(); handled = true; break;
            }
        }
        return IntPtr.Zero;
    }

    // ── Title bar drag ────────────────────────────────────────────

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e)    => WindowState = WindowState.Minimized;
    private void CloseBtn_Click(object sender, RoutedEventArgs e)  => Close();

    // ── Region selection ─────────────────────────────────────────

    private void StartRegionSelection()
    {
        var selector = new RegionSelectorWindow();
        selector.ShowDialog();
        if (selector.SelectedRegion is { } region)
            _vm.AddRegion(region);
    }

    // ── Overlay lifecycle ─────────────────────────────────────────

    private void OnRegionAdded(object? sender, ScreenRegion region)
        => OpenOverlay(region);

    private void OpenOverlay(ScreenRegion region)
    {
        if (_overlays.ContainsKey(region.Id)) return;

        var overlayVm = new OverlayViewModel(region, _vm.TargetFps)
        {
            Opacity = _vm.GlobalOpacity
        };

        var win = new OverlayWindow(overlayVm);
        _overlays[region.Id] = win;

        // When overlay is closed (✕ button or vm.RequestClose), remove it
        win.Closed += (_, _) =>
        {
            _overlays.Remove(region.Id);
            _vm.PersistLayout();
        };

        win.Show();
    }

    private void OnRegionRemoved(object? sender, ScreenRegion region)
    {
        if (_overlays.TryGetValue(region.Id, out var win))
        {
            win.Close();
            _overlays.Remove(region.Id);
        }
    }

    // ── Global state propagation ──────────────────────────────────

    private void OnAllLockedChanged(object? sender, bool locked)
    {
        foreach (var (id, win) in _overlays)
            if (win.DataContext is OverlayViewModel vm)
                vm.IsLocked = locked;
    }

    private void OnGlobalOpacityChanged(object? sender, double opacity)
    {
        foreach (var (_, win) in _overlays)
            if (win.DataContext is OverlayViewModel vm)
                vm.Opacity = opacity;
    }

    private void OnFpsChanged(object? sender, int fps)
    {
        foreach (var (_, win) in _overlays)
            if (win.DataContext is OverlayViewModel vm)
                vm.SetTargetFps(fps);
    }

    private void ToggleAllVisibility()
    {
        _allHidden = !_allHidden;
        foreach (var (_, win) in _overlays)
            win.Visibility = _allHidden ? Visibility.Hidden : Visibility.Visible;
        _vm.StatusText = _allHidden ? "All overlays hidden" : "All overlays visible";
    }

    // ── Clean-up ──────────────────────────────────────────────────

    private void OnMainClosed(object? sender, EventArgs e)
    {
        // Unregister hotkeys
        var hwnd = new WindowInteropHelper(this).Handle;
        Win32Api.UnregisterHotKey(hwnd, HK_ADD);
        Win32Api.UnregisterHotKey(hwnd, HK_LOCK);
        Win32Api.UnregisterHotKey(hwnd, HK_HIDE);

        // Close all overlays
        foreach (var (_, win) in new Dictionary<string, OverlayWindow>(_overlays))
            win.Close();

        _vm.PersistLayout();
    }
}
