using System.IO;
using System.Text.Json;

namespace ResourceValueCalculator;

/// <summary>
/// Loads the bundled component blueprint catalog (blueprints.json, shipped next to the app).
/// Data extracted from star-crafting.com. To add/update components, edit blueprints.json.
/// </summary>
public static class BlueprintService
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static List<Blueprint> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "blueprints.json");
        if (!File.Exists(path)) return new List<Blueprint>();

        using var stream = File.OpenRead(path);
        var blueprints = JsonSerializer.Deserialize<List<Blueprint>>(stream, Options) ?? new List<Blueprint>();
        return blueprints.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
