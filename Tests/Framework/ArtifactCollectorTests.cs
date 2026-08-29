using System.Collections.ObjectModel;
using OpenQA.Selenium;
using UiTests.Framework.Diagnostics;
using Xunit;

namespace UiTests.Tests.Framework;

public class ArtifactCollectorTests
{
    [Theory]
    [InlineData(
        "https://user:password@example.com/path?access_token=secret#fragment",
        "https://example.com/path")]
    [InlineData(
        "http://localhost:8080/api/items?token=secret",
        "http://localhost:8080/api/items")]
    [InlineData("about:blank", "about:blank")]
    public void SanitizeUrlRemovesSensitiveHttpUrlComponents(
        string raw,
        string expected)
    {
        Assert.Equal(expected, ArtifactCollector.SanitizeUrl(raw));
    }

    [Theory]
    [InlineData("../outside", "test")]
    [InlineData("..", "../outside")]
    [InlineData("run/child", "test/child")]
    [InlineData("run\\child", "test\\child")]
    public void ResolvedArtifactDirectoryNeverEscapesRoot(string runId, string testName)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"), "artifacts");
        var directory = ArtifactCollector.ResolveDirectory(root, runId, testName);
        var relative = Path.GetRelativePath(Path.GetFullPath(root), directory);

        Assert.False(Path.IsPathRooted(relative));
        Assert.NotEqual("..", relative);
        Assert.False(relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    [Fact]
    public void CaptureKeepsPageSourceOptInWhilePersistingOnlySanitizedUrlByDefault()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        var root = Path.Combine(sandbox, "artifacts");
        var driver = new StubWebDriver
        {
            Url = "https://user:password@example.com/path?access_token=secret#fragment",
            PageSource = "<html><body>synthetic-sensitive-fixture</body></html>"
        };

        try
        {
            var defaultDirectory = ArtifactCollector.Capture(
                driver,
                "default-evidence",
                "run-42",
                root);

            Assert.Equal(
                "https://example.com/path",
                File.ReadAllText(Path.Combine(defaultDirectory, "url.txt")));
            Assert.False(File.Exists(Path.Combine(defaultDirectory, "page-source.html")));

            var explicitDirectory = ArtifactCollector.Capture(
                driver,
                "explicit-page-source",
                "run-42",
                root,
                includePageSource: true);

            Assert.Equal(
                driver.PageSource,
                File.ReadAllText(Path.Combine(explicitDirectory, "page-source.html")));
        }
        finally
        {
            if (Directory.Exists(sandbox)) Directory.Delete(sandbox, recursive: true);
        }
    }

    [Fact]
    public void CaptureRejectsBlankArtifactIdentityBeforeFilesystemWrites()
    {
        var driver = new StubWebDriver();

        Assert.Throws<ArgumentException>(() =>
            ArtifactCollector.Capture(driver, " ", "run", "artifacts"));
        Assert.Throws<ArgumentException>(() =>
            ArtifactCollector.Capture(driver, "test", " ", "artifacts"));
        Assert.Throws<ArgumentException>(() =>
            ArtifactCollector.Capture(driver, "test", "run", " "));
    }

    private sealed class StubWebDriver : IWebDriver
    {
        public string Url { get; set; } = "about:blank";
        public string Title => "stub";
        public string PageSource { get; init; } = "<html></html>";
        public string CurrentWindowHandle => "stub-window";
        public ReadOnlyCollection<string> WindowHandles => new(["stub-window"]);

        public void Close() { }
        public void Dispose() { }
        public void Quit() { }
        public IOptions Manage() => throw new NotSupportedException();
        public INavigation Navigate() => throw new NotSupportedException();
        public ITargetLocator SwitchTo() => throw new NotSupportedException();
        public IWebElement FindElement(By by) => throw new NotSupportedException();
        public ReadOnlyCollection<IWebElement> FindElements(By by) => throw new NotSupportedException();
    }
}