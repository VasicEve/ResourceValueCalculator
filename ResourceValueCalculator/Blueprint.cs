namespace ResourceValueCalculator;

/// <summary>A craftable component and the materials required to build it (from scunpacked-data).</summary>
public class Blueprint
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public List<BlueprintMaterial> Materials { get; init; } = new();

    // Used by the editable ComboBox for display + type-ahead search.
    public override string ToString() => Name;
}

/// <summary>One required material in a blueprint: a resource name/type and the SCU quantity needed.</summary>
public class BlueprintMaterial
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public decimal Quantity { get; init; }
}
