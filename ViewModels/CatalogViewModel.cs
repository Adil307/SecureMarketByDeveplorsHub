using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class CatalogViewModel
{
    public IReadOnlyList<Category> Categories { get; init; } = Array.Empty<Category>();
    public IReadOnlyList<Product> Products { get; init; } = Array.Empty<Product>();
    public string? Category { get; init; }
    public string? Query { get; init; }
    public string ViewMode { get; init; } = "grid";
    public string Sort { get; init; } = "newest";
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public int CartCount { get; init; }
}
