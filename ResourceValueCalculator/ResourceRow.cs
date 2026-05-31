using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ResourceValueCalculator;

/// <summary>
/// A single resource line. Price = (BasePrice × ScuQty) × BaseQuality × Margin
/// </summary>
public class ResourceRow : INotifyPropertyChanged
{
    private Commodity? _selectedCommodity;
    private string _name = "";
    private decimal _basePrice;
    private decimal _scuQty;
    private decimal _baseQuality = 1m;
    private decimal _margin = 1.2m;

    /// <summary>The commodity picked from the UEX dropdown. Selecting one fills in Name + BasePrice.</summary>
    public Commodity? SelectedCommodity
    {
        get => _selectedCommodity;
        set
        {
            if (SetField(ref _selectedCommodity, value) && value != null)
            {
                Name = value.Name;
                BasePrice = value.BasePrice;
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public decimal BasePrice
    {
        get => _basePrice;
        set { if (SetField(ref _basePrice, value)) OnPropertyChanged(nameof(Price)); }
    }

    public decimal ScuQty
    {
        get => _scuQty;
        set { if (SetField(ref _scuQty, value)) OnPropertyChanged(nameof(Price)); }
    }

    public decimal BaseQuality
    {
        get => _baseQuality;
        set { if (SetField(ref _baseQuality, value)) OnPropertyChanged(nameof(Price)); }
    }

    /// <summary>Profit-margin multiplier (the editable "× 1.2"); set globally from the view model.</summary>
    public decimal Margin
    {
        get => _margin;
        set { if (SetField(ref _margin, value)) OnPropertyChanged(nameof(Price)); }
    }

    /// <summary>Quantity unit shown in the grid: "SCU" (raw resources) or "items" (individual item materials).</summary>
    public string Unit { get; set; } = "SCU";

    // Item-count materials are sold in milli-SCU: 1000 units = 1 SCU
    // (e.g. 100 units of Aphorite = 0.1 SCU; verified on the in-game commodity market).
    private const decimal UnitsPerScu = 1000m;

    /// <summary>The quantity expressed in SCU — item-count materials are converted from units.</summary>
    private decimal QuantityScu =>
        string.Equals(Unit, "items", StringComparison.OrdinalIgnoreCase) ? ScuQty / UnitsPerScu : ScuQty;

    /// <summary>(BasePrice × quantity-in-SCU) × BaseQuality × Margin</summary>
    public decimal Price => BasePrice * QuantityScu * BaseQuality * Margin;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
