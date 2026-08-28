using UiTests.Framework.Execution;
using UiTests.PageObjects;
using Xunit;

namespace UiTests.Tests;

/// <summary>
/// Browser-level authentication contract. Each xUnit test instance owns one
/// isolated browser session created through the validated framework factory.
/// </summary>
public sealed class LoginTests : IDisposable
{
    private readonly BrowserTestSession session;
    private readonly LoginPage loginPage;
    private readonly HomePage homePage;

    public LoginTests()
    {
        session = new BrowserTestSession();
        loginPage = new LoginPage(session.Driver, session.Settings);
        homePage = new HomePage(session.Driver, session.Settings);
    }

    [Fact(DisplayName = "Standard user can authenticate and reach inventory")]
    public void StandardUserLoginShouldSucceed()
    {
        session.Run(nameof(StandardUserLoginShouldSucceed), () =>
        {
            loginPage.Navigate();
            loginPage.Login("standard_user", "secret_sauce");

            Assert.True(homePage.IsLoaded);
            Assert.True(
                session.Driver.Url.StartsWith(homePage.PageUrl, StringComparison.OrdinalIgnoreCase),
                $"Expected URL to start with '{homePage.PageUrl}' but was '{session.Driver.Url}'.");
        });
    }

    public void Dispose() => session.Dispose();
}
