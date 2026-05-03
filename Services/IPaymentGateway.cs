using SecureMarketMvc.Models;

namespace SecureMarketMvc.Services;

public interface IPaymentGateway
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Order order, string baseUrl, CancellationToken cancellationToken = default);
}

public sealed record CheckoutSessionResult(string SessionId, string RedirectUrl);
