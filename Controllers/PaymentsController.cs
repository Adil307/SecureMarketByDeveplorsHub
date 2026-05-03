using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureMarketMvc.Data;
using SecureMarketMvc.Models;
using SecureMarketMvc.Services;

namespace SecureMarketMvc.Controllers;

public sealed class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly StripeOptions _options;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ApplicationDbContext db, IOptions<StripeOptions> options, ILogger<PaymentsController> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("/payments/stripe/webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        if (!VerifyStripeSignature(payload, signatureHeader, _options.WebhookSecret))
        {
            _logger.LogWarning("Rejected Stripe webhook because signature verification failed.");
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = root.GetProperty("type").GetString();

        if (eventType == "checkout.session.completed")
        {
            var session = root.GetProperty("data").GetProperty("object");
            var sessionId = session.GetProperty("id").GetString();
            var clientReferenceId = session.TryGetProperty("client_reference_id", out var reference)
                ? reference.GetString()
                : null;
            var paymentStatus = session.TryGetProperty("payment_status", out var status)
                ? status.GetString()
                : null;
            var paymentIntentId = session.TryGetProperty("payment_intent", out var intent)
                ? intent.GetString()
                : null;

            if (int.TryParse(clientReferenceId, out var orderId) && paymentStatus == "paid")
            {
                var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
                if (order is not null)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.OrderStatus = OrderStatus.Processing;
                    order.PaymentSessionId = sessionId;
                    order.PaymentIntentId = paymentIntentId;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return Ok();
    }

    private static bool VerifyStripeSignature(string payload, string header, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(header) ||
            string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var timestamp = string.Empty;
        var signatures = new List<string>();

        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex < 1)
            {
                continue;
            }

            var key = part[..separatorIndex];
            var value = part[(separatorIndex + 1)..];

            if (key == "t")
            {
                timestamp = value;
            }
            else if (key == "v1")
            {
                signatures.Add(value);
            }
        }

        if (!long.TryParse(timestamp, out var unixTimestamp) || signatures.Count == 0)
        {
            return false;
        }

        var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unixTimestamp);
        if (age > 300)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(keyBytes);
        var expected = Convert.ToHexString(hmac.ComputeHash(payloadBytes)).ToLowerInvariant();

        foreach (var signature in signatures)
        {
            if (signature.Length != expected.Length)
            {
                continue;
            }

            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var signatureBytes = Encoding.ASCII.GetBytes(signature);

            if (CryptographicOperations.FixedTimeEquals(expectedBytes, signatureBytes))
            {
                return true;
            }
        }

        return false;
    }
}
