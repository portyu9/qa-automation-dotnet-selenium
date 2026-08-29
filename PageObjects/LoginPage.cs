using OpenQA.Selenium;
using UiTests.Framework.Configuration;

namespace UiTests.PageObjects;

/// <summary>
/// Configured authentication page. Locators remain local to the page and actions
/// wait for application-observable readiness rather than relying on implicit timing.
/// </summary>
public sealed class LoginPage : BasePage
{
    private static readonly By Username = By.Id("user-name");
    private static readonly By Password = By.Id("password");
    private static readonly By Submit = By.Id("login-button");
    private static readonly By Error = By.Id("login-error");

    public LoginPage(IWebDriver driver, TestSettings settings) : base(driver, settings) { }

    protected override string RelativePath => "/";

    public string ErrorMessage => Wait.UntilVisible(Error).Text;

    public void Login(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(password);

        var usernameField = Wait.UntilVisible(Username);
        usernameField.Clear();
        usernameField.SendKeys(username);

        var passwordField = Wait.UntilVisible(Password);
        passwordField.Clear();
        passwordField.SendKeys(password);

        Wait.UntilClickable(Submit).Click();
    }
}
