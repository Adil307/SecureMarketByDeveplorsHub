using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<Category> Categories { get; init; } = Array.Empty<Category>();
    public IReadOnlyList<Product> Deals { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Product> HomeOutdoor { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Product> Recommended { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Product> Electronics { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Product> Latest { get; init; } = Array.Empty<Product>();
    public int CartCount { get; init; }
}
