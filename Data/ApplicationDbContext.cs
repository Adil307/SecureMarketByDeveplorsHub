using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.Data;

public sealed class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        builder.Entity<Product>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        builder.Entity<Product>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Entity<Product>()
            .Property(x => x.OldPrice)
            .HasPrecision(18, 2);

        builder.Entity<CartItem>()
            .HasIndex(x => new { x.AppUserId, x.ProductId });

        builder.Entity<CartItem>()
            .HasIndex(x => new { x.AnonymousCartId, x.ProductId });

        builder.Entity<Order>()
            .HasIndex(x => x.OrderNumber)
            .IsUnique();

        builder.Entity<Order>()
            .OwnsOne(x => x.ShippingAddress);

        builder.Entity<Order>()
            .Property(x => x.SubTotal)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(x => x.Shipping)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(x => x.Tax)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(x => x.Total)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);
    }
}
