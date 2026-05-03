using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

public sealed class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;

    public HomeController(ApplicationDbContext db, ICartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categoryOrder = new Dictionary<string, int>
        {
            ["clothes-wear"] = 1,
            ["consumer-electronics"] = 2,
            ["home-outdoor"] = 3,
            ["machinery-tools"] = 4,
            ["mobile-accessory"] = 5,
            ["sports-outdoor"] = 6,
            ["animal-pets"] = 7
        };

        var categoryPool = await _db.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var categories = categoryPool
            .OrderBy(x => categoryOrder.TryGetValue(x.Slug, out var order) ? order : 99)
            .ThenBy(x => x.Name)
            .ToList();

        var dealPool = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.IsDeal)
            .ToListAsync(cancellationToken);

        var deals = dealPool
            .OrderBy(x => x.Price)
            .Take(5)
            .ToList();

        var recommended = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.IsFeatured)
            .OrderBy(x => x.Name)
            .Take(8)
            .ToListAsync(cancellationToken);

        var homeOutdoor = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.Category!.Slug == "home-outdoor")
            .OrderBy(x => x.Name)
            .Take(8)
            .ToListAsync(cancellationToken);

        var electronicsPool = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.Category!.Slug == "consumer-electronics" || x.Category!.Slug == "mobile-accessory")
            .ToListAsync(cancellationToken);

        var electronics = electronicsPool
            .OrderByDescending(x => x.Rating)
            .Take(8)
            .ToList();

        var latest = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Take(8)
            .ToListAsync(cancellationToken);

        var model = new HomeIndexViewModel
        {
            Categories = categories,
            Deals = deals,
            HomeOutdoor = homeOutdoor,
            Recommended = recommended,
            Electronics = electronics,
            Latest = latest,
            CartCount = await _cartService.GetCountAsync(cancellationToken)
        };

        return View(model);
    }

    public IActionResult Privacy() => View();

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
