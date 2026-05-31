using System.IO;
using System.Text.Json;

namespace ResourceValueCalculator;

/// <summary>
/// Loads the component blueprint catalog. Prefers a user copy written by the "Update Data" tab
/// (imported from scunpacked-data); otherwise falls back to the blueprints.json shipped with the app.
/// </summary>
public static class BlueprintService
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>%LocalAppData%\ResourceValueCalculator\blueprints.json — written by an import, preferred when present.</summary>
    public static string UserDataPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ResourceValueCalculator", "blueprints.json");

    public static bool HasUserData => File.Exists(UserDataPath);

    public static List<Blueprint> Load()
    {
        var path = HasUserData ? UserDataPath : Path.Combine(AppContext.BaseDirectory, "blueprints.json");
        if (!File.Exists(path)) return new List<Blueprint>();

        using var stream = File.OpenRead(path);
        var blueprints = JsonSerializer.Deserialize<List<Blueprint>>(stream, ReadOptions) ?? new List<Blueprint>();
        return blueprints.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Persists an imported catalog to the user data path (used by the Components tab from now on).</summary>
    public static void Save(IReadOnlyList<Blueprint> blueprints)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserDataPath)!);
        using var stream = File.Create(UserDataPath);
        JsonSerializer.Serialize(stream, blueprints, WriteOptions);
    }
}
