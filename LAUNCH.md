# MVP Launch Readiness

## Go / No-Go checklist

**Security (blocking)**
- [ ] Leaked MongoDB / JWT / Zoho credentials **rotated** on the
      external services.
- [ ] Git history **purged** of the old secrets (filter-repo/BFG) and
      force-pushed, or repo treated as compromised and re-created.
- [ ] `.env` on the VPS has rotated values, `chmod 600`, not in git.
- [ ] Strong, random 256-bit `JwtSettings__Key`.

**Runtime (blocking)**
- [ ] End-to-end smoke test passed against rotated creds + real
      MongoDB + Redis (the run we still owe — see below).
- [ ] `https://APP_DOMAIN/health/ready` returns 200 (Mongo + Redis OK).
- [ ] TLS certificate issued by Let's Encrypt (valid chain, auto-renew).
- [ ] Login / register / password-reset happy paths work.

**Operations**
- [ ] CI green on `main`; deploy workflow secrets configured.
- [ ] MongoDB Atlas automated backups enabled (PITR if available).
- [ ] Prometheus scraping `/metrics` from an internal host;
      `ops/prometheus-alerts.yml` rules loaded and wired to on-call.
- [ ] Log output shipped/retained somewhere durable.
- [ ] On-call has read `OPERATIONS.md` (runbook, SLOs, recovery).
- [ ] Load test meets thresholds (see below).

## Backups & data durability

- **MongoDB (source of truth):** enable Atlas continuous backups /
  point-in-time recovery. This is the only data that must be backed up.
- **Redis:** holds cache, presence, SignalR backplane, and the
  **DataProtection key ring**. `appendonly yes` (set in compose) persists
  it across restarts. If the Redis volume is lost, issued auth/reset and
  antiforgery tokens are invalidated → users simply re-authenticate. No
  business data is lost. Back up the `redis-data` volume if you want to
  avoid forced re-logins on disaster recovery.

## Dependency security

`dotnet restore` runs NuGet audit. All direct and transitive packages
with available patches are pinned to fixed versions (OTLP exporter,
MailKit, MessagePack, Microsoft.Bcl.Memory, Snappier,
System.Security.Cryptography.Xml).

**Accepted risk:** `SharpCompress` (transitive via `MongoDB.Driver.Core`)
— advisory GHSA-6c8g-7p36-r338, **no upstream patch exists**. Used only
for compression on the trusted app↔MongoDB Atlas connection, never on
untrusted/user-supplied archives, so the vector is not reachable.
Suppressed via `<NuGetAuditSuppress>` with justification in the csproj.
Re-evaluate when SharpCompress ships a fix.

## Load test

```
k6 run -e BASE_URL=https://APP_DOMAIN loadtest/smoke.js
```
Thresholds: <1% errors, p95 < 800ms. Raise the `target` VUs to your
expected peak before launch and re-run; scale replicas with
`docker compose up -d --scale api=N` if p95 degrades.

## Rollback

See `DEPLOYMENT.md` → Rollback. In short: redeploy a known-good image
sha via `APP_IMAGE`.

## Still outstanding (owner: you)

1. Rotate credentials + purge git history — the original urgent item.
2. Provide rotated creds + a reachable Redis so the first real
   end-to-end runtime smoke test can be run (code is build-verified
   through Phase 7; Docker artifacts are inspection-verified).
