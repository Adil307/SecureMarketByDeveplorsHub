using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureMarketMvc.Models;

namespace SecureMarketMvc.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureCreatedAsync();

        await SeedRolesAndAdminAsync(services);
        await SeedCatalogAsync(db);
        await SeedDealProductsAsync(db);
    }

    private static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not create role: " +
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        var email = configuration["SeedAdmin:Email"]?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            email = "admin@securemarket.local";
        }

        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Admin@12345";
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Secure Market Admin",
                LockoutEnabled = false,
                LockoutEnd = null,
                AccessFailedCount = 0
            };

            var createResult = await userManager.CreateAsync(admin, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not seed admin user: " +
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            admin.UserName = email;
            admin.Email = email;
            admin.NormalizedEmail = email.ToUpperInvariant();
            admin.NormalizedUserName = email.ToUpperInvariant();
            admin.EmailConfirmed = true;
            admin.FullName = string.IsNullOrWhiteSpace(admin.FullName)
                ? "Secure Market Admin"
                : admin.FullName;
            admin.LockoutEnabled = false;
            admin.LockoutEnd = null;
            admin.AccessFailedCount = 0;

            var updateResult = await userManager.UpdateAsync(admin);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not update admin user: " +
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }

            var hasPassword = await userManager.HasPasswordAsync(admin);

            if (hasPassword)
            {
                var removePasswordResult = await userManager.RemovePasswordAsync(admin);

                if (!removePasswordResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not remove old admin password: " +
                        string.Join("; ", removePasswordResult.Errors.Select(e => e.Description)));
                }
            }

            var addPasswordResult = await userManager.AddPasswordAsync(admin, password);

            if (!addPasswordResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not set admin password: " +
                    string.Join("; ", addPasswordResult.Errors.Select(e => e.Description)));
            }

            await userManager.ResetAccessFailedCountAsync(admin);
            await userManager.SetLockoutEndDateAsync(admin, null);
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not add admin role: " +
                    string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        var categoryData = new[]
        {
            new Category { Name = "Mobile accessory", Slug = "mobile-accessory", Icon = "phone" },
            new Category { Name = "Consumer electronics", Slug = "consumer-electronics", Icon = "bolt" },
            new Category { Name = "Clothes and wear", Slug = "clothes-wear", Icon = "shirt" },
            new Category { Name = "Home and outdoor", Slug = "home-outdoor", Icon = "home" },
            new Category { Name = "Machinery tools", Slug = "machinery-tools", Icon = "tool" },
            new Category { Name = "Sports and outdoor", Slug = "sports-outdoor", Icon = "bike" },
            new Category { Name = "Animal and pets", Slug = "animal-pets", Icon = "paw" }
        };

        foreach (var category in categoryData)
        {
            var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == category.Slug);

            if (existing is null)
            {
                db.Categories.Add(category);
            }
            else
            {
                existing.Name = category.Name;
                existing.Icon = category.Icon;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDealProductsAsync(ApplicationDbContext db)
    {
        async Task<int> Cat(string slug)
        {
            return await db.Categories
                .Where(c => c.Slug == slug)
                .Select(c => c.Id)
                .SingleAsync();
        }

        var allProducts = await db.Products.ToListAsync();

        foreach (var product in allProducts)
        {
            product.IsDeal = false;
        }

        var dealProducts = new List<Product>
        {
            CreateProduct(
                "Smart watches",
                "smart-watches",
                "Fitness smartwatch with notifications and health tracking.",
                111.00m,
                139.00m,
                "/img/products/smartwatch-silver.png",
                await Cat("consumer-electronics"),
                true,
                true,
                "TimePro",
                "Steel / silicone"),

            CreateProduct(
                "Laptops",
                "laptops",
                "Modern laptop for work, study and entertainment.",
                799.00m,
                940.00m,
                "/img/products/laptop.png",
                await Cat("consumer-electronics"),
                true,
                true,
                "TechPro",
                "Aluminium / plastic"),

            CreateProduct(
                "GoPro cameras",
                "gopro-cameras",
                "Action camera for travel, sports and outdoor recording.",
                299.00m,
                499.00m,
                "/img/products/camera.png",
                await Cat("consumer-electronics"),
                true,
                true,
                "GoPro",
                "Plastic / glass"),

            CreateProduct(
                "Headphones",
                "headphones",
                "Premium headphones with comfortable ear cushions.",
                57.70m,
                69.00m,
                "/img/products/headphones-white.png",
                await Cat("consumer-electronics"),
                true,
                true,
                "AudioMax",
                "Plastic"),

            CreateProduct(
                "Canon cameras",
                "canon-cameras",
                "DSLR camera with zoom lens for sharp photos.",
                980.00m,
                null,
                "/img/products/camera.png",
                await Cat("consumer-electronics"),
                true,
                true,
                "Canon",
                "Plastic / glass")
        };

        foreach (var product in dealProducts)
        {
            var existing = await db.Products.FirstOrDefaultAsync(p => p.Slug == product.Slug);

            if (existing is null)
            {
                db.Products.Add(product);
            }
            else
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.OldPrice = product.OldPrice;
                existing.ImageUrl = product.ImageUrl;
                existing.CategoryId = product.CategoryId;
                existing.IsFeatured = product.IsFeatured;
                existing.IsDeal = product.IsDeal;
                existing.Brand = product.Brand;
                existing.Material = product.Material;
                existing.Condition = product.Condition;
                existing.Seller = product.Seller;
                existing.Rating = product.Rating;
                existing.ReviewCount = product.ReviewCount;
                existing.SoldCount = product.SoldCount;
                existing.StockQuantity = product.StockQuantity;
            }
        }

        await db.SaveChangesAsync();
    }

    private static Product CreateProduct(
        string name,
        string slug,
        string description,
        decimal price,
        decimal? oldPrice,
        string imageUrl,
        int categoryId,
        bool featured,
        bool deal,
        string brand,
        string material)
    {
        return new Product
        {
            Name = name,
            Slug = slug,
            Description = description,
            Price = price,
            OldPrice = oldPrice,
            ImageUrl = imageUrl,
            CategoryId = categoryId,
            IsFeatured = featured,
            IsDeal = deal,
            Brand = brand,
            Material = material,
            Condition = "Brand new",
            Seller = "Guangji Trading LLC",
            Rating = 4.7m,
            ReviewCount = 32,
            SoldCount = 154,
            StockQuantity = 100
        };
    }
}