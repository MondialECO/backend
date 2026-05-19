# Mondial Backend

Production .NET 8 backend for the Mondial SaaS — stateless, horizontally
scalable, Docker-deployed, MongoDB + Redis backed.

## Stack

| | |
|---|---|
| Runtime | .NET 8 (ASP.NET Core) |
| Data | MongoDB Atlas |
| Cache / SignalR backplane / presence / DataProtection keys | Redis |
| Auth | ASP.NET Identity + JWT bearer (HS256, 30s clock skew) |
| Real-time | SignalR with Redis backplane |
| Observability | Serilog (correlation-id enriched) + OpenTelemetry traces/metrics + Prometheus scrape at `/metrics` |
| Deploy | Docker on a VPS behind Traefik (Let's Encrypt TLS), GHCR-pushed images, GitHub Actions CD |

## Architecture (high level)

```
Internet ─► Traefik (TLS, HTTP→HTTPS, /metrics IP-allowlist, LB w/ /health/ready probe)
            │
            ├─► api replica 1  ┐
            ├─► api replica 2  ├─► Redis  (backplane / cache / presence / DP keys)
            └─► api replica N  ┘
                       │
                       └─► MongoDB Atlas
```

Stateless replicas — scale with `docker compose up -d --scale api=N`.

## Documentation

| File | Purpose |
|---|---|
| `DEPLOYMENT.md` | One-time VPS setup, first deploy, scaling, rollback |
| `OPERATIONS.md` | SLOs, incident playbook, backup/recovery, alerts |
| `LAUNCH.md` | Pre-launch go/no-go checklist + load test |
| `CHANGELOG.md` | Phase-by-phase history of the hardening track |
| `appsettings.Example.json` | Required configuration keys (env-var driven) |
| `ops/prometheus-alerts.yml` | Prometheus alerting rules |

## Configuration

The app **refuses to start** without required secrets (`StartupConfigValidation`).
Supply them via:

- **Dev:** `dotnet user-secrets set "<Section:Key>" "..."` (per `appsettings.Example.json`).
- **Prod:** environment variables in `/opt/mondial/.env` (`Section__Key` form), `chmod 600`.

**Never commit real secrets.** `appsettings.json` ships with empty placeholders.

## Build / test / run

```
dotnet restore WebApp.sln
dotnet build   WebApp.sln -c Release
dotnet test    WebApp.sln -c Release        # 12 unit tests
# Integration tests use Testcontainers (Mongo+Redis) — run automatically
# in CI; locally they SkippableFact-skip without Docker.
```

Local run requires user-secrets set + a reachable Redis + MongoDB.

### Runtime smoke test (no external infra)

```
pwsh scripts/smoke-local.ps1                 # Mode=Run  - dotnet build + WebApp.dll, Development
pwsh scripts/smoke-local.ps1 -Mode Published # dotnet publish + WebApp.dll, Production
                                             #   (the exact execution path the Docker container uses)
```

Boots the app with format-valid dummy config and probes the public
surface end-to-end (health, /version, /.well-known/security.txt,
/metrics, security headers, correlation-id, response compression,
404 handling, per-IP rate limit). Requires no external Mongo/Redis;
9 probes pass in either mode.

## Deploy

See `DEPLOYMENT.md`. CI builds + tests on every push; CD builds an image
on `main`, pushes to GHCR, and SSH-deploys to the VPS via
`.github/workflows/deploy.yml`.

## Security

- All secrets env/user-secrets driven; none in tracked files.
- Per-IP rate limiting (auth: 5/min/IP; global: 100/min/IP).
- Security headers (CSP, X-Frame-Options DENY, nosniff, Referrer-Policy,
  Permissions-Policy) + HSTS in prod.
- ForwardedHeaders trusted only from known networks/proxies (anti-spoof).
- JWT clock skew 30s, ASP.NET Identity password policy.
- FluentValidation on all auth inputs; envelope-consistent 400s.
- Audit log on every credential lifecycle event.
- Vulnerability gate in CI (`dotnet list package --vulnerable`).
- `/.well-known/security.txt` for responsible disclosure.

## Observability

Every response carries `X-Correlation-ID`; every log line is tagged with it.
Filter logs / traces / audits by that id to follow a single user request.
