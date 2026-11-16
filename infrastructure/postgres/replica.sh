#!/bin/sh
set -eu

until pg_isready -h db -U brasil57 >/dev/null 2>&1; do sleep 2; done
if [ ! -s "$PGDATA/PG_VERSION" ]; then
    mkdir -p "$PGDATA"
    chmod 700 "$PGDATA"
    pg_basebackup -h db -U replicator -D "$PGDATA" -Fp -Xs -R
fi
chmod 700 "$PGDATA"
exec postgres
