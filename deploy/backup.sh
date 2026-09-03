#!/usr/bin/env bash
set -euo pipefail
DATE=$(date +%F)
BACKUP_DIR=${BACKUP_DIR:-/backups}
FILE="$BACKUP_DIR/finance-$DATE.sql.gz"
mkdir -p "$BACKUP_DIR"
echo "[backup] pg_dump to $FILE"
pg_dump -h postgres -U "${POSTGRES_USER:-finance}" -d "${POSTGRES_DB:-finance}" | gzip > "$FILE"
echo "[backup] encrypting"
gpg --symmetric --cipher-algo AES256 --batch --passphrase "${BACKUP_PASSPHRASE:?}" -o "$FILE.gpg" "$FILE" && rm "$FILE"
echo "[backup] rclone to Oracle Object Storage"
rclone copy "$FILE.gpg" "oracle:finance-backups/" --progress
echo "[backup] retention 7d+4w"
find "$BACKUP_DIR" -name "*.gpg" -mtime +7 -delete
rclone delete "oracle:finance-backups/" --min-age 30d 2>/dev/null || true
echo "[backup] done $FILE.gpg"
