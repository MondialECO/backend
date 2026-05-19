# Changelog — `enterprise-hardening` branch

Track of the hardening work taking the backend from initial MVP to
production-grade SaaS. Each phase was build- and test-verified before
commit; Docker artifacts are inspection-verified (no Docker on the dev
machine — exercised in CI and on the VPS).

## Security remediation (prerequisite)

- **`d8a4964`** — remove leaked Mongo/JWT/Zoho secrets from tracked
  config; add `.gitignore`; untrack `bin/`/`obj/`/`.vs/`; add user-secrets
  + `appsettings.Example.json`; fix CI workflow (was triggering on
  `Main` so it had never run).

## Phase 1 — Reliability foundation (`0a8db7a`)

Global exception handler with consistent JSON envelope; correlation-id
middleware (Serilog enriched); Serilog structured logging; fail-fast
startup config validation; health checks (`/health/live`, `/health/ready`
with custom MongoDB + Redis checks).

## Phase 2 — Security hardening (`b9fb368`)

Rate limiting (auth + global); FluentValidation auto-applied via filter;
SecurityHeaders middleware (CSP, X-Frame-Options DENY, nosniff,
Referrer-Policy, Permissions-Policy, set on `OnStarting` to survive
error paths); HSTS outside Development; config-driven CORS; pipeline
ordering hardened.

## Phase 3 — Statelessness (`29cf914`)

SignalR Redis backplane; Redis-backed `IPresenceTracker` (replaces
process-local Dictionary that silently broke horizontal scaling);
DataProtection keys persisted to Redis with fixed application name;
single shared `IConnectionMultiplexer` for cache/backplane/presence/DP;
Redis config externalized.

## Phase 4 — Background email queue (`2f1200f`)

`IEmailQueue` (bounded Channel) + `EmailBackgroundService` with
exponential-backoff retry (4 attempts). Email send moves off the
request path; failures are logged not silently lost.

## Phase 5 — Observability (`86f7936`)

OpenTelemetry traces + metrics (ASP.NET Core, HTTP, runtime
instrumentation); `/metrics` Prometheus endpoint always on; OTLP export
when an endpoint is configured (no noisy errors without a collector);
`IAuditLogger` for security-sensitive events.

## Phase 6 — API versioning & error contract (`cb96b05`)

Non-breaking `Asp.Versioning` (default v1.0 assumed; opt-in via header
or query). Built-in `[ApiController]` model-state 400s now use the
shared `ApiResponse` envelope (single error shape across the API).

## Phase 7 — Test suite + CI (`7fef253`)

xUnit project (12 unit tests + Testcontainers-backed integration
tests). `SkippableFact` auto-skips integration tests without Docker
(locally) and runs them in CI. Test project excluded from web project
globs; `Program` partial-exposed for `WebApplicationFactory`.

## Phase 8 — Dockerized VPS deployment (`2fb98f3`)

Multi-stage Dockerfile (non-root, curl-based HEALTHCHECK,
`WebApp.dll`); `docker-compose.yml` with Traefik (Let's Encrypt TLS,
HTTP→HTTPS redirect, `/health/ready` LB probe, `/metrics` IP-allowlist)
+ scalable api replicas + Redis (auth, persistence, healthcheck); CD
workflow builds → GHCR → SSH deploys; `DEPLOYMENT.md` runbook.

## Phase 9 — Launch readiness (`1f4bf6d`)

`LAUNCH.md` go/no-go checklist (security/runtime/ops); backup strategy
(Atlas PITR + Redis volume implications for DataProtection keys);
rollback pointer; k6 smoke/load test script with SLO thresholds.

## Phase 9.5 — Supply chain (`e8fc235`)

NuGet audit surfaced High/Moderate vulnerabilities. Patched: OpenTelemetry
OTLP exporter → 1.15.3 (`GHSA-4625-4j76-fww9`) + aligned OTel family;
MailKit → 4.16.0 (`GHSA-9j88-vvj5-vhgr`); transitive pins to patched
versions for MessagePack (2.5.187), Microsoft.Bcl.Memory (9.0.14),
Snappier (1.3.1), System.Security.Cryptography.Xml (8.0.3).
SharpCompress (via MongoDB.Driver.Core, no upstream patch) is suppressed
with documented accepted-risk justification (driver↔Atlas compression
only, not untrusted input).

## Phase 10 — Production resilience (`fae12e6`, `80d17a3`)

**MongoDB:** bounded ServerSelection/Connect/Socket timeouts, explicit
pool (10–200), RetryReads/RetryWrites. **Redis:** AbortOnConnectFail
=false + retry/keepalive — no more crash-loop on a Redis blip; the
readiness check still gates traffic. **Kestrel limits:** body, headers,
connections, keep-alive — DoS bounds. **Request timeouts:** 30s default
policy, SignalR hubs opt out. **ForwardedHeaders:** trust only known
networks/proxies — closes X-Forwarded-For spoofing that bypassed the
per-IP rate limiter and poisoned audit logs.

## Phase 11 — CI/CD & supply-chain (`61a4431`)

NuGet package cache; run concurrency w/ cancel-in-progress; explicit
solution restore/build; **security-audit gate** that fails CI on any
High/Critical vulnerable dependency except the documented accepted-risk
advisory. Deploys serialized (no overlap, never cancel in-flight).
Weekly Dependabot for nuget/actions/docker.

## Phase 12 — Operational readiness (`4a6882f`)

`OPERATIONS.md` runbook (SLOs, incident playbook, scaling,
backup/recovery, DR); `ops/prometheus-alerts.yml` (SLO-derived alerts
and runtime alerts).

## Phase 13 — Security round 2 (`a7e079b`)

JWT `ClockSkew` 5min → 30s (post-revocation window 10× smaller).
Audit coverage extended to register / forgot_password / reset_password
/ change_password (success + meaningful failure reasons).
RFC 9116 `/.well-known/security.txt`.

## Phase 14 — Perf polish + build provenance (`8096e61`)

Response compression (Brotli + Gzip) for JSON. `/version` endpoint
reports service / version / commit / buildTime / environment for
incident triage; Dockerfile + CD thread the commit SHA + timestamp so
`/version` reflects the actually-running image.

## Phase 15 — Rate-limit fix + integration tests (`8046e7f`)

**FIX:** the "auth" policy was unpartitioned — 5/min globally meant a
single attacker could lock all users out of `/login`. Converted to a
per-IP partitioned policy. **Tests:** integration tests prove the
middleware actually fires end-to-end (security headers on every
response, correlation id generated/echoed, `/version`,
`/.well-known/security.txt`, `/login` returns 429 after the permit
limit). Each rate-limit-touching test class gets its own `AppFixture`
so state stays isolated.

## Phase 16 — Project entry-point docs

`README.md` + this `CHANGELOG.md`.

## Phase 17 — Real runtime validation

`scripts/smoke-local.ps1` — reusable runtime smoke that boots the app
without external infra (Phase 10's lazy MongoClient + Redis
AbortOnConnectFail=false make this possible) and probes the full public
surface. Two modes:

- `-Mode Run` (Development) — `dotnet build` + `dotnet WebApp.dll`.
- `-Mode Published` (Production) — `dotnet publish` + `dotnet WebApp.dll`,
  which is the **exact execution path the Docker container takes**.

Both modes pass 9/9 probes (health/live, security headers, correlation
id behaviour, /version build metadata, /.well-known/security.txt,
/metrics Prometheus content, response compression negotiation, 404
handling without stack-trace leak, per-IP auth rate-limit firing at the
6th request). This validates the production runtime path end-to-end.

Findings recorded:
- `launchSettings.json` pins `applicationUrl: http://localhost:5093`,
  overriding `ASPNETCORE_URLS` when `dotnet run` is used. Dev-only — not
  used by containers/CI; the smoke script passes `--no-launch-profile`
  internally (older versions did; current version uses `dotnet WebApp.dll`
  directly which doesn't consult launchSettings).
- DataProtection key-ring eager-loads at startup and logs `Key ring
  failed to load during application startup` when Redis is unreachable,
  but the hosted service catches and continues — process stays up
  (Phase 10 resilience confirmed). Keys load lazily once Redis comes
  back; readiness gating prevents user impact. Documented in
  `OPERATIONS.md`.

## Outstanding (owner: project lead)

1. **Rotate leaked credentials + purge git history.** Code/config now
   safe; original old commits still expose the secrets. Rotation is the
   real fix; history purge is cleanup. See LAUNCH.md → outstanding.
2. **First runtime smoke test** against rotated creds + reachable
   Redis + Atlas. Phases 1–7 are build-/test-verified; Docker artifacts
   are inspection-verified. End-to-end run is still pending the infra.
3. **Branch push / PR** — `enterprise-hardening` is local-only.
