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
    private const string DefaultFixtureUrl = "http://127.0.0.1:3200";

    private static readonly HashSet<string> SupportedBrowsers =
        new(StringComparer.OrdinalIgnoreCase) { "chrome", "firefox", "edge" };

    public static TestSettings FromEnvironment() =>
        FromEnvironment(Environment.GetEnvironmentVariable);

    internal static TestSettings FromEnvironment(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        var baseUrl = ReadAbsoluteUri(readVariable, "TEST_BASE_URL", DefaultFixtureUrl);
        var browser = readVariable("TEST_BROWSER")?.Trim() ?? "chrome";
        if (!SupportedBrowsers.Contains(browser))
        {
            throw new InvalidOperationException(
                $"TEST_BROWSER must be one of {string.Join(", ", SupportedBrowsers)}, got '{browser}'.");
        }

        var gridRaw = readVariable("SELENIUM_GRID_URL");
        Uri? gridUrl = null;
        if (!string.IsNullOrWhiteSpace(gridRaw))
        {
            if (!Uri.TryCreate(gridRaw, UriKind.Absolute, out gridUrl) || !IsSafeHttpUri(gridUrl))
            {
                throw new InvalidOperationException(
                    "SELENIUM_GRID_URL must be an absolute http(s) URL without credentials, query, or fragment.");
            }
        }

        return new TestSettings(
            baseUrl,
            browser.ToLowerInvariant(),
            ReadBoolean(readVariable, "TEST_HEADLESS", true),
            TimeSpan.FromSeconds(ReadPositiveInt(readVariable, "TEST_EXPLICIT_WAIT_SECONDS", 10)),
            TimeSpan.FromSeconds(ReadPositiveInt(readVariable, "TEST_PAGE_LOAD_TIMEOUT_SECONDS", 30)),
            gridUrl,
            ReadRunId(readVariable));
    }

    private static Uri ReadAbsoluteUri(
        Func<string, string?> readVariable,
        string name,
        string fallback)
    {
        var raw = readVariable(name) ?? fallback;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || !IsSafeHttpUri(uri))
        {
            throw new InvalidOperationException(
                $"{name} must be an absolute http(s) URL without credentials, query, or fragment.");
        }
        return uri;
    }

    private static bool IsSafeHttpUri(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(uri.Host) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment);

    private static int ReadPositiveInt(
        Func<string, string?> readVariable,
        string name,
        int fallback)
    {
        var raw = readVariable(name);
        if (raw is null) return fallback;
        if (!int.TryParse(raw, out var value) || value <= 0)
            throw new InvalidOperationException($"{name} must be a positive integer.");
        return value;
    }

    private static bool ReadBoolean(
        Func<string, string?> readVariable,
        string name,
        bool fallback)
    {
        var raw = readVariable(name);
        if (raw is null) return fallback;
        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => throw new InvalidOperationException($"{name} must be a boolean value.")
        };
    }

    private static string ReadRunId(Func<string, string?> readVariable)
    {
        var value = readVariable("TEST_RUN_ID")?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return Guid.NewGuid().ToString("n");
        }

        if (value.Length > 128 ||
            !value.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':'))
        {
            throw new InvalidOperationException(
                "TEST_RUN_ID must be 1-128 ASCII letters, digits, dots, underscores, colons, or hyphens.");
        }

        return value;
    }
}