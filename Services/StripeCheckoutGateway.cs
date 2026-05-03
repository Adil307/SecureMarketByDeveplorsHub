using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.Services;

public sealed class StripeCheckoutGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly StripeOptions _options;
    private readonly ILogger<StripeCheckoutGateway> _logger;

    public StripeCheckoutGateway(HttpClient httpClient, IOptions<StripeOptions> options, ILogger<StripeCheckoutGateway> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Order order, string baseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey is missing. Add it with dotnet user-secrets or environment variables.");
        }

        var fields = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("client_reference_id", order.Id.ToString()),
            new("metadata[order_number]", order.OrderNumber),
            new("success_url", $"{baseUrl.TrimEnd('/')}/checkout/success?orderNumber={Uri.EscapeDataString(order.OrderNumber)}&session_id={{CHECKOUT_SESSION_ID}}"),
            new("cancel_url", $"{baseUrl.TrimEnd('/')}/checkout/cancel?orderNumber={Uri.EscapeDataString(order.OrderNumber)}")
        };

        var index = 0;
        foreach (var item in order.Items)
        {
            fields.Add(new($"line_items[{index}][price_data][currency]", _options.Currency));
            fields.Add(new($"line_items[{index}][price_data][unit_amount]", ToMinorUnits(item.UnitPrice).ToString()));
            fields.Add(new($"line_items[{index}][price_data][product_data][name]", item.ProductName));
            fields.Add(new($"line_items[{index}][quantity]", item.Quantity.ToString()));
            index++;
        }

        if (order.Shipping > 0)
        {
            fields.Add(new($"line_items[{index}][price_data][currency]", _options.Currency));
            fields.Add(new($"line_items[{index}][price_data][unit_amount]", ToMinorUnits(order.Shipping).ToString()));
            fields.Add(new($"line_items[{index}][price_data][product_data][name]", "Shipping"));
            fields.Add(new($"line_items[{index}][quantity]", "1"));
            index++;
        }

        if (order.Tax > 0)
        {
            fields.Add(new($"line_items[{index}][price_data][currency]", _options.Currency));
            fields.Add(new($"line_items[{index}][price_data][unit_amount]", ToMinorUnits(order.Tax).ToString()));
            fields.Add(new($"line_items[{index}][price_data][product_data][name]", "Estimated tax"));
            fields.Add(new($"line_items[{index}][quantity]", "1"));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Headers.Add("Idempotency-Key", $"secure-market-order-{order.OrderNumber}");
        request.Content = new FormUrlEncodedContent(fields);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe Checkout Session creation failed for order {OrderNumber}: {Status} {Body}",
                order.OrderNumber, response.StatusCode, body);
            throw new InvalidOperationException("Payment session could not be created. Please try again.");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        var sessionId = root.GetProperty("id").GetString();
        var redirectUrl = root.GetProperty("url").GetString();

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new InvalidOperationException("Payment provider returned an invalid checkout session.");
        }

        return new CheckoutSessionResult(sessionId, redirectUrl);
    }

    private static long ToMinorUnits(decimal amount)
    {
        return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
