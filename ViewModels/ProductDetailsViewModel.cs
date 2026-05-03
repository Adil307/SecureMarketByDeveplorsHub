using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class ProductDetailsViewModel
{
    public Product Product { get; init; } = new();
    public IReadOnlyList<Product> RelatedProducts { get; init; } = Array.Empty<Product>();
    public int CartCount { get; init; }
}
