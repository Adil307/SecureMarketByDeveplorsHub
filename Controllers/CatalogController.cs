using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;
using SecureMarketMvc.Models;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

public sealed class CatalogController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;

    public CatalogController(ApplicationDbContext db, ICartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(
        string? category,
        string? q,
        string view = "grid",
        string sort = "newest",
        decimal? min = null,
        decimal? max = null,
        CancellationToken cancellationToken = default)
    {
        view = view.Equals("list", StringComparison.OrdinalIgnoreCase) ? "list" : "grid";
        sort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort;

        var categories = await _db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

        var query = _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category!.Slug == category);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.Description.Contains(term) ||
                x.Brand.Contains(term) ||
                x.Category!.Name.Contains(term));
        }

        // SQLite cannot translate decimal comparison/order clauses. Keep the broad
        // category/search filter in SQL, then apply price filters and price/rating sort in memory.
        var productPool = await query.ToListAsync(cancellationToken);

        IEnumerable<Product> products = productPool;

        if (min is not null)
        {
            products = products.Where(x => x.Price >= min.Value);
        }

        if (max is not null)
        {
            products = products.Where(x => x.Price <= max.Value);
        }

        products = sort switch
        {
            "price-low" => products.OrderBy(x => x.Price),
            "price-high" => products.OrderByDescending(x => x.Price),
            "rating" => products.OrderByDescending(x => x.Rating),
            _ => products.OrderByDescending(x => x.CreatedUtc)
        };

        var model = new CatalogViewModel
        {
            Categories = categories,
            Products = products.Take(60).ToList(),
            Category = category,
            Query = q,
            ViewMode = view,
            Sort = sort,
            Min = min,
            Max = max,
            CartCount = await _cartService.GetCountAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var relatedPool = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.CategoryId == product.CategoryId && x.Id != product.Id)
            .ToListAsync(cancellationToken);

        var related = relatedPool
            .OrderByDescending(x => x.Rating)
            .Take(6)
            .ToList();

        return View(new ProductDetailsViewModel
        {
            Product = product,
            RelatedProducts = related,
            CartCount = await _cartService.GetCountAsync(cancellationToken)
        });
    }
}
