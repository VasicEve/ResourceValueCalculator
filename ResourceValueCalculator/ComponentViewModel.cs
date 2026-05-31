using System.Collections.ObjectModel;

namespace ResourceValueCalculator;

/// <summary>
/// The "Components" tab: pick a blueprint and see the (fixed) resources required to craft it,
/// each priced from the UEX catalog — laid out just like the calculator.
/// </summary>
public class ComponentViewModel : ResourceListViewModel
{
    private IReadOnlyList<Commodity> _catalog = Array.Empty<Commodity>();

    /// <summary>Full blueprint catalog loaded from blueprints.json (scunpacked-data).</summary>
    public ObservableCollection<Blueprint> Blueprints { get; } = new();

    /// <summary>Blueprints shown in the picker after the category filter is applied.</summary>
    public ObservableCollection<Blueprint> FilteredBlueprints { get; } = new();

    /// <summary>"All categories" plus each distinct blueprint category.</summary>
    public ObservableCollection<string> Categories { get; } = new();

    private const string AllCategories = "All categories";

    private string _selectedCategory = AllCategories;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { if (_selectedCategory != value) { _selectedCategory = value ?? AllCategories; OnPropertyChanged(); ApplyFilter(); } }
    }

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

            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var c in Blueprints
                         .Select(b => b.Category)
                         .Where(c => !string.IsNullOrWhiteSpace(c))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                Categories.Add(c);

            _selectedCategory = AllCategories;
            OnPropertyChanged(nameof(SelectedCategory));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load blueprints.json ({ex.Message}).";
        }
    }

    /// <summary>Rebuilds the picker list from the selected category and refreshes the status line.</summary>
    private void ApplyFilter()
    {
        IEnumerable<Blueprint> items = Blueprints;
        if (!string.Equals(_selectedCategory, AllCategories, StringComparison.Ordinal))
            items = items.Where(b => string.Equals(b.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));

        FilteredBlueprints.Clear();
        foreach (var b in items)
            FilteredBlueprints.Add(b);

        // Drop the current selection if the new filter hides it.
        if (SelectedBlueprint != null && !FilteredBlueprints.Contains(SelectedBlueprint))
            SelectedBlueprint = null;

        StatusMessage = Blueprints.Count == 0
            ? "No blueprints found in blueprints.json."
            : _selectedCategory == AllCategories
                ? $"{Blueprints.Count} components (scunpacked-data). Filter by category, or type to search."
                : $"{FilteredBlueprints.Count} of {Blueprints.Count} components — {_selectedCategory}.";
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
