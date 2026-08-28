using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using UiTests.Framework.Configuration;

namespace UiTests.Framework.Drivers;

public static class WebDriverFactory
{
    public static IWebDriver Create(TestSettings settings)
    {
        DriverOptions options = settings.Browser switch
        {
            "chrome" => ChromeOptions(settings.Headless),
            "firefox" => FirefoxOptions(settings.Headless),
            "edge" => EdgeOptions(settings.Headless),
            _ => throw new ArgumentOutOfRangeException(nameof(settings.Browser))
        };

        IWebDriver driver = settings.GridUrl is null
            ? CreateLocal(settings.Browser, options)
            : new RemoteWebDriver(settings.GridUrl, options);

        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        driver.Manage().Timeouts().PageLoad = settings.PageLoadTimeout;
        driver.Manage().Window.Size = new System.Drawing.Size(1440, 1000);
        return driver;
    }

    private static IWebDriver CreateLocal(string browser, DriverOptions options) => browser switch
    {
        "chrome" => new ChromeDriver((ChromeOptions)options),
        "firefox" => new FirefoxDriver((FirefoxOptions)options),
        "edge" => new EdgeDriver((EdgeOptions)options),
        _ => throw new ArgumentOutOfRangeException(nameof(browser))
    };

    private static ChromeOptions ChromeOptions(bool headless)
    {
        var options = new ChromeOptions();
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--window-size=1440,1000");
        if (headless) options.AddArgument("--headless=new");
        return options;
    }

    private static FirefoxOptions FirefoxOptions(bool headless)
    {
        var options = new FirefoxOptions();
        if (headless) options.AddArgument("-headless");
        return options;
    }

    private static EdgeOptions EdgeOptions(bool headless)
    {
        var options = new EdgeOptions();
        if (headless) options.AddArgument("--headless=new");
        return options;
    }
}
