namespace SecureMarketMvc.Services;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            var connectSrc = _environment.IsDevelopment()
                ? "'self' ws: wss: https://api.stripe.com"
                : "'self' https://api.stripe.com";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                $"connect-src {connectSrc}; " +
                "script-src 'self'; " +
                "form-action 'self' https://checkout.stripe.com;";
        }

        await _next(context);
    }
}
