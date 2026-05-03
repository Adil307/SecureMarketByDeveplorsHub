using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SecureMarketMvc.Models;
using SecureMarketMvc.Services;
using SecureMarketMvc.ViewModels;

namespace SecureMarketMvc.Controllers;

public sealed class AuthController : Controller
{
    private const string DevAdminEmail = "admin@securemarket.local";
    private const string DevAdminPassword = "Admin@12345";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICartService _cartService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ICartService cartService,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _cartService = cartService;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet("/auth/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // TEMP DEVELOPMENT ADMIN LOGIN
    // Open: https://localhost:7044/auth/dev-admin
    [AllowAnonymous]
    [HttpGet("/auth/dev-admin")]
    public async Task<IActionResult> DevAdminLogin(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var admin = await EnsureAdminUserAsync();

        await _signInManager.SignOutAsync();
        await _signInManager.SignInAsync(admin, isPersistent: true);
        await _cartService.MergeAnonymousCartIntoUserAsync(admin.Id, cancellationToken);

        return RedirectToAction("Index", "AdminProducts");
    }

    [AllowAnonymous]
    [HttpPost("/auth/login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var password = model.Password.Trim();

        // Admin login shortcut
        if (string.Equals(email, DevAdminEmail, StringComparison.OrdinalIgnoreCase)
            && password == DevAdminPassword)
        {
            var admin = await EnsureAdminUserAsync();

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(admin, model.RememberMe);
            await _cartService.MergeAnonymousCartIntoUserAsync(admin.Id, cancellationToken);

            return RedirectToAction("Index", "AdminProducts");
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            await Task.Delay(Random.Shared.Next(120, 300), cancellationToken);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        await _userManager.UpdateAsync(user);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        var result = await _signInManager.PasswordSignInAsync(
            user,
            password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        await _cartService.MergeAnonymousCartIntoUserAsync(user.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task<AppUser> EnsureAdminUserAsync()
    {
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create Admin role: " +
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await _roleManager.RoleExistsAsync("Customer"))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole("Customer"));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create Customer role: " +
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var admin = await _userManager.FindByEmailAsync(DevAdminEmail);

        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = DevAdminEmail,
                Email = DevAdminEmail,
                EmailConfirmed = true,
                FullName = "Secure Market Admin",
                LockoutEnabled = false,
                LockoutEnd = null,
                AccessFailedCount = 0
            };

            var createResult = await _userManager.CreateAsync(admin, DevAdminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create admin user: " +
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            admin.UserName = DevAdminEmail;
            admin.Email = DevAdminEmail;
            admin.EmailConfirmed = true;
            admin.FullName = string.IsNullOrWhiteSpace(admin.FullName)
                ? "Secure Market Admin"
                : admin.FullName;
            admin.LockoutEnabled = false;
            admin.LockoutEnd = null;
            admin.AccessFailedCount = 0;

            var updateResult = await _userManager.UpdateAsync(admin);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not update admin user: " +
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }

            var passwordOk = await _userManager.CheckPasswordAsync(admin, DevAdminPassword);

            if (!passwordOk)
            {
                if (await _userManager.HasPasswordAsync(admin))
                {
                    var removeResult = await _userManager.RemovePasswordAsync(admin);

                    if (!removeResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Could not remove old admin password: " +
                            string.Join("; ", removeResult.Errors.Select(e => e.Description)));
                    }
                }

                var addResult = await _userManager.AddPasswordAsync(admin, DevAdminPassword);

                if (!addResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not set admin password: " +
                        string.Join("; ", addResult.Errors.Select(e => e.Description)));
                }
            }
        }

        await _userManager.ResetAccessFailedCountAsync(admin);
        await _userManager.SetLockoutEndDateAsync(admin, null);

        if (!await _userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await _userManager.AddToRoleAsync(admin, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not add admin to Admin role: " +
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        return admin;
    }

    [AllowAnonymous]
    [HttpGet("/auth/register")]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/auth/register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.Email.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync("Customer"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _cartService.MergeAnonymousCartIntoUserAsync(user.Id, cancellationToken);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/auth/denied")]
    public IActionResult Denied()
    {
        return View();
    }
}