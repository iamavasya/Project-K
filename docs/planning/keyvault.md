# Key Vault migration — Variant A (Key Vault references)

Closes Azure audit finding #1 (secrets in plaintext App Service settings). Chosen
approach: **App Service Key Vault references** — no code change, self-host/local
untouched, the app's existing system-assigned managed identity does the reading.

## Prerequisites (already in place)
- App Service `api-projectk-prod-new` has a **system-assigned managed identity**.
- Subscription `projectk-prod-sub`, RG `rg-projectk-prod-paid`, region Poland Central.

## Secrets to migrate

Only these (the rest of the app settings are not secrets and stay as-is). The KV
secret name is the app-setting name with `_`/`__` → `-` (KV names allow only
alphanumerics and hyphens); the app-setting **name stays the same**, only its
**value** becomes a reference.

| App setting | KV secret name |
|---|---|
| `Jwt__Key` | `Jwt-Key` |
| `Email__ApiKey` | `Email-ApiKey` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings-DefaultConnection` |
| `ConnectionStrings__BlobStorage` | `ConnectionStrings-BlobStorage` |
| `Telegram__DevAlerts__BotToken` | `Telegram-DevAlerts-BotToken` |
| `Telegram__PublicChannel__BotToken` | `Telegram-PublicChannel-BotToken` |
| `RateLimitBypassKey` | `RateLimitBypassKey` |
| `AdminServiceToken__PublicAnnouncementDraft` | `AdminServiceToken-PublicAnnouncementDraft` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `APPLICATIONINSIGHTS-CONNECTION-STRING` |
| `Serilog__WriteTo__1__Args__connectionString` | `Serilog-WriteTo-1-Args-connectionString` |

## Step 0 — register the Key Vault resource provider (one-time, free)
The subscription must be registered for `Microsoft.KeyVault` (same as
`Microsoft.Security` earlier). Registration is async (~1–2 min).
```bash
az provider register --namespace Microsoft.KeyVault
az provider show --namespace Microsoft.KeyVault --query registrationState -o tsv   # wait for "Registered"
```

## Step 1 — create the vault (RBAC model, soft-delete + purge protection)
Vault name must be globally unique; change if taken.
```bash
az keyvault create -n kv-projectk-prod -g rg-projectk-prod-paid -l polandcentral \
  --enable-rbac-authorization true --enable-purge-protection true
```

## Step 2 — grant the App Service managed identity read access
PowerShell (avoid `$PID` — it's a read-only automatic variable in PowerShell):
```powershell
$principal = az webapp identity show -n api-projectk-prod-new -g rg-projectk-prod-paid --query principalId -o tsv
$vaultId = az keyvault show -n kv-projectk-prod --query id -o tsv
az role assignment create --assignee $principal --role "Key Vault Secrets User" --scope $vaultId
```
Role propagation can take a few minutes before references resolve.

## Step 2b — grant YOURSELF write access to secrets
Step 2 gave the *app* read access. To *create* secrets you (the operator) need the
**Key Vault Secrets Officer** role on the vault. Wait ~1–2 min after granting.
```powershell
$me = az ad signed-in-user show --query id -o tsv
$vid = az keyvault show -n kv-projectk-prod --query id -o tsv
az role assignment create --assignee $me --role "Key Vault Secrets Officer" --scope $vid
```

## Step 3 — migrate values (PowerShell; robust against Windows CLI parsing)
Reads each current value, stores it in the vault via a temp **file** (avoids
`az.cmd` mangling `;`/`=`/`(` in values), then applies the KV references through a
**JSON file** (avoids the `@(...)` parenthesis parse error on Windows). Idempotent.

**Two bugs this fixes** (do not revert to the inline one-liner):
- PowerShell variables are **case-insensitive**, so a `$kv` loop var silently
  overwrites `$KV` (the vault name). Use distinct names (`$vault`, `$sname`).
- `--settings "name=@Microsoft.KeyVault(...)"` breaks `az.cmd` on the `(` — pass
  settings via `--settings "@file.json"` instead.

```powershell
$RG="rg-projectk-prod-paid"; $APP="api-projectk-prod-new"; $vault="kv-projectk-prod"
$names = @(
  "Jwt__Key","Email__ApiKey","ConnectionStrings__DefaultConnection","ConnectionStrings__BlobStorage",
  "Telegram__DevAlerts__BotToken","Telegram__PublicChannel__BotToken","RateLimitBypassKey",
  "AdminServiceToken__PublicAnnouncementDraft","APPLICATIONINSIGHTS_CONNECTION_STRING",
  "Serilog__WriteTo__1__Args__connectionString")
$all = az webapp config appsettings list -n $APP -g $RG | ConvertFrom-Json
$refs = @()
foreach ($n in $names) {
  $v = ($all | Where-Object { $_.name -eq $n }).value
  if ([string]::IsNullOrWhiteSpace($v)) { Write-Host "skip $n (empty)"; continue }
  if ($v -like '@Microsoft.KeyVault*') { Write-Host "skip $n (already ref)"; continue }
  $sname = $n.Replace('__','-').Replace('_','-')
  $tmp = Join-Path $PWD "_secret.tmp"
  [System.IO.File]::WriteAllText($tmp, $v)
  az keyvault secret set --vault-name $vault -n $sname --file $tmp -o none
  Remove-Item $tmp -Force
  $refs += [pscustomobject]@{ name = $n; value = "@Microsoft.KeyVault(SecretUri=https://$vault.vault.azure.net/secrets/$sname/)"; slotSetting = $false }
  Write-Host "secret set $sname"
}
$json = Join-Path $PWD "kv-settings.json"
($refs | ConvertTo-Json) | Set-Content -Path $json -Encoding ascii
az webapp config appsettings set -n $APP -g $RG --settings "@$json"
Remove-Item $json -Force
Write-Host "DONE"
```
After it succeeds, restart the app so it resolves the references:
```powershell
az webapp restart -n api-projectk-prod-new -g rg-projectk-prod-paid
```

## Step 4 — verify
```bash
az webapp config appsettings list -n api-projectk-prod-new -g rg-projectk-prod-paid \
  --query "[?contains(value,'KeyVault')].name" -o tsv
```
Portal → App Service → Configuration shows a green **Key Vault Reference**
resolution status next to each. Then open the app and confirm login + DB + email
still work (the app now reads secrets via the vault).

## Notes & caveats
- **Self-host / local are untouched.** They keep raw env values; no vault involved.
  This is purely the Azure prod app.
- **CI unaffected.** The GitHub Actions deploy does not manage these app settings
  (it only injects the app version into a config file), so migration needs no
  workflow change.
- **KV reference caching.** References are cached; after rotating a secret in the
  vault, restart the app (or wait for the periodic refresh) to pick it up.
- **`@` escaping.** If `az webapp config appsettings set` misreads the leading `@`
  of the reference on your shell, set that one via the portal, or use
  `--settings @file.json` with the pair in a JSON file.
- **Vault name uniqueness / region.** `kv-projectk-prod` must be globally unique;
  keep it in Poland Central with the rest of the stack.
- **Ties to other findings.** Once secrets are in KV, disabling storage shared-key
  (#6) and enabling SQL Entra-only (#5) becomes a follow-up: swap the
  `ConnectionStrings__*` secrets for managed-identity access instead of keys/passwords.
