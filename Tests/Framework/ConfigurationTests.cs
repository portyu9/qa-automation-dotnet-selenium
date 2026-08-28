using UiTests.Framework.Configuration;
using Xunit;

namespace UiTests.Tests.Framework;

public class ConfigurationTests
{
    [Fact]
    public void DefaultsAreValidAndSafeForCi()
    {
        var settings = TestSettings.FromEnvironment();

        Assert.True(settings.BaseUrl.IsAbsoluteUri);
        Assert.Contains(settings.Browser, new[] { "chrome", "firefox", "edge" });
        Assert.True(settings.ExplicitWait > TimeSpan.Zero);
        Assert.True(settings.PageLoadTimeout > TimeSpan.Zero);
        Assert.False(string.IsNullOrWhiteSpace(settings.RunId));
    }

    [Fact]
    public void InvalidBrowserFailsBeforeDriverCreation()
    {
        var original = Environment.GetEnvironmentVariable("TEST_BROWSER");
        try
        {
            Environment.SetEnvironmentVariable("TEST_BROWSER", "unsupported-browser");
            Assert.Throws<InvalidOperationException>(() => TestSettings.FromEnvironment());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_BROWSER", original);
        }
    }
}
