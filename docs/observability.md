# Production Observability

The API sends ASP.NET Core telemetry to the Application Insights resource configured by
`APPLICATIONINSIGHTS_CONNECTION_STRING`. The same resource receives Serilog traces through
`Serilog__WriteTo__1__Args__connectionString`.

Both settings should be Key Vault references in the production App Service. Do not put the
connection string or instrumentation key in source control or in KQL queries.

## First checks after deployment

Run a request against `/health`, then wait briefly for ingestion and run:

```kusto
requests
| where timestamp > ago(30m)
| summarize Requests=count(), Failed=countif(success == false), P95=percentile(duration, 95)
```

The API should produce `requests` and `dependencies` in addition to `traces`. Exceptions should
appear in `exceptions` when an unhandled request failure occurs.

## Useful queries

### Failed requests

```kusto
requests
| where timestamp > ago(24h)
| where success == false or toint(resultCode) >= 400
| project timestamp, resultCode, duration, name, url, operation_Id
| order by timestamp desc
```

### Exceptions

```kusto
exceptions
| where timestamp > ago(24h)
| project timestamp, type, outerMessage, problemId, operation_Id
| order by timestamp desc
```

### Password reset and invitation flow

```kusto
union isfuzzy=true
(
    requests
    | where timestamp > ago(7d)
    | where url has_any ("password-reset", "invitation/resend")
    | project timestamp, itemType="request", name, resultCode, success, operation_Id
),
(
    traces
    | where timestamp > ago(7d)
    | where message has_any ("PasswordReset", "Invitation", "RequestPasswordReset")
    | project timestamp, itemType="trace", name="", resultCode="", success="", operation_Id, message
)
| order by timestamp desc
```

### Correlate one request

Use `operation_Id` from a request, trace, or exception to inspect the full flow:

```kusto
union isfuzzy=true requests, dependencies, exceptions, traces
| where operation_Id == "<operation-id>"
| project timestamp, itemType, name, target, resultCode, success, message, type
| order by timestamp asc
```

## Access

The Application Insights resource must have query network access enabled. A user querying logs
needs at least `Monitoring Reader` on the Application Insights resource or its resource group.
The API's managed identity is unrelated to a human user's portal query permissions; it only needs
access to Key Vault for the connection-string references.
