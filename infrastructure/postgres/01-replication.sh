#!/bin/sh
set -eu

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=replication_password="$POSTGRES_REPLICATION_PASSWORD" <<'SQL'
CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD :'replication_password';
SQL

printf '\nhost replication replicator samenet scram-sha-256\n' >> "$PGDATA/pg_hba.conf"
