namespace ResourceValueCalculator;

/// <summary>A selectable commodity from the UEX catalog and its base price (highest terminal sell price per SCU).</summary>
public class Commodity
{
    public string Name { get; init; } = "";
    public decimal BasePrice { get; init; }

    // Used by the editable ComboBox for display + type-ahead search.
    public override string ToString() => Name;
}
