# Security Policy

## Reporting a vulnerability

**Please do not open a public issue for security problems.**

Report privately through GitHub's **[Private vulnerability reporting](https://github.com/iamavasya/Project-K/security/advisories/new)**
(Security tab → *Report a vulnerability*). This opens a private advisory visible
only to you and the maintainers.

Please include:

- affected component (backend API, frontend, self-host bundle) and version/tag,
- a description and impact,
- reproduction steps or a proof of concept,
- any suggested remediation.

We aim to acknowledge a report within a few days and will keep you updated as we
triage and fix. Please allow a reasonable time for a fix before any public
disclosure.

## Supported versions

This project is in active beta. Only the **latest released version** receives
security fixes; please upgrade before reporting against older tags.

| Version | Supported |
|---|---|
| latest release | ✅ |
| older betas | ❌ |

## Scope

In scope: the ProjectK backend, the Лілейка frontend, and the self-host Docker
bundle in this repository. Out of scope: third-party services (Azure,
Cloudflare, Telegram, Resend) and self-hoster misconfiguration.
