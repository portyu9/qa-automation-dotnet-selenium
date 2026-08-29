using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UiTests.Framework.Execution;

/// <summary>
/// Owns a newly opened browser window and guarantees restoration of the
/// originating window when the scope is disposed.
/// </summary>
public sealed class BrowserWindowScope : IDisposable
{
    private readonly IWebDriver driver;
    private readonly string originalHandle;
    private readonly string openedHandle;
    private bool disposed;

    private BrowserWindowScope(IWebDriver driver, string originalHandle, string openedHandle)
    {
        this.driver = driver;
        this.originalHandle = originalHandle;
        this.openedHandle = openedHandle;
    }

    public string OpenedHandle => openedHandle;

    public static BrowserWindowScope Open(IWebDriver driver, Action trigger, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(trigger);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        var original = driver.CurrentWindowHandle;
        var before = driver.WindowHandles.ToHashSet(StringComparer.Ordinal);

        trigger();

        var wait = new WebDriverWait(driver, timeout)
        {
            PollingInterval = TimeSpan.FromMilliseconds(100)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchWindowException));

        var opened = wait.Until(current =>
            current.WindowHandles.FirstOrDefault(handle => !before.Contains(handle)));
        if (string.IsNullOrWhiteSpace(opened))
        {
            throw new WebDriverTimeoutException("Expected a new browser window but none appeared before the deadline.");
        }

        driver.SwitchTo().Window(opened);
        return new BrowserWindowScope(driver, original, opened);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        try
        {
            if (driver.WindowHandles.Contains(openedHandle, StringComparer.Ordinal))
            {
                driver.SwitchTo().Window(openedHandle);
                driver.Close();
            }
        }
        finally
        {
            if (driver.WindowHandles.Contains(originalHandle, StringComparer.Ordinal))
            {
                driver.SwitchTo().Window(originalHandle);
            }
        }
    }
}
