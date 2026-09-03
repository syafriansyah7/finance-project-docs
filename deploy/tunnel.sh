#!/usr/bin/env bash
set -e
export ASPNETCORE_URLS=http://localhost:5000
export ASPNETCORE_ENVIRONMENT=Production
: "${ConnectionStrings__Default:?set Neon URL first}"
: "${Jwt__SigningKey:?set 32+ chars}"

echo "[tunnel] starting API on http://localhost:5000 ..."
dotnet run --project "$(dirname "$0")/../src/Finance.Api" --no-launch-profile &
API_PID=$!
sleep 5
curl -s http://localhost:5000/health | grep -q ok && echo "[tunnel] API ok" || (echo "[tunnel] API failed" && kill $API_PID && exit 1)

echo "[tunnel] starting cloudflared..."
cloudflared tunnel --url http://localhost:5000
kill $API_PID
