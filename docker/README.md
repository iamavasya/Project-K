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
  `development` / `staging` / `tailscale` / `production`), but a few values stay
  runtime-injectable via `env.js`: the API URL (`PROJECTK_API_URL`) and the
  environment name shown in the sidebar badge (`PROJECTK_ENVIRONMENT_NAME`).
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
  nginx/                    frontend nginx config baked into the web image
    projectk-frontend.conf  40-projectk-env.sh
  selfhost/                 self-host release artifacts (packaged into the bundle)
    compose.yml             build-from-source self-host stack
    compose.bundle.yml      prebuilt-image self-host stack (GHCR)
    .env.example            self-host deployment env template
```

The orchestrator scripts live in `scripts/dev.sh` / `scripts/dev.ps1`, with thin
`./dev.sh` / `./dev.ps1` wrappers at the repo root. See the
[Command reference](#command-reference).

## NuGet token (never commit it)

Building the **API** image restores private GitHub NuGet packages, which needs a
GitHub token. It is read from your **host environment variable `NUGET_AUTH_TOKEN`** —
the compose files fall back to it automatically
(`${PROJECTK_NUGET_AUTH_TOKEN:-${NUGET_AUTH_TOKEN:-}}`).

**Do not put the token in `docker/env/*.env`** — those files are committed. Set it
in your shell instead:

```powershell
# PowerShell — current session
$env:NUGET_AUTH_TOKEN = "ghp_your_token"
# …or persist for your user (once)
setx NUGET_AUTH_TOKEN "ghp_your_token"   # reopen the shell afterwards
```

```bash
# bash — add to ~/.bashrc / ~/.profile to persist
export NUGET_AUTH_TOKEN="ghp_your_token"
```

No token at all? Skip building and pull a prebuilt image instead (see
[Two ways to run](#two-ways-to-run)).

## Step-by-step (out of the box)

Prerequisites: **Docker Desktop running**, and `NUGET_AUTH_TOKEN` set in your shell
(previous section) if you build locally.

Everything goes through the orchestrator. From the repo root use the thin
entrypoint `./dev.ps1` (PowerShell) or `./dev.sh` (bash / Git Bash); it forwards to
`scripts/dev.*`, which you can also call directly. All take the same commands and
work from any directory.

Run these from the **repo root**:

```powershell
# 1. Start the shared tooling once per session (SQL + Azurite on projectk-dev-net)
./dev.ps1 tools up

# 2. Build + start an environment. First run is slow (restore + ng build);
#    the API applies EF migrations and seeds on startup automatically.
./dev.ps1 up dev --build         # web http://localhost:4200, API http://localhost:5205/api

# 3. Open the app. Swagger is at http://localhost:5205/swagger for dev.

# 4. Day-to-day
./dev.ps1 logs dev               # tail logs (add a service name to narrow)
./dev.ps1 ps dev                 # container status
./dev.ps1 down dev               # stop (DB data is kept in the shared tools volume)
./dev.ps1 down dev -v            # stop + drop this env's volumes
```

Other environments are the same command with a different name — ports are unique so
they can run in parallel (see the [matrix](#environment-matrix)):

```powershell
./dev.ps1 up selfhost --build    # web http://localhost:8080 — first-run /setup flow
./dev.ps1 up e2e --build         # web http://localhost:4201 (separate projectK_e2e DB)
```

bash equivalent — swap `./dev.ps1` for `./dev.sh`:

```bash
./dev.sh tools up
./dev.sh up dev --build
./dev.sh down dev
```

## Command reference

Two equivalent entrypoints, usable from any directory in the repo:

- **`./dev.ps1 <command>`** (PowerShell) / **`./dev.sh <command>`** (bash / Git Bash) —
  thin repo-root wrappers. Recommended.
- **`./scripts/dev.ps1 <command>`** / **`./scripts/dev.sh <command>`** — the underlying
  scripts the wrappers forward to.

`<env>` is one of: `dev`, `e2e`, `selfhost`, `tailscale`, `staging`, `prod`.

### Shared tooling — run once per session

| Command | What it does |
|---------|--------------|
| `tools up`   | Start SQL Server + Azurite on the shared `projectk-dev-net` network |
| `tools down` | Stop the tooling (data volumes are kept; add `-v` to drop them) |
| `tools logs` | Follow the tooling logs |
| `tools ps`   | List the tooling containers |

### Per-environment app stack — API + Web

| Command | What it does |
|---------|--------------|
| `up <env>`              | Start the env's API + Web using existing images |
| `up <env> --build`      | Build the images first, then start |
| `up <env> --pull`       | Pull images (`--pull always`), then start |
| `watch <env>`           | Start with **hot-reload** (bind-mount + `dotnet watch` / `ng serve`); runs in the foreground, Ctrl-C to stop |
| `build <env>`           | Build the images only, do not start |
| `pull <env>`            | Pull the `PROJECTK_API_IMAGE` / `PROJECTK_WEB_IMAGE` tags |
| `down <env>`            | Stop the env's stack (DB data stays in the shared tooling) |
| `down <env> -v`         | Stop and drop this env's own volumes |
| `logs <env> [service]`  | Follow logs; optionally one service (`projectk-api` / `projectk-web`) |
| `ps <env>`              | List the env's containers |
| `--help`                | Print usage |

Notes:

- `up` and `watch` auto-start the shared tooling and the `projectk-dev-net` network
  if they are not already running — you don't have to run `tools up` first.
- Pass **only** the listed options; never combine `--build` with `watch` (hot-reload
  uses base SDK/Node images driven by command, not the built images).
- Each env runs as its own compose project (`projectk-<env>`) with unique host ports
  (see the [matrix](#environment-matrix)), so several environments can run at once.

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

**Build locally.** Needs `NUGET_AUTH_TOKEN` in your shell (see
[NuGet token](#nuget-token-never-commit-it)), then `./dev.ps1 up <env> --build`.

**Consume a prebuilt image.** Set `PROJECTK_API_IMAGE` / `PROJECTK_WEB_IMAGE` in
the env file to a specific tag (e.g. `ghcr.io/iamavasya/projectk-api:0.15.0-beta`),
then `./dev.ps1 pull <env>` and `./dev.ps1 up <env>` — no local build or NuGet
token needed.

## Hot-reload dev loop

`./dev.ps1 watch dev` bind-mounts the source and runs `dotnet watch` +
`ng serve` inside containers, so edits reload live. It runs in the foreground
(Ctrl-C to stop). Do **not** pass `--build` with `watch`: it uses the base
SDK/Node images driven by command, not the production images.

> The native `scripts/start-local.*` loop (no containers) still works and is the
> fastest inner loop if you have the .NET SDK + Node installed on the host.

## Troubleshooting

- **`Cannot connect to the Docker daemon`** — Docker Desktop isn't running.
- **API build fails on restore / `401 Unauthorized`** — `NUGET_AUTH_TOKEN` isn't set
  in the shell you launched the script from, or the token expired. Re-check
  [NuGet token](#nuget-token-never-commit-it), or use a prebuilt image.
- **Web returns 502 right after start** — the API is still starting / migrating; the
  web container depends on the API healthcheck, give it a few seconds.
- **Reset an environment's database** — `./dev.ps1 down <env> -v`, then `up` again.
  To wipe the shared SQL/Azurite entirely: `./dev.ps1 tools down` then
  `docker volume rm projectk-sql-data projectk-azurite-data`.
