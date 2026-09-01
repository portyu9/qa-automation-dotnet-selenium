using OpenQA.Selenium;
using UiTests.Framework.Configuration;
using UiTests.Framework.Diagnostics;
using UiTests.Framework.Drivers;

namespace UiTests.Framework.Execution;

/// <summary>
/// Owns one isolated WebDriver session for one xUnit test instance and captures
/// browser evidence before cleanup whenever the test body throws.
/// </summary>
public sealed class BrowserTestSession : IDisposable
{
    private bool disposed;
    private bool causalTestFailure;

    public BrowserTestSession(TestSettings? settings = null)
    {
        Settings = settings ?? TestSettings.FromEnvironment();
        Driver = WebDriverFactory.Create(Settings);
    }

    public TestSettings Settings { get; }
    public IWebDriver Driver { get; }

    public void Run(string testName, Action testBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        ArgumentNullException.ThrowIfNull(testBody);

        try
        {
            testBody();
        }
        catch
        {
            causalTestFailure = true;
            try
            {
                ArtifactCollector.Capture(Driver, testName, Settings.RunId);
            }
            catch (Exception artifactError)
            {
                Console.Error.WriteLine(
                    $"[artifact-capture:{Settings.RunId}] {artifactError.GetType().Name}");
            }
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        var cleanupFailures = new List<Exception>(capacity: 2);
        try
        {
            Driver.Quit();
        }
        catch (Exception error)
        {
            cleanupFailures.Add(error);
            Console.Error.WriteLine(
                $"[driver-quit:{Settings.RunId}] {error.GetType().Name}");
        }

        try
        {
            Driver.Dispose();
        }
        catch (Exception error)
        {
            cleanupFailures.Add(error);
            Console.Error.WriteLine(
                $"[driver-dispose:{Settings.RunId}] {error.GetType().Name}");
        }

        if (!causalTestFailure && cleanupFailures.Count > 0)
        {
            throw new AggregateException("WebDriver cleanup failed after an otherwise successful test.", cleanupFailures);
        }
    }
}
