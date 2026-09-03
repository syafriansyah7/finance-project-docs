# Deploy tanpa CC — Koyeb + Neon (Rp0, daftar email saja)

Panduan 10 menit untuk user tanpa kartu kredit / tanpa home lab. Menggantikan Oracle.

## 1. Neon PostgreSQL (tanpa CC)

1. Buka https://neon.tech → Sign up dengan Email/GitHub (tanpa CC)
2. Create Project → Region `Singapore` (ap-southeast-1) → PostgreSQL 16
3. Copy **Connection string (pooled)**: `postgres://user:password@ep-xxx.neon.tech/neondb?sslmode=require`
4. Simpan sebagai `ConnectionStrings__Default`

Neon free: 0.5GB storage, 1 project, cukup untuk single-user finance. Tidak butuh CC untuk free tier.

## 2. Koyeb API (tanpa CC)

1. Buka https://app.koyeb.com → Sign up dengan Email/GitHub (tanpa CC)
2. Create Service → **Import from GitHub** (repo anda) → Builder: **Dockerfile** → Path: `src/Finance.Api/Dockerfile`
3. Context: root repo, Port: **8080**
4. Env vars (wajib):
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=postgres://...neon.tech/neondb?sslmode=require
Jwt__SigningKey=isi-32-karakter-minimal-random-aman!!
```
Generate Jwt key: `openssl rand -base64 32`

5. Deploy → Koyeb akan build + berikan URL `https://xxx.koyeb.app` (auto-HTTPS)
6. Test: `curl https://xxx.koyeb.app/health` → `{"status":"ok"}`
7. Migrasi: Koyeb akan auto-migrate on startup (jika anda tambahkan `dotnet ef database update` di Dockerfile entry), atau jalankan manual via Koyeb Console → Run Command: `dotnet ef database update`

## 3. Mobile & Web config

- MAUI `appsettings.json` / `MauiProgram.cs`: ganti `ApiBaseUrl` → `https://xxx.koyeb.app`
- Blazor Web `deploy/.env.example`: `ApiBaseUrl=https://xxx.koyeb.app`

## 4. Backup Neon → Google Drive (tanpa CC)

Di Koyeb, buat **Cron Job** (daily):

```bash
pg_dump "$ConnectionStrings__Default" | gzip | gpg --symmetric --passphrase "$BACKUP_PASSPHRASE" -o /tmp/finance-$(date +%F).sql.gz.gpg
rclone copy /tmp/finance-*.gpg gdrive:finance-backups/ --config <(echo "$RCLONE_CONF")
```

Rclone Google Drive: `rclone config` sekali di lokal → copy `rclone.conf` ke Koyeb Secret `RCLONE_CONF`.

Retention: Google Drive 15GB free → simpan 7 harian + 4 mingguan.

## 5. Verifikasi (T14-T15)

- HTTPS: Koyeb auto (T14)
- Postgres private: Neon hanya bisa diakses via connection string + SSL, tidak public tanpa password (T15)
- Test offline: ikut `SETUP.md:117` (buat 3 transaksi offline di HP → sync → cek di dashboard Koyeb)

## Troubleshooting

- Koyeb build gagal: cek `src/Finance.Api/Dockerfile` context root, pastikan `Finance.sln` ada di root
- Neon connection `sslmode=require` wajib
- Jika Koyeb free limit 512MB, jangan tambah worker

## Kapan pindah ke Oracle?

Jika nanti punya kartu fisik Mastercard yang lolos, migrasi mudah: `pg_dump` dari Neon → `psql` ke Oracle VPS (portable, `EXIT_PLAN.md:30`).
