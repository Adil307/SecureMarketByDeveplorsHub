using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;
using SecureMarketMvc.Models;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await CreateModelAsync(null, cancellationToken);
        return View("Upsert", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var model = await CreateModelAsync(product, cancellationToken);
        return View("Upsert", model);
    }

    // This prevents HTTP 405 if /AdminProducts/Upsert is opened directly
    [HttpGet]
    public IActionResult Upsert()
    {
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upsert(AdminProductViewModel model, CancellationToken cancellationToken)
    {
        var slug = SlugGenerator.Generate(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug);

        if (await _db.Products.AnyAsync(x => x.Slug == slug && x.Id != model.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug already exists.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return View(model);
        }

        Product product;

        if (model.Id == 0)
        {
            product = new Product();
            _db.Products.Add(product);
        }
        else
        {
            product = await _db.Products
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken)
                ?? throw new InvalidOperationException("Product not found.");
        }

        product.Name = model.Name.Trim();
        product.Slug = slug;
        product.Description = model.Description.Trim();
        product.Price = model.Price;
        product.OldPrice = model.OldPrice;
        product.ImageUrl = model.ImageUrl.Trim();
        product.Brand = model.Brand.Trim();
        product.Seller = model.Seller.Trim();
        product.Condition = model.Condition.Trim();
        product.Material = model.Material.Trim();
        product.Rating = model.Rating;
        product.StockQuantity = model.StockQuantity;
        product.IsFeatured = model.IsFeatured;
        product.IsDeal = model.IsDeal;
        product.CategoryId = model.CategoryId;

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Product saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is not null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Product deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminProductViewModel> CreateModelAsync(Product? product, CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (product is null)
        {
            return new AdminProductViewModel
            {
                Categories = categories,
                CategoryId = categories.FirstOrDefault()?.Id ?? 0,
                Name = string.Empty,
                Slug = string.Empty,
                Description = string.Empty,
                ImageUrl = "/img/products/",
                Brand = string.Empty,
                Seller = "Guangji Trading LLC",
                Condition = "Brand new",
                Material = string.Empty,
                Rating = 4.7m,
                StockQuantity = 100
            };
        }

        return new AdminProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            OldPrice = product.OldPrice,
            ImageUrl = product.ImageUrl,
            Brand = product.Brand,
            Seller = product.Seller,
            Condition = product.Condition,
            Material = product.Material,
            Rating = product.Rating,
            StockQuantity = product.StockQuantity,
            IsFeatured = product.IsFeatured,
            IsDeal = product.IsDeal,
            CategoryId = product.CategoryId,
            Categories = categories
        };
    }
}