#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [[ -z "${REPO_ROOT:-}" ]]; then
  echo "REPO_ROOT is not set. Example: export REPO_ROOT=/path/to/repos"
  exit 1
fi

docker compose up --build -d
