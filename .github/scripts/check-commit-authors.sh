#!/usr/bin/env bash
# Fail when a commit or the pull request text credits an AI agent.
# Author, committer, and trailer values are matched on known agent names and
# emails. The message and the pull request text are matched on those same
# names, on "Generated with", and on the robot emoji. The bare word "cursor"
# in a sentence is not a match.
#
# CI sets EVENT_NAME, BASE_SHA, HEAD_SHA, and on pull_request also PR_TITLE
# and PR_BODY. Locally:
#   EVENT_NAME=push BASE_SHA=main HEAD_SHA=HEAD bash .github/scripts/check-commit-authors.sh

set -euo pipefail

# Identity fields only (author, committer, trailer value). A token "cursor"
# here is the Cursor agent, not a sentence about a text cursor.
IDENTITY_RE='cursoragent@cursor\.com|\bcursoragent\b|@cursor\.com\b|cursor\[bot\]|cursor[[:space:]]+agent|^[[:space:]]*cursor[[:space:]]*$|\bclaude\b|\banthropic\b|\bopenai\b|\bchatgpt\b|\bcodex\b|\bcopilot\b|\bgrok\b|\bgemini\b|\bdevin\b|\bcursor\b'

# Prose: commit message, pull request title, pull request body.
# No bare "cursor". Agent links and the CURSOR_AGENT marker count.
BODY_RE='cursoragent@cursor\.com|\bcursoragent\b|@cursor\.com\b|cursor\[bot\]|cursor[[:space:]]+agent|cursor_agent|cursor\.com/agents|cursor\.com/background-agent|\bgenerated[[:space:]]+with\b|\bclaude\b|\banthropic\b|\bopenai\b|\bchatgpt\b|\bcodex\b|\bcopilot\b|\bgrok\b|\bgemini\b|\bdevin\b'

failures=0

fail() {
  printf 'error: %s\n' "$*"
  printf '::error::%s\n' "$*"
  failures=$((failures + 1))
}

is_agent_identity() {
  printf '%s\n' "$1" | grep -Eiq -e "$IDENTITY_RE"
}

# Trailer lines ("Co-authored-by:", "Signed-off-by:", and anything else ending in -by),
# from the trailer block and from any line that is shaped like one.
trailer_lines() {
  {
    printf '%s\n' "$1" | grep -E '^[A-Za-z][A-Za-z0-9-]*-by:' || true
    printf '%s\n' "$1" | git interpret-trailers --parse || true
  } | awk 'NF && !seen[$0]++'
}

check_identity() {
  local where="$1"
  local text="$2"

  if is_agent_identity "$text"; then
    fail "$where is an AI-agent attribution: $text"
  fi
}

check_prose() {
  local where="$1"
  local text="$2"
  local hit line key value

  hit="$(printf '%s\n' "$text" | grep -Ei -e "$BODY_RE" | head -n 3 || true)"
  if [ -n "$hit" ]; then
    fail "$where contains an AI-agent marker: ${hit//$'\n'/ | }"
  fi

  if printf '%s' "$text" | grep -Fq '🤖'; then
    fail "$where contains an AI-agent marker: robot emoji"
  fi

  while IFS= read -r line; do
    [ -n "$line" ] || continue
    key="${line%%:*}"
    value="${line#*:}"
    value="${value#"${value%%[![:space:]]*}"}"

    if printf '%s' "$key" | grep -Eiq 'generated'; then
      fail "$where has an AI-agent trailer: $line"
      continue
    fi

    if is_agent_identity "$value"; then
      fail "$where trailer attributes the change to an AI agent: $line"
    fi
  done < <(trailer_lines "$text")
}

# The hosting platform wraps the submitted pull request text in these comments
# and appends a footer after the closing one. That wrapper is not part of the
# text under review. Drop it, then scan what was submitted. A marker inside the
# submitted text still fails.
submitted_text() {
  local text="$1"
  local line
  local skipping=0

  while IFS= read -r line || [ -n "$line" ]; do
    if [ "$line" = "<!-- CURSOR_AGENT_PR_BODY_BEGIN -->" ]; then
      continue
    fi
    if [ "$line" = "<!-- CURSOR_AGENT_PR_BODY_END -->" ]; then
      skipping=1
      continue
    fi
    if [ "$skipping" -eq 1 ]; then
      continue
    fi
    printf '%s\n' "$line"
  done <<< "$text"
}

check_commit() {
  local sha="$1"
  local author_name author_email committer_name committer_email message

  author_name="$(git log -1 --format='%an' "$sha")"
  author_email="$(git log -1 --format='%ae' "$sha")"
  committer_name="$(git log -1 --format='%cn' "$sha")"
  committer_email="$(git log -1 --format='%ce' "$sha")"
  message="$(git log -1 --format='%B' "$sha")"

  check_identity "commit ${sha} author name" "$author_name"
  check_identity "commit ${sha} author email" "$author_email"
  check_identity "commit ${sha} committer name" "$committer_name"
  check_identity "commit ${sha} committer email" "$committer_email"
  check_prose "commit ${sha} message" "$message"
}

EVENT_NAME="${EVENT_NAME:-local}"
BASE_SHA="${BASE_SHA:-${1:-}}"
HEAD_SHA="${HEAD_SHA:-${2:-HEAD}}"
PR_TITLE="${PR_TITLE:-}"
PR_BODY="${PR_BODY:-}"

if [ -z "$BASE_SHA" ]; then
  printf 'error: BASE_SHA is empty\n' >&2
  exit 2
fi

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  printf 'error: not inside a git repository\n' >&2
  exit 2
fi

commits=()
if printf '%s' "$BASE_SHA" | grep -Eq '^0+$'; then
  printf 'No previous tip; checking %s only.\n' "$HEAD_SHA"
  commits+=("$HEAD_SHA")
else
  if ! git cat-file -e "${BASE_SHA}^{commit}" >/dev/null 2>&1; then
    git fetch --no-tags origin "$BASE_SHA"
  fi
  if ! git cat-file -e "${HEAD_SHA}^{commit}" >/dev/null 2>&1; then
    git fetch --no-tags origin "$HEAD_SHA"
  fi
  while IFS= read -r sha; do
    [ -n "$sha" ] && commits+=("$sha")
  done < <(git rev-list --reverse "${BASE_SHA}..${HEAD_SHA}")
fi

printf 'Checking %s commit(s) in %s..%s\n' "${#commits[@]}" "$BASE_SHA" "$HEAD_SHA"
for sha in "${commits[@]}"; do
  check_commit "$sha"
done

if [ "$EVENT_NAME" = "pull_request" ]; then
  check_prose "pull request title" "$(submitted_text "$PR_TITLE")"
  check_prose "pull request body" "$(submitted_text "$PR_BODY")"
fi

if [ "$failures" -gt 0 ]; then
  printf 'Found %s AI-agent attribution problem(s).\n' "$failures"
  exit 1
fi

printf 'No AI-agent attribution found.\n'
