#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${ConnectionStrings__Default:-}" ]]; then
  echo "ConnectionStrings__Default ausente." >&2
  exit 1
fi
webhook_enabled="${Payment__EnableWebhook:-false}"
if [[ "${webhook_enabled,,}" == "true" ]]; then
  echo "Webhook de pagamento indisponivel nesta implantacao sem worker." >&2
  exit 1
fi

cleanup() {
  trap - EXIT TERM INT
  kill "${api_pid:-}" "${web_pid:-}" "${proxy_pid:-}" 2>/dev/null || true
  wait 2>/dev/null || true
}
trap cleanup EXIT TERM INT

(cd /app/api && exec dotnet Brasil57.Api.dll) & api_pid=$!
(cd /app/storefront && PORT=3000 exec node server.js) & web_pid=$!
caddy run --config /app/Caddyfile --adapter caddyfile & proxy_pid=$!
wait -n "$api_pid" "$web_pid" "$proxy_pid"
