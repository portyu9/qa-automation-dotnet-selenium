# .NET Selenium UI Automation Framework

I created this repository to demonstrate how I build robust browser automation frameworks with C# and Selenium WebDriver.  My goal with this project is to mirror the structure and engineering discipline I use on real world QA automation efforts.  The design follows the **Page Object Model** pattern, encapsulates configuration details, and is easily extendible for more pages and test cases.

## Key Features

- **Selenium with WebDriverManager** – I use `Selenium.WebDriver` and `WebDriverManager` packages to drive modern Chrome browsers.  The `DriverManager` automatically downloads a compatible driver so I don't have to commit binary drivers to source control.
- **xUnit Test Runner** – Tests are written with xUnit attributes and assertions.  This framework plays nicely with the .NET ecosystem and integrates seamlessly into build pipelines.
- **Page Object Model (POM)** – Each page of the application under test has its own class encapsulating selectors and actions.  This abstraction keeps my tests clean and maintainable.
- **Headless by Default** – Browser instances run in headless mode during CI to avoid the overhead of rendering a GUI.  When running locally I can comment out the headless option for debugging.
- **CI‑Ready** – A GitHub Actions workflow restores dependencies and executes the test suite on every push.  The workflow uses `actions/setup‑dotnet` to install the correct .NET SDK and runs the tests headlessly on Ubuntu.

## Technology Stack

- **Language:** C# (Target framework **.NET 7.0**)
- **Test Framework:** xUnit
- **Browser Automation:** Selenium WebDriver with WebDriverManager
- **Build Tool:** `dotnet` CLI
- **CI:** GitHub Actions

## Structure

```
qa-automation-dotnet-selenium/
├── PageObjects/
│   ├── BasePage.cs
│   ├── HomePage.cs
│   └── LoginPage.cs
├── Tests/
│   └── LoginTests.cs
├── UiTests.csproj
├── .github/
│   └── workflows/
│       └── ci.yml
└── README.md
```

- **PageObjects/** – Contains POM classes for the login and inventory pages of [Sauce Demo](https://www.saucedemo.com/).  I chose this public demo site because it is designed for automated testing.  The `BasePage` class holds common browser interactions.
- **Tests/** – Holds xUnit tests.  `LoginTests` verifies that a standard user can log in successfully and be redirected to the inventory page.  Additional tests could be added to cover sorting, cart operations or negative login scenarios.
- **UiTests.csproj** – Project file specifying dependencies and target framework.
- **ci.yml** – GitHub Actions workflow that builds the project and runs the tests in headless mode.

## Getting Started

1. **Prerequisites** – Install the [.NET 7 SDK](https://dotnet.microsoft.com/download) and Google Chrome if you want to run tests locally with a GUI.
2. **Restore dependencies** – Run `dotnet restore` from the project root.  This will download Selenium, WebDriverManager and xUnit packages.
3. **Run tests** – Execute `dotnet test`.  The test runner will download the appropriate ChromeDriver and spin up a headless browser session to perform the login test.  When running locally and you want to see the browser, remove or comment out the line adding the `--headless=new` argument in `LoginTests.cs`.

## Extending the Framework

The foundation set up here makes it straightforward to grow the suite.  To add new pages, create additional classes in `PageObjects/` that inherit from `BasePage`.  To add tests, create new test classes in the `Tests/` folder and leverage existing page objects for interactions.  You can also integrate other drivers (Firefox, Edge) by updating the driver setup in `LoginTests.cs` and referencing the corresponding WebDriverManager config.  For large suites, consider introducing a test fixture for driver management and utilities for loading configuration such as credentials from environment variables.

---

I hope this example illustrates my approach to building clean, maintainable UI test frameworks in .NET.  Feel free to fork or adapt it to suit your own projects.


> **Note:** This repository now includes a minor enhancement note added to demonstrate Quickdraw and YOLO achievements.
