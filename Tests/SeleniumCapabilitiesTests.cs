using OpenQA.Selenium;
using UiTests.Framework.Configuration;
using UiTests.Framework.Execution;
using UiTests.Framework.Testing;
using UiTests.Tests.Fixtures;
using Xunit;

namespace UiTests.Tests;

[Collection(LocalUiCollection.Name)]
public sealed class SeleniumCapabilitiesTests : IDisposable
{
    private readonly BrowserTestSession session;
    private readonly LocalUiServer localUi;

    public SeleniumCapabilitiesTests(LocalUiServer localUi)
    {
        ArgumentNullException.ThrowIfNull(localUi);
        this.localUi = localUi;

        var settings = TestSettings.FromEnvironment();
        if (settings.BaseUrl == LocalUiServer.DefaultBaseUrl && localUi.BaseUrl != settings.BaseUrl)
        {
            throw new InvalidOperationException("Local UI fixture and default TEST_BASE_URL are inconsistent.");
        }

        session = new BrowserTestSession(settings);
    }

    [Fact(DisplayName = "JavaScript, cookies, alerts, and frames preserve explicit browser context")]
    public void BrowserContextPrimitivesShouldRemainDeterministic()
    {
        session.Run(nameof(BrowserContextPrimitivesShouldRemainDeterministic), () =>
        {
            session.Driver.Navigate().GoToUrl(new Uri(localUi.BaseUrl, "interactions.html"));

            var script = Assert.IsAssignableFrom<IJavaScriptExecutor>(session.Driver);
            Assert.Equal("Browser Capability Surface", script.ExecuteScript("return document.title")?.ToString());

            session.Driver.Manage().Cookies.AddCookie(new Cookie("capability-mode", "enabled"));
            session.Driver.Navigate().Refresh();
            Assert.Contains("capability-mode=enabled", script.ExecuteScript("return document.cookie")?.ToString());

            session.Driver.SwitchTo().Frame(session.Driver.FindElement(By.Id("details-frame")));
            Assert.Equal("frame-ready", session.Driver.FindElement(By.Id("frame-value")).Text);
            session.Driver.SwitchTo().DefaultContent();

            session.Driver.FindElement(By.Id("open-alert")).Click();
            var alert = session.Driver.SwitchTo().Alert();
            Assert.Equal("fixture-alert", alert.Text);
            alert.Accept();

            Assert.Equal("Browser Capability Surface", session.Driver.FindElement(By.Id("capability-title")).Text);
        });
    }

    [Fact(DisplayName = "New window scope closes the child and restores the originating handle")]
    public void WindowScopeShouldRestoreOriginalContext()
    {
        session.Run(nameof(WindowScopeShouldRestoreOriginalContext), () =>
        {
            session.Driver.Navigate().GoToUrl(new Uri(localUi.BaseUrl, "interactions.html"));
            var originalHandle = session.Driver.CurrentWindowHandle;

            using (var window = BrowserWindowScope.Open(
                       session.Driver,
                       () => session.Driver.FindElement(By.Id("open-popup")).Click(),
                       TimeSpan.FromSeconds(5)))
            {
                Assert.NotEqual(originalHandle, window.OpenedHandle);
                Assert.Equal(window.OpenedHandle, session.Driver.CurrentWindowHandle);
                Assert.EndsWith("/inventory.html", session.Driver.Url, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(2, session.Driver.WindowHandles.Count);
            }

            Assert.Equal(originalHandle, session.Driver.CurrentWindowHandle);
            Assert.Single(session.Driver.WindowHandles);
        });
    }

    public void Dispose() => session.Dispose();
}
