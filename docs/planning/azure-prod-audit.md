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

Fill this table from the run:

| Service | Tier / SKU | Est. monthly |
|---|---|---|
| App Service plan | | |
| Azure SQL | | |
| Storage (blob) | | |
| Static Web Apps | | |
| Application Insights | | |
| Bandwidth / egress | | |
| **Total** | | |

Right-sizing notes: check App Service plan tier vs. actual CPU/memory, SQL
DTU/vCore utilization, App Insights ingestion volume (sampling), and Storage
egress (a CDN can cut repeat-read egress).

## Findings (fill during live run — severity-ranked)

| # | Severity | Finding | Evidence | Remediation |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |

---
_This document is completed live with the user authenticated; nothing here
requires or stores Azure credentials._
