# Test strategy

## Purpose

The suite uses xUnit v3 for deterministic framework contracts and Selenium for browser-visible behavior. Browser coverage is deliberately narrow and required CI is repository-controlled: framework correctness must not depend on a public application remaining reachable or unchanged.

## Test categories

| Category | Primary question | Browser? | Target |
| --- | --- | ---: | --- |
| Configuration | Are runtime inputs accepted/rejected safely? | No | None |
| Diagnostics | Are artifact paths and URL sanitization safe? | No | None |
| Driver/session | Does the framework construct, own, and tear down a real browser? | Yes | Local fixture |
| Authentication flow | Do acceptance and rejection behave correctly? | Yes | Local fixture |
| Compatibility | Does the same contract work across engines? | Yes | Local fixture |
| Environment integration | Does a deployed system satisfy the browser contract? | Yes | Explicit `TEST_BASE_URL` |
| Grid transport | Does remote session negotiation work? | Yes | Optional Grid |

## Deterministic default target

Required Chrome/Firefox gates use `http://127.0.0.1:3200`, served by `LocalUiServer`. xUnit collection lifecycle starts the server before browser-test construction and disposes it after the collection.

The required gate therefore excludes public DNS/TLS, demonstration-site changes, external accounts, rate limiting, and third-party availability. Those risks belong to explicitly selected environment tests.

## Configuration-negative testing

Configuration tests prove failure before WebDriver creation. They cover unsupported browsers, unsafe/malformed base URLs, credentials, query/fragment-bearing URLs, unsafe Grid URLs, and invalid run identifiers.

Tests inject a variable lookup instead of mutating process environment, preventing invalid test values from leaking into concurrently constructed browser sessions.

## Browser lifecycle policy

Every browser test executes through `BrowserTestSession`. Direct driver construction in test classes is a framework bypass.

The session/factory combination guarantees:

- validated settings before side effects;
- one driver per test instance;
- zero implicit wait;
- bounded page-load timeout;
- deterministic viewport;
- local or Grid execution through one factory;
- best-effort evidence before teardown;
- preservation of the original failure.

## Authentication coverage

The default application fixture deliberately models both success and failure:

- valid synthetic credentials reach `/inventory.html` and expose the inventory container;
- invalid credentials remain on `/` and expose `Invalid username or password`.

Negative authentication is a first-class gate because incorrectly accepting invalid input is a distinct regression from incorrectly rejecting valid input.

## Synchronization policy

Use explicit waits around observable state: visible/clickable elements, URL transitions, or document/page readiness.

Do not use implicit waits, `Thread.Sleep` readiness, arbitrary retry loops, or timeout helpers that swallow causal context.

A slower system may consume its configured wait budget; it should not change the semantic condition being awaited.

## Selector and page-object policy

Page objects own feature-specific locators and operations. Prefer stable IDs/data attributes and accessibility semantics. Keep assertions in tests except reusable page invariants such as loaded-state checks.

Do not turn page objects into generic wrappers for every WebDriver operation.

## External environment policy

Setting a non-default `TEST_BASE_URL` selects a deployed application and suppresses no framework behavior; the same page/test layer runs against that environment. Such runs must be classified separately because failures can belong to deployment state, network, data, or downstream dependencies.

Do not replace the deterministic required CI lane with a public endpoint merely to increase apparent end-to-end realism.

## Grid policy

`SELENIUM_GRID_URL` changes session transport/location, not application semantics. A Grid connection/capability failure is a separate failure class from a browser-visible application assertion.

## Evidence policy

Inspect failure evidence in this order:

1. xUnit assertion/stack trace;
2. sanitized current URL;
3. screenshot;
4. page source;
5. browser/Selenium Manager/Grid logs for session-level failures.

Diagnostic URL output strips credentials, query strings, and fragments. Screenshots/page source are not generally redacted and require synthetic or controlled data.

## Test host and SDK policy

The project targets .NET 8, uses xUnit v3, and commits `global.json`. CI uses `dotnet test`, TRX, and XPlat coverage. SDK/runner/adapter changes are execution-contract changes and require explicit validation.

## CI gate

Primary CI restores/builds once and executes the suite in headless Chrome against the local fixture. Extended CI runs Chrome and Firefox independently. Both retain evidence and run with bounded job time.

The browser gate proves xUnit discovery/lifecycle, collection fixtures, page objects, explicit waits, Selenium Manager, driver/session ownership, evidence generation, TRX, and coverage in a real CI browser environment without public-network application coupling.

## Parallelism

One driver per test is the minimum isolation boundary. The local UI server is shared only by the designated browser collection; framework tests remain independent and do not mutate global environment.

If future flows require mutable application state or separate fixture behavior, isolate state/ports explicitly rather than sharing a static driver or disabling all xUnit parallelism.

## Failure classification

| Failure class | First interpretation |
| --- | --- |
| Configuration | Framework input policy |
| SDK/test host | Toolchain selection |
| Fixture startup/connection | Local target lifecycle or port ownership |
| Driver creation | Browser/Selenium Manager/Grid runtime |
| Explicit-wait timeout | Required state was not observed |
| Auth rejection mismatch | Rejection/error semantics |
| Assertion | Browser-visible application contract |
| Evidence capture | Secondary diagnostic issue |
| Teardown | Session/infrastructure cleanup |
| Browser-specific failure | Compatibility |
| External-target-only failure | Environment/integration first |

A rerun is diagnostic information, not a resolution. A rerun-only pass should be investigated for state leakage, synchronization, infrastructure saturation, or unstable dependencies.

## Exit criteria

A browser/framework change is ready when:

- the intended .NET SDK is selected reproducibly;
- build succeeds;
- xUnit discovers and executes the expected tests;
- configuration/artifact contracts pass without process-global mutation;
- the local fixture lifecycle is deterministic;
- Chrome browser execution passes;
- Firefox passes when extended coverage applies;
- both positive and negative authentication contracts pass;
- TRX and XPlat coverage remain available;
- no implicit/fixed-wait workaround is introduced;
- evidence remains privacy-aware and bounded;
- deployed-environment and Grid responsibilities remain separately attributable.
