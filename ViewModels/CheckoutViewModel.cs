using System.ComponentModel.DataAnnotations;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.ViewModels;

public sealed class CheckoutViewModel
{
    [Required, MaxLength(80), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Address line 1")]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(120), Display(Name = "Address line 2")]
    public string? Line2 { get; set; }

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(20), Display(Name = "Postal code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MaxLength(2), RegularExpression("^[A-Z]{2}$"), Display(Name = "Country code")]
    public string CountryCode { get; set; } = "US";

    [Required, MaxLength(25), Phone]
    public string Phone { get; set; } = string.Empty;

    public IReadOnlyList<CartItem> Items { get; init; } = Array.Empty<CartItem>();

    public decimal SubTotal => Items.Sum(x => (x.Product?.Price ?? 0) * x.Quantity);
    public decimal Shipping => Items.Count == 0 ? 0 : 10m;
    public decimal Tax => Math.Round(SubTotal * 0.05m, 2);
    public decimal Total => SubTotal + Shipping + Tax;

    public Address ToAddress() => new()
    {
        FullName = FullName.Trim(),
        Line1 = Line1.Trim(),
        Line2 = string.IsNullOrWhiteSpace(Line2) ? null : Line2.Trim(),
        City = City.Trim(),
        State = State.Trim(),
        PostalCode = PostalCode.Trim(),
        CountryCode = CountryCode.Trim().ToUpperInvariant(),
        Phone = Phone.Trim()
    };
}
