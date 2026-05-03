using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMarketMvc.Models;

public sealed class Product
{
    public int Id { get; set; }

    [Required, MaxLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 100000)]
    public decimal? OldPrice { get; set; }

    [Required, MaxLength(260)]
    public string ImageUrl { get; set; } = "/img/products/placeholder.svg";

    [MaxLength(80)]
    public string Brand { get; set; } = "Brand";

    [MaxLength(120)]
    public string Seller { get; set; } = "Guangji Trading LLC";

    [MaxLength(80)]
    public string Condition { get; set; } = "Brand new";

    [MaxLength(80)]
    public string Material { get; set; } = "Mixed";

    [Range(0, 5)]
    public decimal Rating { get; set; } = 4.8m;

    [Range(0, 1000000)]
    public int ReviewCount { get; set; } = 32;

    [Range(0, 1000000)]
    public int SoldCount { get; set; } = 154;

    [Range(0, 100000)]
    public int StockQuantity { get; set; } = 100;

    public bool IsFeatured { get; set; }
    public bool IsDeal { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
