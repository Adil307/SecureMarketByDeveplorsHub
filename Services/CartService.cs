using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.Services;

public sealed class CartService : ICartService
{
    private const string CartCookieName = "__Host-SecureMarket.Cart";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<CartItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetOwner(createAnonymousIfMissing: true);

        return await QueryByOwner(owner)
            .Include(x => x.Product)
            .ThenInclude(p => p!.Category)
            .OrderByDescending(x => x.UpdatedUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetOwner(createAnonymousIfMissing: false);
        if (owner is null)
        {
            return 0;
        }

        return await QueryByOwner(owner).SumAsync(x => x.Quantity, cancellationToken);
    }

    public async Task AddAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        quantity = Math.Clamp(quantity, 1, 20);
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if (product is null || product.StockQuantity <= 0)
        {
            return;
        }

        var owner = GetOwner(createAnonymousIfMissing: true)!;
        var item = await QueryByOwner(owner).FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (item is null)
        {
            item = new CartItem
            {
                ProductId = productId,
                Quantity = Math.Min(quantity, product.StockQuantity),
                UpdatedUtc = DateTime.UtcNow
            };

            if (owner.UserId is not null)
            {
                item.AppUserId = owner.UserId;
            }
            else
            {
                item.AnonymousCartId = owner.AnonymousCartId;
            }

            _db.CartItems.Add(item);
        }
        else
        {
            item.Quantity = Math.Min(item.Quantity + quantity, product.StockQuantity);
            item.UpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var owner = GetOwner(createAnonymousIfMissing: false);
        if (owner is null)
        {
            return;
        }

        var item = await QueryByOwner(owner)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (item is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(Math.Clamp(quantity, 1, 20), item.Product?.StockQuantity ?? 20);
            item.UpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(int productId, CancellationToken cancellationToken = default)
    {
        var owner = GetOwner(createAnonymousIfMissing: false);
        if (owner is null)
        {
            return;
        }

        var item = await QueryByOwner(owner).FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearCurrentCartAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetOwner(createAnonymousIfMissing: false);
        if (owner is null)
        {
            return;
        }

        var items = await QueryByOwner(owner).ToListAsync(cancellationToken);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MergeAnonymousCartIntoUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        var anonymousId = context?.Request.Cookies[CartCookieName];

        if (string.IsNullOrWhiteSpace(anonymousId))
        {
            return;
        }

        var anonymousItems = await _db.CartItems
            .Where(x => x.AnonymousCartId == anonymousId)
            .ToListAsync(cancellationToken);

        foreach (var item in anonymousItems)
        {
            var existing = await _db.CartItems
                .FirstOrDefaultAsync(x => x.AppUserId == userId && x.ProductId == item.ProductId, cancellationToken);

            if (existing is null)
            {
                item.AppUserId = userId;
                item.AnonymousCartId = null;
                item.UpdatedUtc = DateTime.UtcNow;
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + item.Quantity, 20);
                existing.UpdatedUtc = DateTime.UtcNow;
                _db.CartItems.Remove(item);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        context?.Response.Cookies.Delete(CartCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true
        });
    }

    private IQueryable<CartItem> QueryByOwner(CartOwner owner)
    {
        if (owner.UserId is not null)
        {
            return _db.CartItems.Where(x => x.AppUserId == owner.UserId);
        }

        return _db.CartItems.Where(x => x.AnonymousCartId == owner.AnonymousCartId);
    }

    private CartOwner? GetOwner(bool createAnonymousIfMissing)
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return new CartOwner(userId, null);
        }

        var anonymousId = context.Request.Cookies[CartCookieName];

        if (string.IsNullOrWhiteSpace(anonymousId))
        {
            if (!createAnonymousIfMissing)
            {
                return null;
            }

            anonymousId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');

            context.Response.Cookies.Append(CartCookieName, anonymousId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            });
        }

        return new CartOwner(null, anonymousId);
    }

    private sealed record CartOwner(string? UserId, string? AnonymousCartId);
}
