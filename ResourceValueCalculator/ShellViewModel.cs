namespace ResourceValueCalculator;

/// <summary>Top-level view model hosting the two tabs and loading shared data once.</summary>
public class ShellViewModel
{
    public CalculatorViewModel Calculator { get; } = new();
    public ComponentViewModel Components { get; } = new();

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
}
