using OpenQA.Selenium;

namespace UiTests.Framework.Diagnostics;

public static class ArtifactCollector
{
    public static string Capture(
        IWebDriver driver,
        string testName,
        string runId,
        string root = "artifacts")
    {
        var safeName = Sanitize(testName);
        var directory = Path.Combine(root, runId, safeName);
        Directory.CreateDirectory(directory);

        File.WriteAllText(Path.Combine(directory, "page-source.html"), driver.PageSource);
        File.WriteAllText(
            Path.Combine(directory, "url.txt"),
            SanitizeUrl(driver.Url ?? string.Empty));

        if (driver is ITakesScreenshot screenshots)
        {
            screenshots.GetScreenshot().SaveAsFile(Path.Combine(directory, "failure.png"));
        }

        return directory;
    }

    internal static string SanitizeUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return value;
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

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
