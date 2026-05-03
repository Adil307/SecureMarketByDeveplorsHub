using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Data;

namespace SecureMarketMvc.Controllers;

[Authorize]
public sealed class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(x => x.AppUserId == userId)
            .OrderByDescending(x => x.OrderedAtUtc)
            .Take(30)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    public async Task<IActionResult> Details(string orderNumber, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId && x.OrderNumber == orderNumber, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }
}
