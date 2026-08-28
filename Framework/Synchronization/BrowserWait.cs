using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UiTests.Framework.Synchronization;

/// <summary>
/// Explicit synchronization primitives for application-observable browser state.
/// The class intentionally does not wrap normal WebDriver actions; it only owns
/// bounded waits so page objects remain readable and implicit waits stay disabled.
/// </summary>
public sealed class BrowserWait
{
    private readonly WebDriverWait wait;

    public BrowserWait(IWebDriver driver, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(driver);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");

        wait = new WebDriverWait(driver, timeout)
        {
            PollingInterval = TimeSpan.FromMilliseconds(150)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    public IWebElement UntilVisible(By locator) => wait.Until(driver =>
    {
        var element = driver.FindElement(locator);
        return element.Displayed ? element : null;
    })!;

    public IWebElement UntilClickable(By locator) => wait.Until(driver =>
    {
        var element = driver.FindElement(locator);
        return element.Displayed && element.Enabled ? element : null;
    })!;

    public void UntilDocumentReady()
    {
        wait.Until(driver =>
            driver is IJavaScriptExecutor javascript &&
            string.Equals(
                javascript.ExecuteScript("return document.readyState")?.ToString(),
                "complete",
                StringComparison.OrdinalIgnoreCase));
    }

    public void UntilUrlStartsWith(Uri expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        wait.Until(driver =>
            driver.Url.StartsWith(expected.AbsoluteUri, StringComparison.OrdinalIgnoreCase));
    }
}
