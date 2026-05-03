using System.ComponentModel.DataAnnotations;

namespace SecureMarketMvc.Models;

public sealed class Address
{
    [Required, MaxLength(80)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Line2 { get; set; }

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MaxLength(2), RegularExpression("^[A-Z]{2}$")]
    public string CountryCode { get; set; } = "US";

    [Required, MaxLength(25), Phone]
    public string Phone { get; set; } = string.Empty;
}
