#!/usr/bin/env bash
set -euo pipefail
DATE=$(date +%F)
FILE="/tmp/finance-$DATE.sql.gz"
echo "[backup-neon] pg_dump Neon to $FILE"
pg_dump "$ConnectionStrings__Default" | gzip > "$FILE"
echo "[backup-neon] encrypt"
gpg --symmetric --cipher-algo AES256 --batch --passphrase "${BACKUP_PASSPHRASE:?}" -o "$FILE.gpg" "$FILE" && rm "$FILE"
echo "[backup-neon] rclone to Google Drive"
rclone copy "$FILE.gpg" "gdrive:finance-backups/" --progress
find /tmp -name "*.gpg" -mtime +7 -delete || true
echo "[backup-neon] done"
