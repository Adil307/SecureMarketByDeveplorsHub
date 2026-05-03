using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;
using SecureMarketMvc.Models;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

[Authorize]
public sealed class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ApplicationDbContext db,
        ICartService cartService,
        IPaymentGateway paymentGateway,
        ILogger<CheckoutController> logger)
    {
        _db = db;
        _cartService = cartService;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _cartService.GetItemsAsync(cancellationToken);
        if (items.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutViewModel
        {
            Items = items,
            FullName = User.Identity?.Name ?? string.Empty,
            CountryCode = "US"
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model, CancellationToken cancellationToken)
    {
        var items = await _cartService.GetItemsAsync(cancellationToken);

        if (items.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        model = new CheckoutViewModel
        {
            FullName = model.FullName,
            Line1 = model.Line1,
            Line2 = model.Line2,
            City = model.City,
            State = model.State,
            PostalCode = model.PostalCode,
            CountryCode = model.CountryCode?.ToUpperInvariant() ?? "US",
            Phone = model.Phone,
            Items = items
        };

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var order = new Order
        {
            OrderNumber = CreateOrderNumber(),
            AppUserId = userId,
            OrderedAtUtc = DateTime.UtcNow,
            ShippingAddress = model.ToAddress(),
            SubTotal = model.SubTotal,
            Shipping = model.Shipping,
            Tax = model.Tax,
            Total = model.Total,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.New,
            Items = items.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = x.Product?.Name ?? "Product",
                ProductImageUrl = x.Product?.ImageUrl ?? "/img/products/placeholder.svg",
                UnitPrice = x.Product?.Price ?? 0,
                Quantity = x.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var session = await _paymentGateway.CreateCheckoutSessionAsync(order, baseUrl, cancellationToken);
            order.PaymentSessionId = session.SessionId;
            await _db.SaveChangesAsync(cancellationToken);

            await _cartService.ClearCurrentCartAsync(cancellationToken);

            return Redirect(session.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for order {OrderNumber}", order.OrderNumber);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(string orderNumber, string? session_id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber && x.AppUserId == userId, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(string orderNumber, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders.FirstOrDefaultAsync(
            x => x.OrderNumber == orderNumber && x.AppUserId == userId,
            cancellationToken);

        if (order is not null && order.PaymentStatus == PaymentStatus.Pending)
        {
            order.PaymentStatus = PaymentStatus.Cancelled;
            await _db.SaveChangesAsync(cancellationToken);
        }

        TempData["Error"] = "Payment was cancelled.";
        return RedirectToAction("Index", "Cart");
    }

    private static string CreateOrderNumber()
    {
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);
        return $"SM{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(bytes)}";
    }
}
