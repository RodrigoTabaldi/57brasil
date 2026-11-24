#!/usr/bin/env bash
set -euo pipefail
umask 077

sha="${SSH_ORIGINAL_COMMAND:-}"
[[ "$sha" =~ ^[0-9a-f]{40}$ ]] || { echo 'Invalid commit' >&2; exit 1; }
exec 9>/run/lock/brasil57-deploy.lock
flock -n 9 || { echo 'Deployment already running' >&2; exit 1; }
repo=/root/57brasil-github
env_file=/root/brasil57/.env.production
cd "$repo"
git fetch origin main
[[ "$(git rev-parse refs/remotes/origin/main)" == "$sha" ]] || { echo 'Commit is no longer main' >&2; exit 1; }
git checkout --detach "$sha"
compose=(docker compose -p brasil57-vps --env-file "$env_file" -f compose.vps.yaml)
"${compose[@]}" config --quiet
"${compose[@]}" --profile migration build
mkdir -p /root/brasil57/backups
backup="/root/brasil57/backups/pre-deploy-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
"${compose[@]}" exec -T db pg_dump -U brasil57 -d brasil57 | gzip > "$backup"
test -s "$backup"
"${compose[@]}" run --rm migrate
"${compose[@]}" up -d --no-build
for attempt in {1..30}; do
  if curl -fsS --max-time 5 https://57brasil.com.br/health/ready >/dev/null && curl -fsS --max-time 5 https://57brasil.com.br/ >/dev/null; then
    echo "Deployment healthy: $sha"
    exit 0
  fi
  sleep 2
done
echo 'Health check failed; investigate deployment and database migration' >&2
exit 1
