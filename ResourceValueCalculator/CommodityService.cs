using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ResourceValueCalculator;

/// <summary>
/// Pulls live commodity prices from the public UEX Corp API (https://uexcorp.space).
/// Every sellable commodity (ores, refined metals, minerals, gems, …) is included via the
/// /commodities list; where a commodity is sold at terminals, Base Price is the highest
/// terminal sell price, otherwise the UEX average sell price.
/// </summary>
public static class CommodityService
{
    private const string CommoditiesUrl = "https://api.uexcorp.space/2.0/commodities";
    private const string TerminalsUrl = "https://api.uexcorp.space/2.0/terminals?type=commodity";
    private const string PricesUrl = "https://api.uexcorp.space/2.0/commodities_prices?id_terminal=";
    private const int TerminalsPerCall = 10;   // UEX caps id_terminal at 10 per request
    private const int MaxConcurrency = 4;       // gentle on the API so batches don't get rate-limited
    private const int MaxAttempts = 3;

    public static async Task<List<Commodity>> FetchAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ResourceValueCalculator/1.0");

        // 1) Authoritative catalog — one reliable call lists every commodity, so anything with a
        //    sell value (gems included) is always present even if the terminal sweep below misses it.
        var commodities = await http.GetFromJsonAsync<UexList<CommodityRow>>(CommoditiesUrl, ct);
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in commodities?.Data ?? new())
            if (!string.IsNullOrWhiteSpace(c.Name) && c.PriceSell is decimal ps && ps > 0)
                prices[c.Name!] = ps;

        // 2) Prefer the highest terminal sell price wherever we can get it.
        foreach (var kv in await FetchBestTerminalPricesAsync(http, ct))
            if (kv.Value > 0)
                prices[kv.Key] = kv.Value;

        return prices
            .Select(kv => new Commodity { Name = kv.Key, BasePrice = kv.Value })
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Highest recorded sell price per commodity across all commodity terminals (best place to sell).</summary>
    private static async Task<Dictionary<string, decimal>> FetchBestTerminalPricesAsync(HttpClient http, CancellationToken ct)
    {
        var best = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        UexList<TerminalRow>? terminals;
        try { terminals = await http.GetFromJsonAsync<UexList<TerminalRow>>(TerminalsUrl, ct); }
        catch { return best; } // the /commodities baseline still stands

        var ids = (terminals?.Data ?? new()).Select(t => t.Id).Where(id => id > 0).ToList();
        using var gate = new SemaphoreSlim(MaxConcurrency);

        var batches = ids.Chunk(TerminalsPerCall).Select(async batch =>
        {
            await gate.WaitAsync(ct);
            try { return await GetRowsWithRetryAsync(http, PricesUrl + string.Join(",", batch), ct); }
            finally { gate.Release(); }
        });

        foreach (var rows in await Task.WhenAll(batches))
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.CommodityName) && r.PriceSellMax is decimal pm && pm > 0 &&
                    (!best.TryGetValue(r.CommodityName!, out var cur) || pm > cur))
                    best[r.CommodityName!] = pm;

        return best;
    }

    private static async Task<List<TerminalPrice>> GetRowsWithRetryAsync(HttpClient http, string url, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                var resp = await http.GetFromJsonAsync<UexList<TerminalPrice>>(url, ct);
                return resp?.Data ?? new List<TerminalPrice>();
            }
            catch when (attempt < MaxAttempts)
            {
                await Task.Delay(250 * attempt, ct); // brief backoff, then retry
            }
            catch
            {
                return new List<TerminalPrice>(); // give up on this batch; baseline prices cover it
            }
        }
    }

    private sealed class UexList<T>
    {
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
    }

    private sealed class CommodityRow
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price_sell")] public decimal? PriceSell { get; set; }
    }

    private sealed class TerminalRow
    {
        [JsonPropertyName("id")] public int Id { get; set; }
    }

    private sealed class TerminalPrice
    {
        [JsonPropertyName("commodity_name")] public string? CommodityName { get; set; }
        [JsonPropertyName("price_sell_max")] public decimal? PriceSellMax { get; set; }
    }
}
