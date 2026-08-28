namespace UiTests.Framework.Configuration;

public sealed record TestSettings(
    Uri BaseUrl,
    string Browser,
    bool Headless,
    TimeSpan ExplicitWait,
    TimeSpan PageLoadTimeout,
    Uri? GridUrl,
    string RunId)
{
    private static readonly HashSet<string> SupportedBrowsers =
        new(StringComparer.OrdinalIgnoreCase) { "chrome", "firefox", "edge" };

    public static TestSettings FromEnvironment()
    {
        var baseUrl = ReadAbsoluteUri("TEST_BASE_URL", "https://www.saucedemo.com");
        var browser = Environment.GetEnvironmentVariable("TEST_BROWSER")?.Trim() ?? "chrome";
        if (!SupportedBrowsers.Contains(browser))
        {
            throw new InvalidOperationException(
                $"TEST_BROWSER must be one of {string.Join(", ", SupportedBrowsers)}, got '{browser}'.");
        }

        var gridRaw = Environment.GetEnvironmentVariable("SELENIUM_GRID_URL");
        Uri? gridUrl = null;
        if (!string.IsNullOrWhiteSpace(gridRaw))
        {
            if (!Uri.TryCreate(gridRaw, UriKind.Absolute, out gridUrl) ||
                (gridUrl.Scheme != Uri.UriSchemeHttp && gridUrl.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("SELENIUM_GRID_URL must be an absolute http(s) URL.");
            }
        }

        return new TestSettings(
            baseUrl,
            browser.ToLowerInvariant(),
            ReadBoolean("TEST_HEADLESS", true),
            TimeSpan.FromSeconds(ReadPositiveInt("TEST_EXPLICIT_WAIT_SECONDS", 10)),
            TimeSpan.FromSeconds(ReadPositiveInt("TEST_PAGE_LOAD_TIMEOUT_SECONDS", 30)),
            gridUrl,
            Environment.GetEnvironmentVariable("TEST_RUN_ID")?.Trim() is { Length: > 0 } runId
                ? runId
                : Guid.NewGuid().ToString("n"));
    }

    private static Uri ReadAbsoluteUri(string name, string fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name) ?? fallback;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"{name} must be an absolute http(s) URL.");
        }
        return uri;
    }

    private static int ReadPositiveInt(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (raw is null) return fallback;
        if (!int.TryParse(raw, out var value) || value <= 0)
            throw new InvalidOperationException($"{name} must be a positive integer.");
        return value;
    }

    private static bool ReadBoolean(string name, bool fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (raw is null) return fallback;
        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => throw new InvalidOperationException($"{name} must be a boolean value.")
        };
    }
}
