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
}