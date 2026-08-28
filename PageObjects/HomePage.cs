using OpenQA.Selenium;
using UiTests.Framework.Configuration;

namespace UiTests.PageObjects;

/// <summary>
/// Inventory page reached after successful authentication.
/// </summary>
public sealed class HomePage : BasePage
{
    private static readonly By InventoryContainer = By.Id("inventory_container");

    public HomePage(IWebDriver driver, TestSettings settings) : base(driver, settings) { }

    protected override string RelativePath => "/inventory.html";

    public bool IsLoaded
    {
        get
        {
            Wait.UntilUrlStartsWith(new Uri(PageUrl));
            return Wait.UntilVisible(InventoryContainer).Displayed;
        }
    }
}
