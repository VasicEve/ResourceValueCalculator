using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ResourceValueCalculator;

/// <summary>
/// Imports crafting blueprints live from the scunpacked-data project on GitHub
/// (datamined Star Citizen game files) and distills each one into a simple
/// bill of materials — the same shape stored in blueprints.json.
/// </summary>
public static class ScunpackedImporter
{
    public const string SourceUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/blueprints.json";

    public static List<Blueprint> Import(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Downloading blueprints from scunpacked-data…");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ResourceValueCalculator/1.0");

        using var stream = http.GetStreamAsync(SourceUrl, ct).GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(stream);

        progress?.Report("Parsing blueprints…");
        var raw = new List<Blueprint>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            if (el.ValueKind != JsonValueKind.Object) continue;

            // Only "creation" (crafting) blueprints with a named output.
            if (el.TryGetProperty("Kind", out var kind) &&
                !string.Equals(kind.GetString(), "creation", StringComparison.Ordinal))
                continue;
            if (!el.TryGetProperty("Output", out var output) || output.ValueKind != JsonValueKind.Object)
                continue;
            var name = output.TryGetProperty("Name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var type = output.TryGetProperty("Type", out var t) ? (t.GetString() ?? "") : "";

            if (!el.TryGetProperty("Tiers", out var tiers) ||
                tiers.ValueKind != JsonValueKind.Array || tiers.GetArrayLength() == 0)
                continue;
            var tier0 = tiers[0];
            if (tier0.ValueKind != JsonValueKind.Object ||
                !tier0.TryGetProperty("Requirements", out var req) ||
                req.ValueKind != JsonValueKind.Object)
                continue;

            // One resource per requirement group (slot), from the base tier.
            var materials = new List<BlueprintMaterial>();
            if (req.TryGetProperty("Children", out var groups) && groups.ValueKind == JsonValueKind.Array)
            {
                foreach (var g in groups.EnumerateArray())
                    if (TryFirstMaterial(g, out var matName, out var qty, out var unit) && !string.IsNullOrWhiteSpace(matName))
                        materials.Add(new BlueprintMaterial { Name = matName!, Quantity = decimal.Round(qty, 6), Unit = unit });
            }
            if (materials.Count == 0) continue;

            raw.Add(new Blueprint { Name = name!, Category = PrettyCategory(type), Materials = materials });
        }

        progress?.Report($"Distilling {raw.Count} blueprints…");
        return DedupeAndDisambiguate(raw);
    }

    /// <summary>
    /// Depth-first: the first material in a requirement group's subtree. A slot is filled by either a
    /// raw "resource" (quantity in SCU) or an "item" such as a refined material (quantity is a count).
    /// </summary>
    private static bool TryFirstMaterial(JsonElement node, out string? name, out decimal quantity, out string unit)
    {
        name = null;
        quantity = 0m;
        unit = "SCU";
        if (node.ValueKind != JsonValueKind.Object) return false;

        if (node.TryGetProperty("Kind", out var k))
        {
            var kind = k.GetString();
            if (string.Equals(kind, "resource", StringComparison.Ordinal))
            {
                name = node.TryGetProperty("Name", out var rn) ? rn.GetString() : null;
                if (node.TryGetProperty("QuantityScu", out var q) && q.ValueKind == JsonValueKind.Number)
                    q.TryGetDecimal(out quantity);
                unit = "SCU";
                return name != null;
            }
            if (string.Equals(kind, "item", StringComparison.Ordinal))
            {
                name = node.TryGetProperty("Name", out var inm) ? inm.GetString() : null;
                if (node.TryGetProperty("Quantity", out var q) && q.ValueKind == JsonValueKind.Number)
                    q.TryGetDecimal(out quantity);
                unit = "items"; // individual items, not SCU
                return name != null;
            }
        }

        if (node.TryGetProperty("Children", out var children) && children.ValueKind == JsonValueKind.Array)
            foreach (var c in children.EnumerateArray())
                if (TryFirstMaterial(c, out name, out quantity, out unit)) return true;
        return false;
    }

    private static string PrettyCategory(string type)
    {
        const string armorPrefix = "Char_Armor_";
        if (type.StartsWith(armorPrefix, StringComparison.Ordinal))
            return "Armor / " + type.Substring(armorPrefix.Length);
        return type switch
        {
            "WeaponPersonal" => "FPS Weapon",
            "WeaponGun" => "Vehicle Weapon",
            "WeaponMining" => "Mining Weapon",
            _ => Regex.Replace(type, "([a-z0-9])([A-Z])", "$1 $2")
        };
    }

    /// <summary>Drop exact duplicates; disambiguate same-named recipes by their distinguishing material.</summary>
    private static List<Blueprint> DedupeAndDisambiguate(List<Blueprint> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<Blueprint>();
        foreach (var b in items)
        {
            var sig = b.Name + "||" + string.Join("|",
                b.Materials.Select(m => m.Name + ":" + m.Quantity).OrderBy(s => s, StringComparer.Ordinal));
            if (seen.Add(sig)) unique.Add(b);
        }

        var result = new List<Blueprint>();
        foreach (var group in unique.GroupBy(b => b.Name, StringComparer.Ordinal))
        {
            var g = group.ToList();
            if (g.Count == 1) { result.Add(g[0]); continue; }

            var firstMaterials = g.Select(b => b.Materials.Count > 0 ? b.Materials[0].Name : "");
            bool byMaterial = firstMaterials.Distinct(StringComparer.OrdinalIgnoreCase).Count() == g.Count;
            for (int i = 0; i < g.Count; i++)
            {
                var b = g[i];
                var suffix = byMaterial && b.Materials.Count > 0 ? b.Materials[0].Name : $"Variant {i + 1}";
                result.Add(new Blueprint { Name = $"{b.Name} ({suffix})", Category = b.Category, Materials = b.Materials });
            }
        }
        return result.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
