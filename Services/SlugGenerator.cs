using System.Text.RegularExpressions;

namespace SecureMarketMvc.Services;

public static partial class SlugGenerator
{
    public static string Generate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Guid.NewGuid().ToString("N");
        }

        var slug = value.Trim().ToLowerInvariant();
        slug = NonLetterOrDigit().Replace(slug, "-");
        slug = MultipleDash().Replace(slug, "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonLetterOrDigit();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleDash();
}
