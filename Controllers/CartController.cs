using Microsoft.AspNetCore.Mvc;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

public sealed class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new CartViewModel
        {
            Items = await _cartService.GetItemsAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        await _cartService.AddAsync(productId, quantity, cancellationToken);
        TempData["Success"] = "Product added to cart.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(int productId, int quantity, CancellationToken cancellationToken)
    {
        await _cartService.UpdateAsync(productId, quantity, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId, CancellationToken cancellationToken)
    {
        await _cartService.RemoveAsync(productId, cancellationToken);
        TempData["Success"] = "Item removed.";
        return RedirectToAction(nameof(Index));
    }
}
