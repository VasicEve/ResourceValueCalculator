using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ResourceValueCalculator;

/// <summary>The "Resource Value Calculator" tab: user-entered resource rows valued from the UEX catalog.</summary>
public class CalculatorViewModel : ResourceListViewModel
{
    /// <summary>Commodity catalog pulled live from UEX (https://uexcorp.space/commodities).</summary>
    public ObservableCollection<Commodity> Catalog { get; } = new();

    private string _statusMessage = "Loading commodities from UEX…";
    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }

    public CalculatorViewModel()
    {
        AddCommand = new RelayCommand(_ => Resources.Add(new ResourceRow { Margin = ProfitMargin }));
        RemoveCommand = new RelayCommand(row =>
        {
            if (row is ResourceRow r) Resources.Remove(r);
        });

        Resources.Add(new ResourceRow { Margin = ProfitMargin }); // start with one empty row
    }

    public void SetCatalog(IReadOnlyList<Commodity> commodities)
    {
        Catalog.Clear();
        foreach (var c in commodities)
            Catalog.Add(c);

        StatusMessage = commodities.Count > 0
            ? $"{commodities.Count} commodities loaded from UEX — Base Price = highest terminal sell price / SCU."
            : "No commodities returned by UEX — enter Base Price manually.";
    }

    public void SetLoadError(string message)
        => StatusMessage = $"Couldn't reach UEX ({message}) — enter Base Price manually.";
}
