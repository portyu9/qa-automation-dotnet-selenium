using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using UiTests.PageObjects;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit;

namespace UiTests.Tests
{
    /// <summary>
    /// End‑to‑end tests for the login flow using Sauce Demo.  Each test spins up
    /// its own ChromeDriver instance.  The driver is closed in the Dispose
    /// method.  The test runs Chrome in headless mode to be suitable for CI.  When
    /// debugging locally you can comment out the headless option to watch the
    /// browser interact with the site.
    /// </summary>
    public class LoginTests : IDisposable
    {
        private readonly IWebDriver driver;
        private readonly LoginPage loginPage;
        private readonly HomePage homePage;

        public LoginTests()
        {
            // Ensure the appropriate driver binary is downloaded
            new DriverManager().SetUpDriver(new ChromeConfig());

            var options = new ChromeOptions();
            // Use the new headless mode which replicates full Chrome features
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");

            driver = new ChromeDriver(options);
            // Set an implicit wait to handle element lookups more gracefully
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            loginPage = new LoginPage(driver);
            homePage = new HomePage(driver);
        }

        [Fact(DisplayName = "Standard user should be able to login successfully")]
        public void StandardUserLoginShouldSucceed()
        {
            // Navigate to login page and authenticate
            loginPage.Navigate();
            loginPage.Login("standard_user", "secret_sauce");

            // Assert we are redirected to the inventory page by checking the page load condition
            Assert.True(homePage.IsLoaded);
            Assert.Equal(homePage.PageUrl, driver.Url);
        }

        public void Dispose()
        {
            driver.Quit();
        }
    }
}
