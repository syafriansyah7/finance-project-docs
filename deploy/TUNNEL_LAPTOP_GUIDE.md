# Tunnel tanpa CC — Laptop + Cloudflare Tunnel (5 menit)

Jalan tanpa Oracle, tanpa Koyeb/Render, tanpa kartu, tanpa home lab baru. Cukup laptop yang sudah ada.

## 1. Install cloudflared (sekali)

**Windows (laptop dashboard):**
```powershell
winget install --id Cloudflare.cloudflared
```

**Linux / Bluefin:**
```bash
brew install cloudflared
# atau
sudo rpm-ostree install cloudflared
```

Cek: `cloudflared --version`

## 2. Jalankan API di laptop

```bash
cd /path/to/finance-project-docs
# set env (jangan commit)
export ConnectionStrings__Default="postgresql://neondb_owner:npg_5lCRbz4nJsZD@ep-jolly-wildflower-b31dj0qh-pooler.c-4.ap-southeast-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"
export Jwt__SigningKey="Bni2026TunnelLaptop32CharsRandom!!"
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000

dotnet run --project src/Finance.Api --no-launch-profile
# cek: curl http://localhost:5000/health -> {"status":"ok"}
```

## 3. Buka Tunnel (di terminal kedua, tanpa CC)

```bash
cloudflared tunnel --url http://localhost:5000
```

Output akan ada URL:
```
https://finance-trycloudflare-com-xxx.trycloudflare.com
```

Itu URL publik HP akan pakai. Copy URL tersebut.

## 4. HP & Dashboard config

- MAUI `MauiProgram.cs` / `appsettings.json`: ganti `ApiBaseUrl` → `https://xxx.trycloudflare.com`
- Blazor Web `src/Finance.Web/Program.cs`: `ApiBaseUrl` → sama

Test HP: buka `https://xxx.trycloudflare.com/health` di browser HP → ok

## 5. Backup (tetap Neon → Google Drive)

Tunnel tidak butuh backup terpisah — Neon tetap di cloud. Backup via:
```bash
pg_dump "postgresql://neondb_owner:...@ep-...neon.tech/neondb?sslmode=require" | gzip | gpg --symmetric --passphrase "$BACKUP_PASSPHRASE" -o finance-$(date +%F).sql.gz.gpg
rclone copy finance-*.gpg gdrive:finance-backups/
```

## Catatan

- Tunnel `trycloudflare.com` gratis, tanpa CC, tanpa daftar, tapi URL ganti tiap restart. Untuk URL tetap: `cloudflared tunnel create` + `cloudflared tunnel route dns` (butuh domain Cloudflare free, tetap tanpa CC — daftar cloudflare.com dengan email saja).
- Laptop tidak perlu 24 jam nyala — nyalakan saat mau sync, dashboard bisa diakses saat tunnel nyala.
- Jika laptop mati, HP tetap bisa pakai offline queue (SyncQueue) → sync nanti saat laptop nyala lagi.
