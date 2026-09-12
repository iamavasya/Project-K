#Requires -Version 5.1
# Repo-root entrypoint for the dev-container orchestrator.
# Lets you run `./dev.ps1 <command>` from the repository root instead of
# `./scripts/dev.ps1 <command>`. All arguments are forwarded unchanged.
# See docker/README.md for the full command reference.
& "$PSScriptRoot/scripts/dev.ps1" @args
exit $LASTEXITCODE
