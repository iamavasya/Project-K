# Azure prod-paid — security & cost audit

Status: **runbook ready; live findings pending** `az login` + subscription
confirmation. All audit commands below are **read-only**.

## Prep

1. Azure CLI installed (`winget install Microsoft.AzureCLI`).
2. `az login` (device code or browser) — **the user runs this**, Claude never
   holds the credentials.
3. Select the prod-paid subscription:
   ```bash
   az account list -o table
   az account set --subscription "<PROD_PAID_SUB_ID_OR_NAME>"   # confirm which
   ```
4. Inventory:
   ```bash
   az resource list -o table
   az group list -o table
   ```

## What the app uses (from the repo)

| Azure service | Evidence | Notes |
|---|---|---|
| App Service (API) | `dotnet.yml` → `azure/webapps-deploy`, `vars.AZURE_WEBAPP_NAME` | connection strings injected as app settings |
| Static Web Apps (web) | `angular.yml` → `Azure/static-web-apps-deploy` | staging host `ambitious-dune-0421b2803...` |
| Azure SQL | `UseSqlServer`, `ConnectionStrings:DefaultConnection` | injected at runtime |
| Blob Storage | `Azure.Storage.Blobs`, container `photos`, `PublicAccess: true` | public blob read |
| Application Insights | Serilog sink `Serilog.Sinks.ApplicationInsights` | Serilog-only |
| Key Vault | — | **none** — a known gap |
| Cloudflare (not Azure) | `CF-IPCountry` geo-block, `projectk.rostyslav-mukha.dev` | fronts the origin |

## Security checks

### Storage account (highest-priority — public `photos` container)
```bash
az storage account list -o table
az storage account show -n <acct> -g <rg> \
  --query "{https:enableHttpsTrafficOnly, tls:minimumTlsVersion, publicBlob:allowBlobPublicAccess, sharedKey:allowSharedKeyAccess, network:networkRuleSet.defaultAction}"
az storage container list --account-name <acct> --auth-mode login \
  --query "[].{name:name, publicAccess:properties.publicAccess}" -o table
```
Look for: `allowBlobPublicAccess=true`, container `publicAccess=blob`, TLS < 1.2,
`networkRuleSet.defaultAction=Allow`. Recommend: front blobs with a CDN/SAS or
disable public access and serve via the API; enforce TLS 1.2; restrict network.

### Azure SQL
```bash
az sql server list -o table
az sql server firewall-rule list -s <server> -g <rg> -o table
az sql server show -n <server> -g <rg> --query "{tls:minimalTlsVersion, adOnly:administrators.azureADOnlyAuthentication}"
az sql db tde show -s <server> -g <rg> -n <db> 2>/dev/null
az security atp storage show ... # / Defender for SQL via portal
```
Look for: `0.0.0.0` / "Allow Azure services" broad rules, TLS < 1.2, no Entra-only
auth, TDE off, no Defender for SQL, auditing off.

### App Service
```bash
az webapp show -n <app> -g <rg> --query "{https:httpsOnly, identity:identity.type}"
az webapp config show -n <app> -g <rg> --query "{minTls:minTlsVersion, ftps:ftpsState, http20:http20Enabled}"
az webapp config appsettings list -n <app> -g <rg> -o table   # look for plaintext secrets
```
Look for: `httpsOnly=false`, `minTlsVersion<1.2`, `ftpsState=AllAllowed`, no
managed identity, connection strings/secrets in plain app settings. Recommend:
Key Vault references + system-assigned managed identity; HTTPS-only; TLS 1.2; FTPS
disabled.

### Identity, secrets, Defender
```bash
az keyvault list -o table                       # expect empty → recommend creating one
az security secure-score-controls list 2>/dev/null
az security pricing list --query "[].{name:name, tier:pricingTier}" -o table
```
Recommend: introduce Key Vault + managed identity; enable Defender for Cloud plans
on Storage/SQL/App Service if cost allows; review secure-score recommendations.

## Cost (monthly)

```bash
# Current-month actuals by service
az consumption usage list --top 1000 \
  --query "[].{svc:meterDetails.meterCategory, cost:pretaxCost, cur:currency}" -o tsv \
  | awk -F'\t' '{c[$1]+=$2} END{for(k in c) printf "%-28s %8.2f\n", k, c[k]}' | sort -k2 -nr
```
(Or Cost Management → Cost analysis, group by *Service name*, export.)

Run on 2026-08-11 (subscription `projectk-prod-sub` / `e8816a37…`, region Poland
Central). The consumption/cost API returned no line items for this subscription,
so the figures below are **estimated from the actual SKUs** — confirm exact
amounts in Portal → Cost Management → Cost analysis.

| Service | Tier / SKU | Est. monthly (USD) |
|---|---|---|
| App Service plan `ASP-…-99d0` | **B1 Basic**, Linux, 1 instance | ~$13 |
| Azure SQL `projectk-prod-new` | **Basic** (5 DTU, 2 GB) | ~$5 |
| Storage `stprojectkprodnew` | StorageV2 **Standard_LRS**, small | <$1 |
| Static Web App `stapp-…` | **Free** | $0 |
| Application Insights + Log Analytics | pay-per-GB, low volume | ~$0–3 |
| Bandwidth / egress | low at current scale | ~$0–1 |
| **Total** | | **≈ $18–22 / month** |

Cost is dominated by the B1 plan (~$13) + SQL Basic (~$5); everything else is
marginal. Right-sizing: B1 is already the smallest always-on paid tier and is
appropriate; SQL Basic is the floor. The main lever is **Defender plans** — leave
foundational CSPM (free) on and only enable paid Defender-for-X if the posture
warrants the ~$15/resource/month. `alwaysOn` is currently **off** on a paid plan
(cold starts) — turning it on has no extra cost.

## Findings — run 2026-08-11 (severity-ranked)

| # | Severity | Finding | Evidence | Remediation |
|---|---|---|---|---|
| 1 | **High** | All app secrets stored as **plaintext App Service settings**; no Key Vault | `Jwt__Key`, `Email__ApiKey`, `ConnectionStrings__DefaultConnection`, `ConnectionStrings__BlobStorage`, `Telegram__*__BotToken`, `RateLimitBypassKey`, `AdminServiceToken__*` all in app config; `az keyvault list` empty | Create a Key Vault; move secrets to **Key Vault references** (`@Microsoft.KeyVault(...)`); grant the app's **existing system-assigned managed identity** `get` on secrets. Rotate anything that was exposed. |
| 2 | **Medium-High** | SQL firewall **"Allow all Azure services"** rule (`0.0.0.0`) | `AllowAllWindowsAzureIps 0.0.0.0–0.0.0.0` | Remove it; allow only the App Service **outbound IPs**, or move to **VNet integration + Private Endpoint**. Also remove the stale personal dev-machine IP rule when not needed. |
| 3 | **Medium** | **Defender for Cloud not enabled** (`Microsoft.Security` provider unregistered) — no CSPM / secure score | `az security pricing list` → "Subscription Not Registered" | Register `Microsoft.Security`; enable **free foundational CSPM**; consider paid Defender for SQL/Storage/App Service if budget allows (~$15/resource/mo). |
| 4 | **Medium** | **SQL auditing disabled** — no audit trail | `az sql server audit-policy show` → `Disabled` | Enable server auditing to a Log Analytics workspace (one already exists). |
| 5 | **Medium** | SQL **Entra-only auth not enforced** — SQL-password auth still allowed | `administrators.azureADOnlyAuthentication = null` | Consider enforcing Entra-only; at minimum keep the SQL admin credential in Key Vault and rotate. |
| 6 | **Low-Medium** | Storage **shared-key access enabled** and **network default = Allow** | `allowSharedKeyAccess=true`, `networkRuleSet.defaultAction=Allow` | Prefer Entra + managed identity over shared keys (disable shared key once the app uses MI); restrict network or add a Private Endpoint. |
| 7 | **Low** | App Service hardening/perf: `alwaysOn=false`, `http20=false`, FTPS enabled | `az webapp config show` | On a paid plan turn **alwaysOn on** (no extra cost, kills cold starts); enable **HTTP/2**; set **FTPS = Disabled** if unused. |
| 8 | **Info / good** | Positives to keep | TDE **Enabled**; HTTPS-only; **TLS 1.2** on storage/SQL/app; **managed identity present**; storage **public blob access disabled**; SWA Free tier | — |

### Config consistency note (not security)
App config sets `BlobStorage:PublicAccess: true`, but the storage account has
`allowBlobPublicAccess = false`, so anonymous blob URLs won't serve. Verify photo
serving works in prod (via SAS or the API) — the mismatch is either benign or a
latent bug. This also means a media feed (see `feed-and-dashboard.md`) must serve
via SAS/CDN/API, not public URLs.

### Suggested order
1. Key Vault + move secrets + rotate (High).
2. Fix SQL firewall (drop "Allow all Azure services") (Med-High).
3. Register Defender, enable free CSPM; enable SQL auditing (Med).
4. App Service `alwaysOn` + HTTP/2; storage shared-key/network hardening (Low).

---
_Completed live on 2026-08-11 with the user authenticated (`rostyslav.mukha@gmail.com`,
sub `projectk-prod-sub`). All commands read-only; no credentials stored._
