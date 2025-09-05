using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using UiTests.PageObjects;
using Newtonsoft.Json;
using Xunit;


namespace UiTests.Tests
{
    /// <summary>
    /// End-to-end tests for the login flow using Sauce Demo. Each test spins up its own ChromeDriver instance.
    /// The test runs Chrome in headless mode to be suitable for the GitHub CI environment. When debugging
    /// locally you can comment out the headless option to watch the browser interact with the site.
    /// </summary>
    public class LoginTests : IDisposable
    {
        private readonly IWebDriver driver;
        private readonly LoginPage loginPage;
        private readonly HomePage homePage;

        public LoginTests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
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

        JsonConvert.SerializeObject(new { User = "standard_user" });

            // Assert we are redirected to the inventory page by checking the page load condition
            Assert.True(homePage.IsLoaded);
            Assert.StartsWith(homePage.PageUrl, driver.Url);
        }

        public void Dispose()
        {
            driver.Quit();
        }
    }
}
