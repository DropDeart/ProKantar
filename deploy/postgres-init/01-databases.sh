#!/bin/sh
# İlk açılışta (boş volume) servis veritabanlarını açar; tracking'e PostGIS eklenir. identity_db POSTGRES_DB ile gelir.
set -e
for db in prokantar_kantar_db prokantar_platform_db prokantar_tracking_db; do
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres -c "CREATE DATABASE $db OWNER $POSTGRES_USER;"
done
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname prokantar_tracking_db -c "CREATE EXTENSION IF NOT EXISTS postgis;"
