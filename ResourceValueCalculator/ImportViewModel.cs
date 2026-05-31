using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ResourceValueCalculator;

/// <summary>
/// The "Update Data" tab: pulls the latest crafting blueprints from scunpacked-data on GitHub,
/// saves them locally, and refreshes the Components tab.
/// </summary>
public class ImportViewModel : INotifyPropertyChanged
{
    private readonly Action _onImported;

    public ImportViewModel(Action onImported)
    {
        _onImported = onImported;
        ImportCommand = new RelayCommand(async _ => await ImportAsync(), _ => !IsBusy);
        _statusMessage = BlueprintService.HasUserData
            ? "Using imported blueprints. Click Import to refresh from scunpacked-data."
            : "No data imported yet — click Import to load blueprints from scunpacked-data and unlock the Calculator and Components tabs.";
    }

    public string SourceUrl => ScunpackedImporter.SourceUrl;

    public ICommand ImportCommand { get; }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _statusMessage;
    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    private async Task ImportAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var progress = new Progress<string>(s => StatusMessage = s);
            var blueprints = await Task.Run(() => ScunpackedImporter.Import(progress));
            BlueprintService.Save(blueprints);
            _onImported();
            StatusMessage = $"Imported {blueprints.Count} blueprints from scunpacked-data — " +
                            "saved locally and applied to the Components tab (used on next launch too).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
