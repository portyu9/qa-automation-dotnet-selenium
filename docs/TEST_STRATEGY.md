# Test strategy

## Purpose

The suite uses xUnit v3 for deterministic framework contracts and real-browser tests for user-visible behavior. Browser coverage is deliberately narrow: use it where rendering/navigation/interaction is the behavior under test, and keep framework policy independently testable without starting a browser wherever possible.

## Test categories

| Category | Primary question | Browser required? | Typical gate |
| --- | --- | ---: | --- |
| Configuration contract | Are runtime inputs accepted/rejected correctly without mutating process-global state? | No | Every change |
| Diagnostics contract | Are evidence paths and URL sanitization safe/deterministic? | No | Every change |
| Driver/session integration | Does the framework construct and own a real browser correctly? | Yes | Pull request |
| User-flow E2E | Does a critical workflow succeed through the browser boundary? | Yes | Pull request / release |

## Configuration-negative testing

Configuration tests should prove failure before driver creation. Current contracts include:

- unsupported browser names;
- unsafe/malformed base URLs;
- URL credentials;
- query/fragment-bearing base and Grid URLs;
- non-positive timeout budgets where applicable.

Configuration-contract tests inject a variable lookup into `TestSettings` rather than temporarily writing process environment variables. This is required for parallel safety: process-level environment mutation can otherwise leak an intentionally invalid value into another xUnit v3 test that is constructing a real browser at the same time.

A configuration failure should never be diagnosed by waiting for Selenium to fail later.

## Browser lifecycle policy

Tests execute through `BrowserTestSession`, which creates the driver with `WebDriverFactory` and owns cleanup. Direct driver construction in tests is a framework bypass and should be rejected in review.

Every browser test receives:

- zero implicit wait;
- explicit page-load budget;
- deterministic viewport;
- local or Grid execution through the same code path;
- best-effort failure evidence before driver teardown.

Failure evidence must not change the exception reported by the test. A broken screenshot/page-source call is secondary information.

## Synchronization policy

Use explicit waits around observable conditions. Appropriate conditions include visible/clickable elements, URL/state changes, or document/page readiness.

Do not use:

- implicit waits;
- `Thread.Sleep` as readiness;
- arbitrary retry loops around failed assertions;
- wait helpers that swallow timeout context.

A slow environment should consume a bounded wait budget and produce a clear failure, not alter the test's semantics.

## Selector and page-object policy

Page objects own selectors and feature-level interactions. Prefer stable IDs/data attributes and accessibility-oriented selectors when the application exposes them.

Keep assertions in tests unless the assertion represents a reusable page invariant such as `IsLoaded`. Do not turn pages into generic Selenium wrapper classes.

## Evidence policy

On browser failure inspect evidence in this order:

1. xUnit assertion/stack trace;
2. sanitized current URL;
3. screenshot;
4. page source;
5. Grid/browser infrastructure logs if the failure is session-level.

`url.txt` removes user-info, query strings, and fragments. Screenshot/page-source content is not automatically redacted; synthetic non-sensitive data is required.

## Test host and SDK policy

The test host is part of the framework contract. The project targets .NET 8, uses xUnit v3, and commits `global.json` so a newer machine-wide SDK cannot silently change `dotnet test` behavior.

The current CI path uses `xunit.runner.visualstudio` through `dotnet test` because TRX and XPlat coverage are retained as operational evidence. A future move to another Microsoft.Testing.Platform invocation should be treated as a runner/evidence migration and validated explicitly rather than introduced as an incidental package update.

## CI gate

CI restores/builds the project, runs the test suite in headless Chrome, emits TRX plus XPlat coverage, and uploads `TestResults/` and `artifacts/` even when tests fail.

The real-browser CI run proves that xUnit v3 discovery/execution, page objects, explicit waits, session lifecycle, driver factory, Selenium Manager, TRX, and coverage cooperate in a runner environment. Framework tests make configuration/diagnostic regressions attributable without depending on browser startup.

## Parallelism

One driver per test is the minimum isolation boundary. Before enabling higher browser concurrency, verify that application accounts and mutable test data are independent.

Do not share a static driver or global page objects. Do not mutate process-global environment variables from a test as a temporary configuration fixture. If one workflow requires exclusive application state, isolate only that test collection rather than globally disabling xUnit parallelism.

## Failure classification

| Failure class | First interpretation |
| --- | --- |
| Configuration contract | Framework input regression/misconfiguration |
| Test-host/SDK mismatch | Runner/toolchain selection defect; inspect `global.json`, SDK, and adapter versions |
| Driver creation | Browser/Selenium Manager/Grid/runtime issue |
| Explicit-wait timeout | Expected state was not observed within policy budget |
| Assertion | Application behavior or test expectation mismatch |
| Evidence capture | Secondary diagnostic failure; preserve primary exception |
| Teardown | Session cleanup/infrastructure defect |

A rerun is diagnostic information, not a resolution. A test that becomes green only when rerun should be investigated for state leakage, environment saturation, synchronization, or an unstable dependency.

## Exit criteria

A browser-framework change is ready when:

- the intended .NET SDK is selected reproducibly;
- project build succeeds without hidden warnings introduced by the change;
- xUnit v3 discovers and executes all expected tests;
- configuration and diagnostics contracts pass in parallel-safe form;
- real headless-browser execution passes;
- TRX and XPlat coverage remain available;
- no implicit/fixed-wait workaround is introduced;
- artifacts remain privacy-aware and bounded;
- documentation reflects changes to lifecycle, synchronization, test-host, or evidence semantics.
