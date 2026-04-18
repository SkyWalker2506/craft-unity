#!/usr/bin/env bash
# Enables craft-unity's repo-local git hooks (pre-commit .meta validator).
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

git -C "$REPO_ROOT" config core.hooksPath .githooks
chmod +x "$SCRIPT_DIR/pre-commit"

echo "✓ hooks installed — pre-commit .meta validation active"
echo "  (disable: git config --unset core.hooksPath)"
