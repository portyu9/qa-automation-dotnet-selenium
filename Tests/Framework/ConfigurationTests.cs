using UiTests.Framework.Configuration;
using Xunit;

namespace UiTests.Tests.Framework;

public class ConfigurationTests
{
    [Fact]
    public void DefaultsAreValidAndSafeForCi()
    {
        var settings = TestSettings.FromEnvironment(_ => null);

        Assert.Equal(new Uri("http://127.0.0.1:3200"), settings.BaseUrl);
        Assert.Equal("chrome", settings.Browser);
        Assert.True(settings.Headless);
        Assert.True(settings.ExplicitWait > TimeSpan.Zero);
        Assert.True(settings.PageLoadTimeout > TimeSpan.Zero);
        Assert.False(string.IsNullOrWhiteSpace(settings.RunId));
    }

    [Fact]
    public void ExplicitValuesAreParsedWithoutMutatingProcessEnvironment()
    {
        var values = new Dictionary<string, string?>
        {
            ["TEST_BASE_URL"] = "https://example.test/app",
            ["TEST_BROWSER"] = "firefox",
            ["TEST_HEADLESS"] = "false",
            ["TEST_EXPLICIT_WAIT_SECONDS"] = "7",
            ["TEST_PAGE_LOAD_TIMEOUT_SECONDS"] = "21",
            ["SELENIUM_GRID_URL"] = "https://grid.example.test/wd/hub",
            ["TEST_RUN_ID"] = "contract-run:firefox"
        };

        var settings = TestSettings.FromEnvironment(
            name => values.TryGetValue(name, out var value) ? value : null);

        Assert.Equal(new Uri("https://example.test/app"), settings.BaseUrl);
        Assert.Equal("firefox", settings.Browser);
        Assert.False(settings.Headless);
        Assert.Equal(TimeSpan.FromSeconds(7), settings.ExplicitWait);
        Assert.Equal(TimeSpan.FromSeconds(21), settings.PageLoadTimeout);
        Assert.Equal(new Uri("https://grid.example.test/wd/hub"), settings.GridUrl);
        Assert.Equal("contract-run:firefox", settings.RunId);
    }

    [Fact]
    public void InvalidBrowserFailsBeforeDriverCreation()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestSettings.FromEnvironment(
                name => name == "TEST_BROWSER" ? "unsupported-browser" : null));
    }

    [Theory]
    [InlineData("TEST_BASE_URL", "https://user:password@example.test")]
    [InlineData("TEST_BASE_URL", "https://example.test/app?access_token=secret")]
    [InlineData("TEST_BASE_URL", "https://example.test/app#fragment")]
    [InlineData("TEST_BASE_URL", "https://example.test:0/app")]
    [InlineData("SELENIUM_GRID_URL", "https://grid.example.test/wd/hub?token=secret")]
    [InlineData("SELENIUM_GRID_URL", "https://grid.example.test:0/wd/hub")]
    public void UnsafeFrameworkUrlsFailBeforeDriverCreation(string name, string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestSettings.FromEnvironment(variable => variable == name ? value : null));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("run/child")]
    [InlineData("run\\child")]
    [InlineData("contains space")]
    public void UnsafeRunIdentifiersFailBeforeDriverCreation(string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestSettings.FromEnvironment(
                name => name == "TEST_RUN_ID" ? value : null));
    }
}