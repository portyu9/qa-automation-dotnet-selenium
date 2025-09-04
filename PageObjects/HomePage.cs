using OpenQA.Selenium;

namespace UiTests.PageObjects
{
    /// <summary>
    /// Represents the inventory (home) page that appears after a successful login.
    /// It exposes a simple IsLoaded property to verify that the inventory container
    /// is displayed.
    /// </summary>
    public class HomePage : BasePage
    {
        public HomePage(IWebDriver driver) : base(driver) { }

        public override string PageUrl => "https://www.saucedemo.com/inventory.html";

        private IWebElement InventoryContainer => driver.FindElement(By.Id("inventory_container"));

        /// <summary>
        /// Returns true when the inventory container is visible.  The call to
        /// IWebElement.Displayed will implicitly wait for a short time if a
        /// default implicit wait is configured on the driver.
        /// </summary>
        public bool IsLoaded => InventoryContainer.Displayed;
    }
}
