#!/usr/bin/env bash
set -euo pipefail
FILE=${1:?usage: restore.sh finance-YYYY-MM-DD.sql.gz.gpg}
echo "[restore] downloading $FILE from Oracle Object Storage"
rclone copy "oracle:finance-backups/$FILE" /tmp/
gpg --decrypt --batch --passphrase "${BACKUP_PASSPHRASE:?}" -o /tmp/restore.sql.gz /tmp/"$FILE"
gunzip -c /tmp/restore.sql.gz | psql -h postgres -U "${POSTGRES_USER:-finance}" -d "${POSTGRES_DB:-finance}"
echo "[restore] done - verify with SELECT count(*) FROM transactions"
