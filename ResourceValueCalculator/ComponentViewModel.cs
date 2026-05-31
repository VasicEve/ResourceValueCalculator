using System.Collections.ObjectModel;

namespace ResourceValueCalculator;

/// <summary>
/// The "Components" tab: pick a blueprint and see the (fixed) resources required to craft it,
/// each priced from the UEX catalog — laid out just like the calculator.
/// </summary>
public class ComponentViewModel : ResourceListViewModel
{
    private IReadOnlyList<Commodity> _catalog = Array.Empty<Commodity>();

    /// <summary>Craftable components (blueprints) loaded from blueprints.json (scunpacked-data).</summary>
    public ObservableCollection<Blueprint> Blueprints { get; } = new();

    private Blueprint? _selectedBlueprint;
    public Blueprint? SelectedBlueprint
    {
        get => _selectedBlueprint;
        set { if (_selectedBlueprint != value) { _selectedBlueprint = value; OnPropertyChanged(); BuildRows(); } }
    }

    private string _statusMessage = "Loading blueprints…";
    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public void LoadBlueprints()
    {
        try
        {
            var blueprints = BlueprintService.Load();
            Blueprints.Clear();
            foreach (var b in blueprints)
                Blueprints.Add(b);

            StatusMessage = Blueprints.Count > 0
                ? $"{Blueprints.Count} components loaded (scunpacked-data). Pick one to see the resources it requires."
                : "No blueprints found in blueprints.json.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load blueprints.json ({ex.Message}).";
        }
    }

    public void SetCatalog(IReadOnlyList<Commodity> commodities)
    {
        _catalog = commodities;
        BuildRows(); // (re)price the current selection now that we have prices
    }

    public void SetLoadError(string message) { /* prices unavailable; required-resource rows show 0 */ }

    private void BuildRows()
    {
        Resources.Clear();
        if (_selectedBlueprint is null) return;

        foreach (var material in _selectedBlueprint.Materials)
        {
            Resources.Add(new ResourceRow
            {
                Name = material.Name,
                ScuQty = material.Quantity,
                BaseQuality = 1m,
                BasePrice = LookupPrice(material.Name),
                Margin = ProfitMargin
            });
        }
    }

    private decimal LookupPrice(string name)
    {
        foreach (var c in _catalog)
            if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                return c.BasePrice;
        return 0m;
    }
}
