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

> **Applied 2026-08-11:** free remediations #2 (Defender provider registered),
> #4 (SQL auditing → storage), #7 (App Service alwaysOn/HTTP2/FTPS) done, and the
> SQL "Allow all Azure services" rule (#2 in the list) replaced with 32 pinned
> App Service outbound-IP rules. Remaining: Key Vault migration (#1, biggest),
> Cloudflare origin lockdown (#5 commands), storage shared-key/Entra (#5/#6).

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

## Free remediations — ready-to-run (no extra Azure charge)

Run in an authenticated `az` session on sub `projectk-prod-sub`. These are the
zero/near-zero-cost fixes; paid Defender plans and Private Endpoints are excluded.

### 1. App Service hardening (free, safe, reversible)
```bash
az webapp config set -n api-projectk-prod-new -g rg-projectk-prod-paid \
  --always-on true --http20-enabled true --ftps-state Disabled
```

### 2. Defender for Cloud — free foundational CSPM / secure score
```bash
az provider register --namespace Microsoft.Security   # free; secure score appears after ~min
```
Do **not** set any `az security pricing` plan to `Standard` (that's the paid tier).

### 3. SQL firewall — pin App Service outbound IPs, drop "Allow all Azure services"
Add the app's *possible* outbound IPs **first**, then remove the broad rule, so
the app never loses DB access mid-change. On a B1 plan these IPs only change if
the plan moves scale unit — re-run if connectivity breaks.
```bash
RG=rg-projectk-prod-paid; SRV=sql-projectk-server-prod-new
for ip in 134.112.153.11 134.112.153.93 134.112.10.178 74.248.251.169 \
  134.112.153.166 74.248.251.186 74.248.10.11 74.248.79.89 74.248.250.82 \
  134.112.152.45 74.248.251.11 134.112.163.124 74.248.10.93 134.112.163.132 \
  134.112.163.138 134.112.152.71 134.112.8.251 20.215.86.133 134.112.163.142 \
  134.112.9.139 134.112.9.189 134.112.152.121 74.248.251.81 74.248.104.106 \
  134.112.163.241 134.112.153.211 74.248.105.179 74.248.251.197 134.112.11.179 \
  134.112.153.242 20.215.12.4 20.215.12.8; do \
  az sql server firewall-rule create -g $RG -s $SRV -n "appsvc-${ip//./-}" \
    --start-ip-address $ip --end-ip-address $ip -o none; done
az sql server firewall-rule delete -g $RG -s $SRV -n AllowAllWindowsAzureIps
```

### 4. SQL auditing → existing storage account (near-free)
```bash
az sql server audit-policy update -n sql-projectk-server-prod-new \
  -g rg-projectk-prod-paid --state Enabled \
  --blob-storage-target-state Enabled --storage-account stprojectkprodnew
```

### 5. (Cloudflare) lock App Service origin to Cloudflare — free
The app is fronted by Cloudflare. Restrict inbound to Cloudflare's published IP
ranges so the origin can't be reached directly (bypassing WAF/geo-block). Free
via App Service **access restrictions**. Use the current lists from
`https://www.cloudflare.com/ips-v4` / `-v6`. Pattern:
```bash
# repeat --add for each Cloudflare CIDR, ascending priority
az webapp config access-restriction add -n api-projectk-prod-new -g rg-projectk-prod-paid \
  --rule-name cf-<n> --action Allow --ip-address <CLOUDFLARE_CIDR> --priority 100
```
Caution: add **all** Cloudflare ranges before the implicit deny takes effect, and
keep a break-glass rule for your own IP, or you can lock yourself out. Pair with a
shared secret header validated at the origin for defence-in-depth. Verify the
custom domain + health checks still pass after applying.

Not free (deferred): Key Vault migration of secrets (#1 — KV itself ≈ $0 but needs
app changes + redeploy), paid Defender plans, Private Endpoints.

---
_Completed live on 2026-08-11 with the user authenticated (`rostyslav.mukha@gmail.com`,
sub `projectk-prod-sub`). All commands read-only; no credentials stored. State-changing
remediations are listed for the user to apply in their own session._
