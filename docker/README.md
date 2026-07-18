# Project-K dev containers

Run any environment — `dev`, `e2e`, `selfhost`, `tailscale`, `staging`, `prod` —
locally in containers, selected by a single variable. Build the images yourself
or consume a prebuilt one. Images are tagged **locally only and never pushed** to
a registry.

## How it works

- **The .NET API image is environment-agnostic.** One built image runs as any
  environment; the environment is chosen at runtime via `ASPNETCORE_ENVIRONMENT`,
  which selects the matching `appsettings.<Env>.json`. Connection strings, CORS,
  and JWT come from environment variables.
- **The Angular build config is baked per environment** (`NG_CONFIG` build arg:
  `development` / `staging` / `tailscale` / `production`), but the API URL stays
  runtime-injectable (`PROJECTK_API_URL` → `env.js`).
- **SQL Server + Azurite are shared tooling.** They run once
  (`compose.tools.yml`) on the shared `projectk-dev-net` network. Every
  environment's app stack attaches to it and uses its **own database** on the same
  server (`projectK_dev`, `projectK_e2e`, …), so tooling is shared while data
  stays isolated.

```
docker/
  compose.tools.yml         shared SQL + Azurite (run once)
  compose.app.yml           parameterized API + Web (per environment)
  compose.dev.override.yml  hot-reload override (dotnet watch / ng serve)
  env/
    dev.env  e2e.env  selfhost.env  tailscale.env   (committed, local defaults)
    staging.env.example  prod.env.example           (copy → fill secrets)
```

## Quick start

Everything goes through the orchestrator: `scripts/dev.sh` (bash) or
`scripts/dev.ps1` (PowerShell). Both take the same commands.

```bash
# 1. Start the shared tooling once (SQL + Azurite)
./scripts/dev.sh tools up

# 2. Bring up an environment (builds the images on first run)
./scripts/dev.sh up dev --build      # http://localhost:4200  (API 5205)
./scripts/dev.sh up selfhost --build # http://localhost:8080  (API 5215)

# 3. Stop it (add -v to drop volumes)
./scripts/dev.sh down dev
```

```powershell
./scripts/dev.ps1 tools up
./scripts/dev.ps1 up selfhost --build
./scripts/dev.ps1 down selfhost
```

## Commands

| Command | Description |
|---------|-------------|
| `tools up \| down \| logs \| ps` | Manage the shared SQL + Azurite |
| `up <env> [--build\|--pull]`     | Start an environment's app stack |
| `watch <env>`                    | Start with **hot-reload** (bind-mount + `dotnet watch` / `ng serve`) |
| `build <env>`                    | Build the images only, no run |
| `pull <env>`                     | Pull `PROJECTK_*_IMAGE` images |
| `down <env> [-v]`                | Stop the app stack |
| `logs <env> [service]`           | Tail logs |
| `ps <env>`                       | List the stack's containers |

## Environment matrix

| env       | ASPNETCORE_ENVIRONMENT | NG_CONFIG   | Web  | API  | Database            |
|-----------|------------------------|-------------|------|------|---------------------|
| dev       | Development            | development | 4200 | 5205 | projectK_dev        |
| e2e       | E2E                    | development | 4201 | 5206 | projectK_e2e        |
| selfhost  | SelfHost               | production  | 8080 | 5215 | projectK_selfhost   |
| tailscale | Tailscale              | tailscale   | 4210 | 5225 | projectK_tailscale  |
| staging   | Staging                | staging     | 8090 | 5235 | projectK_staging *  |
| prod      | Production             | production  | 8095 | 5245 | projectK_prod *     |

\* `staging` / `prod` ship as `*.env.example`. Copy to `docker/env/<env>.env`
(gitignored) and set real secrets. The defaults point at the local shared tooling
for a config smoke test; swap the connection strings + JWT to target real cloud.

Ports are unique per environment, so multiple stacks can run at once.

## Two ways to run

**Build locally.** Requires a GitHub NuGet token for the private packages during
the API image build — set `PROJECTK_NUGET_AUTH_TOKEN` in the env file (or export
`NUGET_AUTH_TOKEN`), then `./scripts/dev.sh up <env> --build`.

**Consume a prebuilt image.** Set `PROJECTK_API_IMAGE` / `PROJECTK_WEB_IMAGE` in
the env file to a specific tag (e.g. `ghcr.io/iamavasya/projectk-api:0.15.0-beta`),
then `./scripts/dev.sh pull <env>` and `./scripts/dev.sh up <env>` — no local
build or NuGet token needed.

## Hot-reload dev loop

`./scripts/dev.sh watch dev` bind-mounts the source and runs `dotnet watch` +
`ng serve` inside containers, so edits reload live. It runs in the foreground
(Ctrl-C to stop). Do **not** pass `--build` with `watch`: it uses the base
SDK/Node images driven by command, not the production images.

> The native `scripts/start-local.*` loop (no containers) still works and is the
> fastest inner loop if you have the .NET SDK + Node installed on the host.
