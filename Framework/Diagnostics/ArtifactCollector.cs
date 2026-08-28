using OpenQA.Selenium;

namespace UiTests.Framework.Diagnostics;

public static class ArtifactCollector
{
    public static string Capture(IWebDriver driver, string testName, string runId, string root = "artifacts")
    {
        var safeName = Sanitize(testName);
        var directory = Path.Combine(root, runId, safeName);
        Directory.CreateDirectory(directory);

        File.WriteAllText(Path.Combine(directory, "page-source.html"), driver.PageSource);
        File.WriteAllText(Path.Combine(directory, "url.txt"), driver.Url ?? string.Empty);

        if (driver is ITakesScreenshot screenshots)
        {
            screenshots.GetScreenshot().SaveAsFile(Path.Combine(directory, "failure.png"));
        }

        return directory;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
