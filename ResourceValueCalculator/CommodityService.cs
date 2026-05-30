using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ResourceValueCalculator;

/// <summary>
/// Pulls live per-terminal commodity prices from the public UEX Corp API
/// (https://uexcorp.space/commodities). Base Price = the highest terminal
/// sell price recorded for a commodity (UEX field "price_sell_max").
/// </summary>
public static class CommodityService
{
    private const string TerminalsUrl = "https://api.uexcorp.space/2.0/terminals?type=commodity";
    private const string PricesUrl = "https://api.uexcorp.space/2.0/commodities_prices?id_terminal=";
    private const int TerminalsPerCall = 10; // UEX caps id_terminal at 10 IDs per request
    private const int MaxConcurrency = 8;

    public static async Task<List<Commodity>> FetchAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ResourceValueCalculator/1.0");

        // 1) Every commodity terminal in the game.
        var terminals = await http.GetFromJsonAsync<TerminalsResponse>(TerminalsUrl, ct);
        var ids = (terminals?.Data ?? new List<Terminal>())
            .Select(t => t.Id)
            .Where(id => id > 0)
            .ToList();

        // 2) Fetch prices in batches of up to 10 terminals each (throttled).
        using var gate = new SemaphoreSlim(MaxConcurrency);
        var tasks = ids.Chunk(TerminalsPerCall).Select(async batch =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var url = PricesUrl + string.Join(",", batch);
                var resp = await http.GetFromJsonAsync<PricesResponse>(url, ct);
                return resp?.Data ?? new List<TerminalPrice>();
            }
            catch
            {
                return new List<TerminalPrice>(); // tolerate an individual batch failing
            }
            finally
            {
                gate.Release();
            }
        });

        var rows = (await Task.WhenAll(tasks)).SelectMany(r => r);

        // 3) Base Price = highest recorded sell price across all terminals, per commodity.
        return rows
            .Where(r => r.IdCommodity > 0
                        && !string.IsNullOrWhiteSpace(r.CommodityName)
                        && r.PriceSellMax > 0)
            .GroupBy(r => r.IdCommodity)
            .Select(g => new Commodity
            {
                Name = g.First().CommodityName!,
                BasePrice = g.Max(r => r.PriceSellMax)
            })
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class TerminalsResponse
    {
        [JsonPropertyName("data")] public List<Terminal> Data { get; set; } = new();
    }

    private sealed class Terminal
    {
        [JsonPropertyName("id")] public int Id { get; set; }
    }

    private sealed class PricesResponse
    {
        [JsonPropertyName("data")] public List<TerminalPrice> Data { get; set; } = new();
    }

    /// <summary>One commodity's price record at one terminal.</summary>
    private sealed class TerminalPrice
    {
        [JsonPropertyName("id_commodity")] public int IdCommodity { get; set; }
        [JsonPropertyName("commodity_name")] public string? CommodityName { get; set; }

        // Highest sell price recorded at this terminal (per SCU).
        [JsonPropertyName("price_sell_max")] public decimal PriceSellMax { get; set; }
    }
}
