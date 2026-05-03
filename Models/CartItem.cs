namespace SecureMarketMvc.Models;

public sealed class CartItem
{
    public int Id { get; set; }

    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public string? AnonymousCartId { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
