# .NET / Selenium UI Test Framework

A .NET 8 UI automation framework built with Selenium WebDriver and xUnit. The repository separates browser configuration, driver creation, diagnostics, page abstractions, and test intent so the suite can execute consistently on developer machines, CI runners, or Selenium Grid.

## Core design

- **xUnit** provides test discovery and lifecycle integration.
- **Selenium WebDriver 4.47.0** provides browser automation; Selenium Manager resolves local browser drivers.
- **Page objects** contain locators and reusable user interactions.
- **`TestSettings`** validates environment configuration before side effects.
- **`WebDriverFactory`** centralizes Chrome, Firefox, Edge, headless, and Grid creation.
- **`ArtifactCollector`** standardizes failure screenshots/page source/URL evidence.
- **GitHub Actions** builds and executes tests with TRX and coverage artifacts.

## Structure

```text
.
├── Framework/
│   ├── Configuration/TestSettings.cs
│   ├── Drivers/WebDriverFactory.cs
│   └── Diagnostics/ArtifactCollector.cs
├── PageObjects/
├── Tests/
│   └── Framework/
├── docs/
├── .github/workflows/ci.yml
└── UiTests.csproj
```

## Configuration

| Variable | Meaning | Default |
| --- | --- | --- |
| `TEST_BASE_URL` | application base URL | `https://www.saucedemo.com` |
| `TEST_BROWSER` | `chrome`, `firefox`, or `edge` | `chrome` |
| `TEST_HEADLESS` | headless browser mode | `true` |
| `TEST_EXPLICIT_WAIT_SECONDS` | page/component wait budget | `10` |
| `TEST_PAGE_LOAD_TIMEOUT_SECONDS` | navigation timeout | `30` |
| `SELENIUM_GRID_URL` | optional remote WebDriver endpoint | unset |
| `TEST_RUN_ID` | artifact/correlation identifier | generated GUID |

Copy the values from `.env.example` into the executing shell or CI environment. Do not commit credentials.

## Local execution

Prerequisites: .NET 8 SDK and at least one supported browser.

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --logger "trx;LogFileName=tests.trx"
```

Run against Firefox:

```bash
TEST_BROWSER=firefox TEST_HEADLESS=true dotnet test
```

Run against Grid:

```bash
SELENIUM_GRID_URL=http://localhost:4444/wd/hub TEST_BROWSER=chrome dotnet test
```

## Framework conventions

### Driver lifecycle

New tests should obtain drivers through `WebDriverFactory`; do not pin `chromedriver`, `geckodriver`, or `msedgedriver` packages. Each concurrent test must own an independent driver. Always call `Quit`/`Dispose` from fixture teardown even after assertion failure.

### Waiting

Implicit wait is intentionally zero. Encapsulate explicit waits inside the page/component that understands the condition. Never combine a large implicit wait with explicit waits, and avoid `Thread.Sleep` for synchronization.

### Page objects

A page object models a UI capability, not the entire HTML document. Keep assertions in tests unless an assertion is intrinsic to a component contract. Prefer stable test IDs and accessible locators over DOM structure or generated CSS classes.

### Failure evidence

On failure, capture screenshot, current URL, and page source via `ArtifactCollector`. When adding console/network capture, redact cookies, authorization headers, credentials, and sensitive request bodies.

### Test data

Tests must be independently runnable and should create unique data. Prefer API-based setup for states that are not the subject of the UI test. Shared accounts should be avoided for parallel suites or isolated into non-parallel collections.

## CI

The workflow restores, builds, and tests on .NET 8, writes TRX and XPlat coverage results, and uploads `TestResults/` plus browser artifacts even when tests fail. Concurrency cancellation prevents stale runs from consuming browser capacity after a newer commit arrives.

## Extension points

- add browser-specific capabilities inside `WebDriverFactory`, not individual tests;
- add environment variables through `TestSettings` with validation and safe defaults;
- add reusable widgets/components beneath `PageObjects` when multiple pages share interaction behavior;
- add Grid/cloud-provider capabilities without changing test intent;
- add accessibility, visual, or network assertions as dedicated services so they can be enabled selectively.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/TEST_STRATEGY.md`](docs/TEST_STRATEGY.md) for design and governance details.
