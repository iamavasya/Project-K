#!/usr/bin/env bash
# Repo-root entrypoint for the dev-container orchestrator.
# Lets you run `./dev.sh <command>` from the repository root instead of
# `./scripts/dev.sh <command>`. All arguments are forwarded unchanged.
# See docker/README.md for the full command reference.
exec "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/scripts/dev.sh" "$@"
