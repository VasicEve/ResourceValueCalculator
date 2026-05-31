using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ResourceValueCalculator;

/// <summary>
/// Top-level view model hosting the tabs and loading shared data once.
/// The Calculator and Components tabs are shown only after blueprint data has been imported.
/// </summary>
public class ShellViewModel : INotifyPropertyChanged
{
    // Tab order in MainWindow.xaml: 0 = Calculator, 1 = Components, 2 = Update Data.
    private const int CalculatorTabIndex = 0;
    private const int UpdateDataTabIndex = 2;

    public CalculatorViewModel Calculator { get; } = new();
    public ComponentViewModel Components { get; } = new();
    public ImportViewModel Import { get; }

    public ShellViewModel()
    {
        _hasImportedData = BlueprintService.HasUserData;
        _selectedTabIndex = _hasImportedData ? CalculatorTabIndex : UpdateDataTabIndex;
        Import = new ImportViewModel(OnImported);
    }

    private bool _hasImportedData;
    /// <summary>True once blueprints have been imported; the data-dependent tabs are visible only then.</summary>
    public bool HasImportedData
    {
        get => _hasImportedData;
        private set { if (_hasImportedData != value) { _hasImportedData = value; OnPropertyChanged(); } }
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { if (_selectedTabIndex != value) { _selectedTabIndex = value; OnPropertyChanged(); } }
    }

    /// <summary>Loads the local blueprint catalog and the live UEX commodity prices, sharing the latter with both tabs.</summary>
    public async Task LoadAsync()
    {
        Components.LoadBlueprints();

        try
        {
            var catalog = await CommodityService.FetchAsync();
            Calculator.SetCatalog(catalog);
            Components.SetCatalog(catalog);
        }
        catch (Exception ex)
        {
            Calculator.SetLoadError(ex.Message);
            Components.SetLoadError(ex.Message);
        }
    }

    // Called after a successful import: refresh the catalog, reveal the tabs, and jump to the calculator.
    private void OnImported()
    {
        Components.LoadBlueprints();
        HasImportedData = true;
        SelectedTabIndex = CalculatorTabIndex;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
