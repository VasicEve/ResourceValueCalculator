using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ResourceValueCalculator;

/// <summary>
/// Shared base for the calculator and component tabs: a list of resource rows, an editable
/// profit-margin multiplier applied to every row, and a live grand total of the rows' prices.
/// </summary>
public abstract class ResourceListViewModel : INotifyPropertyChanged
{
    private readonly List<ResourceRow> _tracked = new();

    public ObservableCollection<ResourceRow> Resources { get; } = new();

    private decimal _grandTotal;
    public decimal GrandTotal
    {
        get => _grandTotal;
        private set { if (_grandTotal != value) { _grandTotal = value; OnPropertyChanged(); } }
    }

    private decimal _profitMargin = 1.2m;
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

    protected ResourceListViewModel()
    {
        Resources.CollectionChanged += OnResourcesChanged;
    }

    // Resync row subscriptions to the current collection (handles Add, Remove, and Reset/Clear).
    private void OnResourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var row in _tracked)
            row.PropertyChanged -= OnRowChanged;
        _tracked.Clear();

        foreach (var row in Resources)
        {
            row.PropertyChanged += OnRowChanged;
            _tracked.Add(row);
        }
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

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
