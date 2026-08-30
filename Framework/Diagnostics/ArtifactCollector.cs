using OpenQA.Selenium;

namespace UiTests.Framework.Diagnostics;

public static class ArtifactCollector
{
    public static string Capture(
        IWebDriver driver,
        string testName,
        string runId,
        string root = "artifacts",
        bool includePageSource = false)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var directory = ResolveDirectory(root, runId, testName);
        Directory.CreateDirectory(directory);

        File.WriteAllText(
            Path.Combine(directory, "url.txt"),
            SanitizeUrl(driver.Url ?? string.Empty));

        if (driver is ITakesScreenshot screenshots)
        {
            screenshots.GetScreenshot().SaveAsFile(Path.Combine(directory, "failure.png"));
        }

        if (includePageSource)
        {
            File.WriteAllText(Path.Combine(directory, "page-source.html"), driver.PageSource);
        }

        return directory;
    }

    internal static string ResolveDirectory(string root, string runId, string testName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);

        var rootPath = Path.GetFullPath(root);
        var directory = Path.GetFullPath(Path.Combine(
            rootPath,
            SanitizePathSegment(runId),
            SanitizePathSegment(testName)));
        var relative = Path.GetRelativePath(rootPath, directory);

        if (relative == ".." ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("artifact path must remain inside the configured root");
        }

        return directory;
    }

    internal static string SanitizeUrl(string value)
    {
        if (string.Equals(value, "about:blank", StringComparison.OrdinalIgnoreCase))
        {
            return "about:blank";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return "<invalid-url>";
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return $"{uri.Scheme}:<redacted>";
        }

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri.AbsoluteUri;
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch =>
            invalid.Contains(ch) || ch is '/' or '\\' ? '_' : ch).ToArray()).Trim();

        return sanitized is "" or "." or ".." ? "_" : sanitized;
    }
}