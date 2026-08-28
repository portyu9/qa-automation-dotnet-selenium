# Test strategy

## Purpose

The suite uses xUnit for deterministic framework contracts and real-browser tests for user-visible behavior. Browser coverage is deliberately narrow: use it where rendering/navigation/interaction is the behavior under test, and keep framework policy independently testable without starting a browser wherever possible.

## Test categories

| Category | Primary question | Browser required? | Typical gate |
| --- | --- | ---: | --- |
| Configuration contract | Are runtime inputs accepted/rejected correctly? | No | Every change |
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

## CI gate

CI restores/builds the project, runs the test suite in headless Chrome, emits TRX plus XPlat coverage, and uploads `TestResults/` and `artifacts/` even when tests fail.

The real-browser CI run proves that page objects, explicit waits, session lifecycle, driver factory, and Selenium Manager cooperate in a runner environment. Framework unit tests make configuration/diagnostic regressions attributable without depending on browser startup.

## Parallelism

One driver per test is the minimum isolation boundary. Before enabling higher browser concurrency, verify that application accounts and mutable test data are independent.

Do not share a static driver or global page objects. If one workflow requires exclusive state, isolate only that test collection.

## Failure classification

| Failure class | First interpretation |
| --- | --- |
| Configuration contract | Framework input regression/misconfiguration |
| Driver creation | Browser/Selenium Manager/Grid/runtime issue |
| Explicit-wait timeout | Expected state was not observed within policy budget |
| Assertion | Application behavior or test expectation mismatch |
| Evidence capture | Secondary diagnostic failure; preserve primary exception |
| Teardown | Session cleanup/infrastructure defect |

A rerun is diagnostic information, not a resolution. A test that becomes green only when rerun should be investigated for state leakage or synchronization defects.

## Exit criteria

A browser-framework change is ready when:

- project build succeeds without hidden warnings introduced by the change;
- configuration and diagnostics contracts pass;
- real headless-browser execution passes;
- no implicit/fixed-wait workaround is introduced;
- artifacts remain privacy-aware and bounded;
- documentation reflects changes to lifecycle, synchronization, or evidence semantics.
