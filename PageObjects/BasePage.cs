using OpenQA.Selenium;

namespace UiTests.PageObjects
{
    /// <summary>
    /// Abstract base class for all pages.  It encapsulates the WebDriver instance
    /// and exposes a Navigate method.  Concrete pages override the PageUrl property
    /// to define where they live.
    /// </summary>
    public abstract class BasePage
    {
        protected readonly IWebDriver driver;

        protected BasePage(IWebDriver driver)
        {
            this.driver = driver;
        }

        /// <summary>
        /// Gets the absolute URL for this page.  Derived classes must override.
        /// </summary>
        public abstract string PageUrl { get; }

        /// <summary>
        /// Navigate the browser to the page's URL.
        /// </summary>
        public void Navigate()
        {
            driver.Navigate().GoToUrl(PageUrl);
        }
    }
}
