# Test strategy

## Purpose

The suite uses xUnit v3 for deterministic framework contracts and Selenium for browser-visible behavior. Browser coverage is deliberately narrow and required CI is repository-controlled: framework correctness must not depend on a public application remaining reachable or unchanged.

The execution contract includes the .NET runtime and dependency graph. A browser test is not considered reproducible if it passes only because a developer machine selected a different SDK or resolved a different transitive package set.

## Test categories

| Category | Primary question | Browser? | Target |
| --- | --- | ---: | --- |
| Toolchain/dependency | Is the exact SDK/lock graph valid and free of gated advisories? | No | Local restore/build |
| Configuration | Are runtime inputs accepted/rejected safely? | No | None |
| Diagnostics | Are artifact paths, URL sanitization, and evidence defaults safe? | No | None |
| Driver/session | Does the framework construct, own, and tear down a real browser? | Yes | Local fixture |
| Browser context | Are cookies, frames, alerts, script execution, and child windows owned correctly? | Yes | Local fixture |
| Authentication flow | Do acceptance and rejection behave correctly? | Yes | Local fixture |
| Compatibility | Does the same contract work across engines? | Yes | Local fixture |
| Environment integration | Does a deployed system satisfy the browser contract? | Yes | Explicit `TEST_BASE_URL` |
| Grid transport | Does remote session negotiation work? | Yes | Optional Grid |
| Source security | Does C# static analysis detect security/data-flow issues? | No | CodeQL |
| Repository security | Do dependency/configuration/secret scans remain clean at policy severity? | No | Trivy / NuGet Audit |

## Runtime and dependency qualification

The project targets `net10.0` and pins SDK `10.0.400` in `global.json` with roll-forward disabled.

Required restore uses the committed `packages.lock.json` in `--locked-mode`. This creates two distinct failure classes:

- the declared PackageReference/target framework and the committed lock graph disagree;
- the graph resolves correctly but contains a dependency with a gated security advisory.

NuGet Audit is explicitly enabled for the full graph with `NuGetAuditMode=all` and `NuGetAuditLevel=high`. HIGH and CRITICAL advisory warnings (`NU1903` and `NU1904`) are promoted to errors. The policy is explicit even though .NET 10 audits transitive packages by default, so a future runtime-default change cannot silently weaken the repository contract.

`TreatWarningsAsErrors` makes compile/analyzer warnings fail the Release build. Do not downgrade these policies simply to make a dependency update green; classify and resolve the underlying incompatibility or advisory.

## Deterministic default target

Required Chrome/Firefox gates use `http://127.0.0.1:3200`, served by `LocalUiServer`. xUnit collection lifecycle starts the server before browser-test construction and disposes it after the collection.

The fixture owns accepted TCP request tasks as well as the listener. Disposal cancels/stops acceptance and drains the owned client tasks before teardown completes, so a green collection cannot leave repository-fixture request work running in the background.

The required gate therefore excludes public DNS/TLS, demonstration-site changes, external accounts, rate limiting, and third-party availability. Those risks belong to explicitly selected environment tests.

## Configuration-negative testing

Configuration tests prove failure before WebDriver creation. They cover unsupported browsers, unsafe/malformed base URLs, credentials, query/fragment-bearing URLs, explicit port `0`, unsafe Grid URLs, and invalid run identifiers.

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
- best-effort minimal evidence before teardown;
- preservation of the original failure.

The local fixture lifecycle separately guarantees that its listener, accept loop, and accepted request tasks have explicit ownership and teardown.

## Native browser-context capability coverage

Use native Selenium APIs directly when they clearly express a requirement. The deterministic capability suite verifies:

- `IJavaScriptExecutor` for explicit browser-side script execution;
- cookie creation/readback through `Manage().Cookies`;
- frame switching followed by `DefaultContent()` restoration;
- alert text and acceptance;
- child-window creation and restoration through `BrowserWindowScope`.

`BrowserWindowScope` is justified because a child window has lifecycle ownership that must be deterministic even when assertions fail. It waits with a bounded `WebDriverWait`, closes the child it owns, restores the original handle, and supports idempotent disposal.

Add the Selenium Actions API only when product behavior genuinely depends on low-level keyboard, pointer, drag/drop, wheel, touch/pen, or related interaction. Do not add it solely to enumerate Selenium features.

## Authentication coverage

The default application fixture deliberately models both success and failure:

- valid synthetic credentials reach the inventory surface;
- invalid credentials remain on the login surface and expose a stable rejection message.

Negative authentication is a first-class gate because incorrectly accepting invalid input is a distinct regression from incorrectly rejecting valid input.

## Synchronization policy

Use explicit waits around observable state: visible/clickable elements, URL transitions, document/page readiness, alerts, and window-count/handle changes.

Do not use implicit waits, `Thread.Sleep` readiness, arbitrary retry loops, or timeout helpers that swallow causal context.

A slower system may consume its configured wait budget; it should not change the semantic condition being awaited.

## Selector and page-object policy

Page objects own feature-specific locators and operations. Prefer stable IDs/data attributes and accessibility semantics. Keep assertions in tests except reusable page invariants such as loaded-state checks.

Do not turn page objects into generic wrappers for every WebDriver operation.

## External environment policy

Setting a non-default `TEST_BASE_URL` selects a deployed application; the same page/test layer runs against that environment. Such runs must be classified separately because failures can belong to deployment state, network, data, or downstream dependencies.

Do not replace the deterministic required CI lane with a public endpoint merely to increase apparent end-to-end realism.

## Grid policy

`SELENIUM_GRID_URL` changes session transport/location, not application semantics. A Grid connection/capability failure is a separate failure class from a browser-visible application assertion. Explicit port `0` is rejected for both application and Grid targets before driver creation.

## Evidence policy

Inspect failure evidence in this order:

1. xUnit assertion/stack trace;
2. sanitized current URL;
3. screenshot;
4. browser/Selenium Manager/Grid logs for session-level failures;
5. page source only when a caller explicitly opts in for a controlled-data diagnostic case.

Generic automatic capture intentionally does **not** persist page source. DOM source can contain hidden inputs, tokens, personal/customer data, or other values not visible in a screenshot. Page-source capture is therefore an explicit data-handling decision rather than a default failure behavior.

Artifact path identity is validated before any evidence write. HTTP(S) diagnostic URLs drop credentials, query strings, and fragments; non-HTTP URLs are reduced to a scheme-only redacted sentinel, while `about:blank` is preserved. Screenshots remain unredacted visual evidence and require synthetic or controlled data plus bounded retention.

## Security verification strategy

Security controls are complementary rather than interchangeable.

### NuGet Audit

Every required restore audits direct and transitive dependencies. HIGH/CRITICAL advisories fail restore. This is the earliest dependency-security gate and applies before browser creation.

### CodeQL

The security workflow initializes C# CodeQL with `security-extended`, performs the exact locked restore and Release build, then analyzes the compiled source. This covers source/data-flow classes that dependency scanners do not address.

### Trivy

Trivy independently scans the repository filesystem for supported fixed HIGH/CRITICAL dependency findings, supported HIGH/CRITICAL misconfiguration, and committed secrets. JSON output is retained for attribution.

### Dependency Review

Pull requests use GitHub Dependency Review when Dependency graph data is available. If the service is unavailable, the workflow must say so explicitly; NuGet Audit and Trivy remain independent gates but are not represented as equivalent to diff-aware dependency review.

### Dependabot

Dependabot proposes NuGet and GitHub Actions updates. Update generation is not a merge decision: dependency changes must clear the behavioral/runtime/security gates exposed by the changed package.

## CI gate

Primary CI:

1. installs/selects SDK `10.0.400`;
2. restores `packages.lock.json` with `--locked-mode` and HIGH/CRITICAL NuGet Audit;
3. builds `net10.0` Release with warnings as errors;
4. executes the xUnit suite in headless Chrome against the local fixture;
5. retains TRX, XPlat/Cobertura coverage, browser failure artifacts, and a machine-readable CI observability envelope.

Extended CI applies the same restore/build policy and runs Chrome and Firefox independently. Both browser lanes use the repository-owned fixture and bounded job time.

The browser gates prove xUnit discovery/lifecycle, collection fixtures, page objects, explicit waits, native browser-context primitives, Selenium Manager, driver/session/window ownership, fixture-client draining, evidence generation, TRX, and coverage in a real CI browser environment without public-network application coupling.

Security and documentation remain separately attributable workflows instead of being hidden inside the browser job.

## Parallelism

One driver per test is the minimum isolation boundary. The local UI server is shared only by the designated browser collection; framework tests remain independent and do not mutate global environment. The server does not declare teardown complete while accepted request tasks are still owned by it.

If future flows require mutable application state or separate fixture behavior, isolate state/ports explicitly rather than sharing a static driver or disabling all xUnit parallelism.

## Failure classification

| Failure class | First interpretation |
| --- | --- |
| SDK selection | Toolchain reproducibility |
| Locked restore | Dependency graph mismatch/drift |
| `NU1903` / `NU1904` | HIGH/CRITICAL dependency advisory |
| Build warning/error | Source/analyzer quality |
| CodeQL | Static source/data-flow security |
| Trivy | Dependency/configuration/secret security |
| Dependency Review | New dependency risk in the PR delta |
| Configuration | Framework input policy |
| Fixture startup/connection | Local target lifecycle or port ownership |
| Fixture client-drain/teardown | Owned asynchronous fixture cleanup |
| Driver creation | Browser/Selenium Manager/Grid runtime |
| Explicit-wait timeout | Required state was not observed |
| Frame/alert/window/cookie mismatch | Browser-context ownership/behavior |
| Auth rejection mismatch | Rejection/error semantics |
| Assertion | Browser-visible application contract |
| Evidence capture | Secondary diagnostic issue |
| Teardown | Session/infrastructure cleanup |
| Browser-specific failure | Compatibility |
| External-target-only failure | Environment/integration first |

A rerun is diagnostic information, not a resolution. A rerun-only pass should be investigated for state leakage, synchronization, infrastructure saturation, or unstable dependencies.

## Exit criteria

A browser/framework change is ready when:

- SDK `10.0.400` is selected reproducibly;
- locked restore succeeds without rewriting the dependency graph;
- HIGH/CRITICAL NuGet Audit is clean;
- Release build succeeds with warnings as errors;
- xUnit discovers and executes the expected tests;
- configuration/artifact contracts pass without process-global mutation;
- explicit application/Grid port `0` is rejected before driver creation;
- automatic evidence remains minimal and page-source capture is opt-in;
- the local fixture lifecycle is deterministic and drains owned client tasks;
- Chrome browser execution passes;
- Firefox passes when extended coverage applies;
- browser-context capability contracts pass without fixed sleeps;
- both positive and negative authentication contracts pass;
- TRX and XPlat/Cobertura coverage remain available;
- CodeQL and Trivy gates pass;
- Dependency Review passes when GitHub Dependency graph is available, or the workflow explicitly reports that service limitation without weakening the other gates;
- no implicit/fixed-wait workaround is introduced;
- evidence remains privacy-aware and bounded;
- deployed-environment and Grid responsibilities remain separately attributable.
