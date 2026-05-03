using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class CartViewModel
{
    public IReadOnlyList<CartItem> Items { get; init; } = Array.Empty<CartItem>();
    public decimal SubTotal => Items.Sum(x => (x.Product?.Price ?? 0) * x.Quantity);
    public decimal Shipping => Items.Count == 0 ? 0 : 10m;
    public decimal Tax => Math.Round(SubTotal * 0.05m, 2);
    public decimal Total => SubTotal + Shipping + Tax;
    public int Count => Items.Sum(x => x.Quantity);
}
