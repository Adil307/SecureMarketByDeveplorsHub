# SecureMarketMvc

ASP.NET Core MVC ecommerce marketplace starter that follows the supplied Figma/screenshots:
home page, catalog list/grid, product detail, cart, checkout, account auth, orders, admin product management, responsive mobile layout, Razor partials, layout, and code-first EF Core models.

## Tech

- ASP.NET Core MVC / Razor Views
- C#
- EF Core Code First
- SQLite for development
- ASP.NET Core Identity
- Server-side Stripe Checkout Session creation with webhook verification
- Pure local CSS/JS, no React/Angular/Vue

## Run

```bash
dotnet restore
dotnet run
```

Open the HTTPS URL from the console, usually:

```text
https://localhost:7044
```

The app creates the SQLite database automatically on first run using EF Core model classes.

## Optional admin user

Set an admin email and strong password using user secrets. Do not put real secrets in `appsettings.json`.

```bash
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin@12345Strong!"
dotnet run
```

Then log in and open:

```text
/adminproducts
```

## Stripe test payment setup

Use Stripe test mode keys only during development.

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_your_webhook_secret"
```

Local webhook test with Stripe CLI:

```bash
stripe listen --forward-to https://localhost:7044/payments/stripe/webhook
```

Copy the `whsec_...` value shown by Stripe CLI into user secrets.

## Code-first migrations, if your trainer requires migration files

The project runs with `EnsureCreated` for easy internship demo. For a formal code-first migration workflow, change `SeedData.InitializeAsync` from `EnsureCreatedAsync()` to `MigrateAsync()`, then run:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Security choices already included

- Identity password hashing, password policy, unique emails, account lockout.
- Global antiforgery validation for POST forms.
- HTTPS redirection and HSTS outside development.
- HttpOnly, Secure, SameSite cookies.
- Security headers: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.
- Server-side cart/order total calculation.
- Parameterized EF Core LINQ queries.
- Role-based admin product management.
- Hosted payment redirect; no card numbers touch the MVC server.
- Stripe webhook signature verification with HMAC and timestamp tolerance.

## Important production steps

- Use a real database such as SQL Server/PostgreSQL.
- Store production secrets in your hosting secret store or environment variables.
- Add email confirmation before allowing real customer accounts.
- Add rate limiting / WAF at the host level.
- Rotate Data Protection keys only with a planned user sign-out strategy.
- Keep .NET runtime and NuGet packages patched.
