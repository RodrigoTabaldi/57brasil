#!/usr/bin/env bash
set -euo pipefail
umask 077

cd "$(dirname "$0")/.."
env_file="${1:-.env.production}"
if [[ ! -f "$env_file" ]]; then
  echo "Arquivo de configuracao ausente: $env_file" >&2
  exit 1
fi

declare -A settings=()
while IFS='=' read -r key value || [[ -n "$key" ]]; do
  [[ "$key" =~ ^[A-Z][A-Z0-9_]*$ ]] || continue
  settings["$key"]="$value"
done < "$env_file"
for name in SITE_DOMAIN ACME_EMAIL PUBLIC_SITE_URL POSTGRES_PASSWORD POSTGRES_REPLICATION_PASSWORD REDIS_PASSWORD RABBITMQ_PASSWORD; do
  value="${settings[$name]:-}"
  if [[ -z "$value" || "$value" == replace-with-* || "$value" == *exemplo.com.br* ]]; then
    echo "Configure $name em $env_file antes da implantacao." >&2
    exit 1
  fi
done
if [[ "${settings[PUBLIC_SITE_URL]}" != "https://${settings[SITE_DOMAIN]}" ]]; then
  echo "PUBLIC_SITE_URL deve ser https://SITE_DOMAIN (sem barra final)." >&2
  exit 1
fi

compose=(docker compose --env-file "$env_file" -f compose.production.yaml)
"${compose[@]}" config --quiet
"${compose[@]}" build

# Preserve a logical snapshot before applying migrations to an existing database.
if [[ -n "$("${compose[@]}" ps -q db)" ]]; then
  backup_dir="${BACKUP_DIR:-./backups}"
  mkdir -p "$backup_dir"
  chmod 700 "$backup_dir"
  backup_file="$backup_dir/brasil57-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
  partial_file="${backup_file}.partial"
  trap 'rm -f "$partial_file"' EXIT
  "${compose[@]}" exec -T db pg_dump -U brasil57 -d brasil57 | gzip > "$partial_file"
  mv "$partial_file" "$backup_file"
  trap - EXIT
  echo "Backup criado: $backup_file"
fi

"${compose[@]}" run --rm migrate
"${compose[@]}" up -d --no-build
"${compose[@]}" ps
echo "Verifique https://${settings[SITE_DOMAIN]}/health/ready apos o DNS e o certificado TLS estarem ativos."
