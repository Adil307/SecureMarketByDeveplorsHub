using System.ComponentModel.DataAnnotations;

namespace SecureMarketMvc.Models;

public sealed class Category
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Icon { get; set; } = "box";

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
