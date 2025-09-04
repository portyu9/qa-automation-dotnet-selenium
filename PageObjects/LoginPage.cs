using OpenQA.Selenium;

namespace UiTests.PageObjects
{
    /// <summary>
    /// Page object representing the login page of Sauce Demo.  It exposes element
    /// properties for username, password and the login button along with a
    /// convenience method to perform the login action.
    /// </summary>
    public class LoginPage : BasePage
    {
        public LoginPage(IWebDriver driver) : base(driver) { }

        public override string PageUrl => "https://www.saucedemo.com/";

        private IWebElement UsernameField => driver.FindElement(By.Id("user-name"));
        private IWebElement PasswordField => driver.FindElement(By.Id("password"));
        private IWebElement LoginButton => driver.FindElement(By.Id("login-button"));

        /// <summary>
        /// Perform a login by filling the username and password fields and clicking
        /// the login button.
        /// </summary>
        /// <param name="username">The user name to enter</param>
        /// <param name="password">The password to enter</param>
        public void Login(string username, string password)
        {
            UsernameField.Clear();
            UsernameField.SendKeys(username);
            PasswordField.Clear();
            PasswordField.SendKeys(password);
            LoginButton.Click();
        }
    }
}
