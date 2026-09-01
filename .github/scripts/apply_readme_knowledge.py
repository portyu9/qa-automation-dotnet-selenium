from pathlib import Path
import re

path = Path('README.md')
text = path.read_text(encoding='utf-8')
marker = '## Dependency maintenance\n'
section = '''## Confidence boundaries

The browser framework treats session ownership, synchronization, compatibility, packaging, and deployed-environment behavior as separate confidence claims.

| Signal | Confidence gained | Deliberate limit |
| --- | --- | --- |
| xUnit framework/configuration contracts | Driver factory, configuration, lifecycle, waits, and evidence policy behave deterministically without requiring a deployed system | Does not prove browser rendering, remote-grid behavior, or external infrastructure |
| Primary Selenium browser gate | Covered navigation, interaction, frame/window/alert/cookie/JavaScript, and page-object contracts execute in the primary qualified browser | It does not imply universal browser, device, operating-system, viewport, or accessibility coverage |
| Alternate-browser compatibility | Covered flows survive a deliberate browser-engine change while the application contract remains controlled | Selected compatibility coverage is not complete cross-browser equivalence |
| Repository-owned fixture | WebDriver semantics are exercised without public DNS, third-party uptime, uncontrolled content, or undeclared accounts | It does not prove deployed TLS, ingress, identity, production data, or external-service integration |
| Explicit waits / no implicit readiness model | Failures identify which observable browser condition did not become true instead of hiding uncertainty behind elapsed time | A timeout value cannot make an invalid readiness condition correct |
| Optional Grid execution | The framework can delegate session creation to a remote WebDriver endpoint while retaining the same test contract | It does not prove any particular cloud/grid provider's capacity, network, browser image, or operational SLA |
| TRX / coverage / browser evidence | CI retains attributable execution and failure context while native test exit status remains authoritative | Artifact presence alone is not proof; executed tests, conclusions, and evidence semantics must agree |
| Locked NuGet / audit / CodeQL / Trivy / dependency review | Reproducibility and independent security controls inspect distinct dependency, source, repository, and change-diff surfaces | A locked graph or green scanner result is scoped evidence, not proof of vulnerability absence |

Prefer the **lowest-cost boundary that contributes the semantics under test**. Browser automation is appropriate for browser-owned behavior; configuration and lifecycle policy belong below the browser, while deployed Grid/environment validation remains an explicit integration concern.

'''
if '## Confidence boundaries\n' not in text:
    if marker not in text:
        raise SystemExit('Dependency maintenance marker missing')
    text = text.replace(marker, section + marker)
path.write_text(text, encoding='utf-8')

patterns = [
    re.compile(r'\.NET\s+\d', re.I),
    re.compile(r'\bnet\d+\.\d\b', re.I),
    re.compile(r'\bxUnit\s+v?\d', re.I),
    re.compile(r'\bSelenium\s+v?\d', re.I),
    re.compile(r'\bChrome\s+\d', re.I),
    re.compile(r'\bFirefox\s+\d', re.I),
    re.compile(r'\bNuGet\s+v?\d', re.I),
    re.compile(r'\bSDK\s+\d', re.I),
]
candidates = []
for md in [Path('README.md'), *Path('docs').rglob('*.md')]:
    for number, line in enumerate(md.read_text(encoding='utf-8').splitlines(), 1):
        if any(pattern.search(line) for pattern in patterns):
            candidates.append(f'{md}:{number}: {line}')
if candidates:
    raise SystemExit('Residual .NET/tool version candidates:\n' + '\n'.join(candidates))
