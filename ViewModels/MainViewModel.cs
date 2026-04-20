using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TibiaSmartScreen.Models;
using TibiaSmartScreen.Services;

namespace TibiaSmartScreen.ViewModels;

/// <summary>
/// Main application ViewModel. Manages the list of regions, global state, and layout persistence.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly LayoutService _layoutService = new();
    private AppSettings _settings;
    private readonly AppLayout _currentLayout;
    private ScreenRegion? _selectedRegion;
    private bool _allLocked;
    private string _statusText = "Ready — add a region to get started";
    private double _globalOpacity = 0.95;
    private int _targetFps = 30;

    public ObservableCollection<ScreenRegion> Regions { get; } = new();

    public ScreenRegion? SelectedRegion
    {
        get => _selectedRegion;
        set => SetField(ref _selectedRegion, value);
    }

    public bool AllLocked
    {
        get => _allLocked;
        set
        {
            if (SetField(ref _allLocked, value))
            {
                foreach (var r in Regions)
                    r.IsLocked = value;
                OnPropertyChanged(nameof(LockButtonText));
                AllLockedChanged?.Invoke(this, value);
            }
        }
    }

    public string LockButtonText => AllLocked ? "🔒 Unlock All" : "🔓 Lock All";

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public double GlobalOpacity
    {
        get => _globalOpacity;
        set
        {
            if (SetField(ref _globalOpacity, Math.Clamp(value, 0.05, 1.0)))
                GlobalOpacityChanged?.Invoke(this, value);
        }
    }

    public int TargetFps
    {
        get => _targetFps;
        set
        {
            if (SetField(ref _targetFps, Math.Clamp(value, 1, 60)))
            {
                _settings.TargetFps = _targetFps;
                FpsChanged?.Invoke(this, _targetFps);
            }
        }
    }

    // Commands
    public ICommand AddRegionCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand ToggleLockCommand { get; }
    public ICommand SaveLayoutCommand { get; }

    // Events consumed by the overlay manager / code-behind
    public event EventHandler<ScreenRegion>? RegionAdded;
    public event EventHandler<ScreenRegion>? RegionRemoved;
    public event EventHandler<bool>? AllLockedChanged;
    public event EventHandler<double>? GlobalOpacityChanged;
    public event EventHandler<int>? FpsChanged;
    public event Action? RequestAddRegion;

    public MainViewModel()
    {
        _settings = _layoutService.LoadSettings();
        _targetFps = _settings.TargetFps;
        _currentLayout = _layoutService.LoadOrCreate(_settings.LastLayoutName);

        // Restore saved regions
        foreach (var r in _currentLayout.Regions)
            Regions.Add(r);

        AddRegionCommand = new RelayCommand(_ => RequestAddRegion?.Invoke());
        DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, _ => SelectedRegion != null);
        ToggleLockCommand = new RelayCommand(_ => AllLocked = !AllLocked);
        SaveLayoutCommand = new RelayCommand(_ => PersistLayout());
    }

    /// <summary>Registers a newly selected region and opens its overlay.</summary>
    public void AddRegion(ScreenRegion region)
    {
        Regions.Add(region);
        _currentLayout.Regions.Add(region);
        SelectedRegion = region;
        StatusText = $"Region '{region.Name}' added";
        RegionAdded?.Invoke(this, region);
        PersistLayout();
    }

    private void ExecuteDeleteSelected(object? _)
    {
        if (SelectedRegion == null) return;
        var region = SelectedRegion;
        Regions.Remove(region);
        _currentLayout.Regions.Remove(region);
        SelectedRegion = Regions.FirstOrDefault();
        StatusText = $"Region '{region.Name}' removed";
        RegionRemoved?.Invoke(this, region);
        PersistLayout();
    }

    public void PersistLayout()
    {
        _layoutService.SaveLayout(_currentLayout);
        _settings.LastLayoutName = _currentLayout.Name;
        _layoutService.SaveSettings(_settings);
    }
}
