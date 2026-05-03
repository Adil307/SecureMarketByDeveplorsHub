using System.ComponentModel.DataAnnotations;

namespace SecureMarketMvc.Services;

public sealed class StripeOptions
{
    [MaxLength(200)]
    public string SecretKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string WebhookSecret { get; set; } = string.Empty;

    [RegularExpression("^[a-z]{3}$")]
    public string Currency { get; set; } = "usd";
}
