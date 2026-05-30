using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ResourceValueCalculator;

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ResourceRow> Resources { get; } = new();

    /// <summary>Commodity catalog pulled live from UEX (https://uexcorp.space/commodities).</summary>
    public ObservableCollection<Commodity> Catalog { get; } = new();

    private string _statusMessage = "Loading commodities from UEX…";
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    private decimal _grandTotal;
    public decimal GrandTotal
    {
        get => _grandTotal;
        private set
        {
            if (_grandTotal == value) return;
            _grandTotal = value;
            OnPropertyChanged();
        }
    }

    private decimal _profitMargin = 1.2m;
    /// <summary>Editable profit-margin multiplier applied to every resource (replaces the fixed × 1.2).</summary>
    public decimal ProfitMargin
    {
        get => _profitMargin;
        set
        {
            if (_profitMargin == value) return;
            _profitMargin = value;
            OnPropertyChanged();
            foreach (var row in Resources)
                row.Margin = value; // each row recomputes Price → total updates
        }
    }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }

    public MainViewModel()
    {
        AddCommand = new RelayCommand(_ => Resources.Add(new ResourceRow { Margin = ProfitMargin }));
        RemoveCommand = new RelayCommand(row =>
        {
            if (row is ResourceRow r) Resources.Remove(r);
        });

        Resources.CollectionChanged += OnResourcesChanged;
        Resources.Add(new ResourceRow { Margin = ProfitMargin }); // start with one empty row
    }

    /// <summary>Fetches the live commodity list + sell prices from the UEX API.</summary>
    public async Task LoadCatalogAsync()
    {
        try
        {
            var commodities = await CommodityService.FetchAsync();
            Catalog.Clear();
            foreach (var c in commodities)
                Catalog.Add(c);

            StatusMessage = Catalog.Count > 0
                ? $"{Catalog.Count} commodities loaded from UEX — Base Price = highest terminal sell price / SCU."
                : "No commodities returned by UEX — enter Base Price manually.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't reach UEX ({ex.Message}) — enter Base Price manually.";
        }
    }

    private void OnResourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (ResourceRow row in e.OldItems)
                row.PropertyChanged -= OnRowChanged;

        if (e.NewItems != null)
            foreach (ResourceRow row in e.NewItems)
                row.PropertyChanged += OnRowChanged;

        RecalculateTotal();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResourceRow.Price))
            RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        decimal total = 0m;
        foreach (var row in Resources)
            total += row.Price;
        GrandTotal = total;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
