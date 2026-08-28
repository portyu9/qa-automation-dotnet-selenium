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
}
