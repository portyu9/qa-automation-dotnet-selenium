using OpenQA.Selenium;
using UiTests.Framework.Configuration;
using UiTests.Framework.Synchronization;

namespace UiTests.PageObjects;

/// <summary>
/// Base page boundary. Page objects expose application intent while navigation
/// and synchronization remain explicit and bounded.
/// </summary>
public abstract class BasePage
{
    private readonly TestSettings settings;

    protected BasePage(IWebDriver driver, TestSettings settings)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Wait = new BrowserWait(driver, settings.ExplicitWait);
    }

    protected IWebDriver Driver { get; }
    protected BrowserWait Wait { get; }

    protected abstract string RelativePath { get; }

    public string PageUrl => new Uri(settings.BaseUrl, RelativePath).AbsoluteUri;

    public void Navigate()
    {
        Driver.Navigate().GoToUrl(PageUrl);
        Wait.UntilDocumentReady();
    }
}
