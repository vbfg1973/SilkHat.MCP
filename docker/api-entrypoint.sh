#!/usr/bin/env sh
set -eu

if command -v git >/dev/null 2>&1; then
  git config --global --add safe.directory '*' || true
  if [ -d /repos ]; then
    git config --global --add safe.directory /repos || true
  fi
fi

exec dotnet SilkHat.Api.dll
