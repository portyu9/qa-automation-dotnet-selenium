# UI automation strategy

## Scope

Browser automation is reserved for behavior that requires a real browser: navigation, rendering, client-side state, accessibility-facing interactions, and critical end-to-end workflows. API/service tests should cover business combinations that do not require browser semantics.

## Locator policy

Prefer stable semantic selectors in this order: dedicated test IDs, accessible role/name, label, and stable domain attributes. Avoid selectors coupled to CSS layout, DOM depth, generated classes, or visible text that is frequently localized.

Page objects should expose intent such as `Login` or `AddItemToCart`, not generic wrappers such as `ClickElement(string selector)`.

## Synchronization

Use explicit waits for observable state: element visible/enabled, URL transition, document state, or application-specific condition. Fixed sleeps are not synchronization and should not be introduced into functional tests.

## Data and isolation

Each test owns its browser and state. Prefer creating prerequisite data through APIs and clear it after the test. Tests must not rely on execution order. Run identifiers should be included in created data when the application permits it.

## Failure triage

For browser failures retain, at minimum, the assertion stack, screenshot, URL, and DOM/page source. Where supported, add browser console and network diagnostics. Artifacts need run/test correlation and bounded retention.

## Cross-browser strategy

Use Chromium as the fast pull-request gate unless product risk requires more. Firefox/Edge/Grid matrices are appropriate for scheduled or release validation. Cross-browser coverage should be risk-driven rather than multiplying every test across every browser by default.

## Flake governance

Retries may protect CI from known external instability, but retry counts are not a substitute for fixing race conditions. Track tests that pass only after retry. Common causes are shared data, stale elements, mixed wait strategies, environment saturation, and non-deterministic navigation.
