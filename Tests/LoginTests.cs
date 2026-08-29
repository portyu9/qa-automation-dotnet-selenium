using UiTests.Framework.Configuration;
using UiTests.Framework.Execution;
using UiTests.Framework.Testing;
using UiTests.PageObjects;
using UiTests.Tests.Fixtures;
using Xunit;

namespace UiTests.Tests;

/// <summary>
/// Browser-level authentication contract. Each xUnit test instance owns one
/// isolated browser session created through the validated framework factory.
/// </summary>
[Collection(LocalUiCollection.Name)]
public sealed class LoginTests : IDisposable
{
    private readonly BrowserTestSession session;
    private readonly LoginPage loginPage;
    private readonly HomePage homePage;

    public LoginTests(LocalUiServer localUi)
    {
        ArgumentNullException.ThrowIfNull(localUi);

        var settings = TestSettings.FromEnvironment();
        if (settings.BaseUrl == LocalUiServer.DefaultBaseUrl && localUi.BaseUrl != settings.BaseUrl)
        {
            throw new InvalidOperationException("Local UI fixture and default TEST_BASE_URL are inconsistent.");
        }

        session = new BrowserTestSession(settings);
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

    [Fact(DisplayName = "Invalid credentials remain on authentication page with a stable error")]
    public void InvalidCredentialsShouldBeRejected()
    {
        session.Run(nameof(InvalidCredentialsShouldBeRejected), () =>
        {
            loginPage.Navigate();
            loginPage.Login("invalid_user", "incorrect_password");

            Assert.Equal("Invalid username or password", loginPage.ErrorMessage);
            Assert.Equal("/", new Uri(session.Driver.Url).AbsolutePath);
        });
    }

    public void Dispose() => session.Dispose();
}
