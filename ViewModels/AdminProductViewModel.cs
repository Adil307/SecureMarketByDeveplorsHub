using System.ComponentModel.DataAnnotations;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class AdminProductViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(140)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? Slug { get; set; }

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public decimal? OldPrice { get; set; }

    [Required, MaxLength(260), Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = "/img/products/placeholder.svg";

    [Required, MaxLength(80)]
    public string Brand { get; set; } = "Brand";

    [Required, MaxLength(120)]
    public string Seller { get; set; } = "Guangji Trading LLC";

    [Required, MaxLength(80)]
    public string Condition { get; set; } = "Brand new";

    [Required, MaxLength(80)]
    public string Material { get; set; } = "Mixed";

    [Range(0, 5)]
    public decimal Rating { get; set; } = 4.7m;

    [Range(0, 100000)]
    public int StockQuantity { get; set; } = 100;

    public bool IsFeatured { get; set; }
    public bool IsDeal { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();
}
