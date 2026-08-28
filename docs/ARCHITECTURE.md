# Architecture

## Layering

The suite uses four primary boundaries:

1. **Tests** express user-visible behavior and assertions.
2. **Page objects/components** own locators, waits, and reusable browser interactions.
3. **Framework services** own configuration, driver lifecycle, diagnostics, and cross-cutting test infrastructure.
4. **Selenium/WebDriver** is an implementation detail behind the driver factory.

Tests should not instantiate `ChromeDriver` directly in new code. Use `WebDriverFactory` so local execution and Selenium Grid remain interchangeable.

## Configuration

`TestSettings` converts environment variables into immutable typed state. URL, browser, timeout, and boolean validation happen before browser creation. This avoids late failures caused by misspelled browser names or malformed Grid endpoints.

Secrets do not belong in settings files committed to source control. Authentication values should come from CI secrets or an external secret manager and should never be emitted by diagnostics.

## Driver management

Selenium Manager, included with Selenium, is the default local driver-management mechanism. No browser-driver binary package is pinned in the project. Remote execution is selected by `SELENIUM_GRID_URL` without changing test code.

Implicit waits are disabled. Explicit waits belong in page/component abstractions around observable conditions. Mixing implicit and explicit waits makes timeout behavior difficult to reason about.

## Parallelism

Parallel UI execution requires isolated browser instances and isolated test data. Do not share a static `IWebDriver`. A fixture may own one driver per test/class depending on the isolation model, but the fixture must dispose it deterministically.

If a system account cannot safely support concurrent tests, isolate that test collection rather than disabling parallelism globally.

## Diagnostics

`ArtifactCollector` captures screenshot, page source, and current URL into a run/test-specific directory. Capture should happen only on failure or explicit diagnostic demand to keep CI storage bounded.

Console/network telemetry may be added through WebDriver BiDi or DevTools integrations, but logs must redact tokens, cookies, credentials, and sensitive form values.
