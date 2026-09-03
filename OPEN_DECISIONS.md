# Open Decisions

This document contains decisions intentionally left open. It prevents an AI agent from silently treating a draft assumption as a final project rule.

## Product

- [ ] Final application name
- [ ] Android-only v1 or future iOS target
- [ ] Exact starter categories
- [ ] Budget period rules
- [ ] Receipt/attachment scope for v1

## Technology

- [ ] Exact .NET SDK / MAUI version to pin
- [ ] Exact SQLite provider/library
- [ ] Authentication implementation
- [ ] Token lifetime/refresh strategy
- [ ] Exact sync cursor implementation

## Operations

- [x] Backup destination — **Oracle Object Storage Always Free (20 GB)** via rclone/oci cli
- [x] Backup retention — **daily 7 days + weekly 4 weeks**
- [x] Reverse proxy — **Caddy** (auto-HTTPS)
- [x] Deployment automation — **manual v1; optional Gitea Actions v2 (FOSS, self-hosted)**
- [x] DNS/domain — **Rp0 murni: DuckDNS / sslip.io** (paid domain + Cloudflare DNS is optional upgrade, not required)
- [x] Oracle Cloud region — **ap-singapore-1 priority (closest to Indonesia), fallback ap-tokyo-1 / ap-mumbai-1**; instance sizing = Ampere A1 Always Free (verify capacity before provisioning)

## Governance

- [ ] Final license choice
- [ ] Copyright holder name

## Rule

When an implementation needs one of these decisions, the AI agent should first reference this document and the user context. For reversible local prototyping, use the simplest option and mark it clearly as temporary rather than silently promoting it to a permanent architecture decision.
