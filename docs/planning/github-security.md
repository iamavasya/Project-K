# GitHub security & quality — runbook

Status: committable files landed; repo-settings toggles pending (need repo admin).

## What's in the repo now

| File | Purpose |
|---|---|
| `.github/dependabot.yml` | weekly dependency PRs for nuget, npm, github-actions, docker |
| `.github/workflows/codeql.yml` | CodeQL scanning for C# and JS/TS (build-mode `none`) |
| `SECURITY.md` | private vulnerability-reporting policy |
| `.github/CODEOWNERS` | review routing |

## Repo-settings toggles (owner/admin — one-time)

These are **account/repo settings**, not files. Do them in the GitHub UI
(*Settings → Code security and analysis*) or via `gh api`. All are free for this
repo.

### UI
- **Private vulnerability reporting** → Enable
- **Dependabot alerts** → Enable
- **Dependabot security updates** → Enable
- **Secret scanning** → Enable, and **Push protection** → Enable
- **Code scanning** → the `CodeQL` workflow appears once it has run on `main`

### CLI equivalents

```bash
# Dependabot alerts + automated security fixes
gh api -X PUT  /repos/iamavasya/Project-K/vulnerability-alerts
gh api -X PUT  /repos/iamavasya/Project-K/automated-security-fixes

# Private vulnerability reporting
gh api -X PUT  /repos/iamavasya/Project-K/private-vulnerability-reporting

# Secret scanning + push protection
gh api -X PATCH /repos/iamavasya/Project-K \
  -f security_and_analysis[secret_scanning][status]=enabled \
  -f security_and_analysis[secret_scanning_push_protection][status]=enabled
```

Verify:

```bash
gh api /repos/iamavasya/Project-K --jq '.security_and_analysis'
```

## Follow-ups / caveats

- **Private NuGet feed.** The backend restores `ProjectK.Optimization` and
  `ProjectK.ProbeAndBadges.*` from a private feed (`NUGET_AUTH_TOKEN`).
  - CodeQL uses `build-mode: none`, so it does **not** need the feed.
  - Dependabot's nuget updater may fail to resolve those private packages. If it
    does, add a `registries:` block to `dependabot.yml` with the feed URL and a
    `${{secrets.NUGET_...}}` token, or `ignore` the `ProjectK.*` packages there.
- **Secret-scanning backlog.** After enabling, review any historical hits and
  rotate exposed credentials (connection strings, tokens) — see the Azure audit
  for the secrets-in-App-Service-config recommendation (move to Key Vault).
- **First CodeQL run** must complete on `main` before code-scanning results and
  the "Code scanning" setting surface in the Security tab.
