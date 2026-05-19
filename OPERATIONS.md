# Operations Runbook

Operational reference for running the Mondial backend in production
(Dockerized, stateless replicas behind Traefik, Redis-backed, MongoDB
Atlas). Pair this with `DEPLOYMENT.md` (how to deploy) and `LAUNCH.md`
(go/no-go).

## Service level objectives

| SLO | Target |
|-----|--------|
| Availability (monthly) | 99.9% |
| API latency p95 | < 800 ms |
| API latency p99 | < 1500 ms |
| 5xx error rate | < 0.5% of requests |
| Successful deploy → healthy | < 5 min |

Error budget burn drives prioritization: if availability drops below
target, reliability work preempts feature work.

## Health & observability surface

| Endpoint | Use |
|----------|-----|
| `GET /health/live` | Liveness — process/pipeline responding (no deps) |
| `GET /health/ready` | Readiness — MongoDB + Redis reachable |
| `GET /metrics` | Prometheus scrape (private networks only) |

- Every response carries `X-Correlation-ID`; every log line is tagged
  with it. To trace a user-reported issue, get the id from the response
  and filter logs by `CorrelationId`.
- Security-sensitive actions are on the `Audit` log category
  (`AUDIT <action> actor=… success=… ip=… correlationId=…`).

## Incident playbook

### 1. App down / 5xx spike
1. `docker compose ps` — replicas running? Restarting?
2. `curl -fsS https://APP_DOMAIN/health/ready` — which dependency?
3. `docker compose logs --tail=200 api` — filter by `CorrelationId`,
   look for `Unhandled exception` / `Application terminated`.
4. If a bad deploy: roll back (see `DEPLOYMENT.md` → Rollback).
5. If load: scale `docker compose up -d --scale api=N api`.

### Note on DataProtection at startup

The DataProtection key ring is persisted to Redis (so all replicas share
it). The framework's hosted service eagerly tries to load the ring at
startup; if Redis is unreachable at that exact moment it logs `Key ring
failed to load during application startup` and continues — the app does
**not** crash (Phase 10 resilience). Once Redis is reachable, keys load
lazily on first use. Readiness is gated on Redis, so the reverse proxy
holds traffic until then; the practical impact is just an extra log line
during cold/rolling deploys when Redis warms after the API.

### 2. Redis unreachable
- Symptom: `/health/ready` 503, replicas out of rotation, SignalR
  fan-out degraded. App does **not** crash-loop (AbortOnConnectFail
  =false) and auto-recovers when Redis returns.
- Check `docker compose logs redis`; `docker compose restart redis`.
- Data loss impact: cache/presence/backplane rebuild automatically;
  DataProtection key ring loss → users must re-authenticate (no data
  loss). Volume `redis-data` persists across restarts.

### 3. MongoDB (Atlas) degraded
- Symptom: `/health/ready` 503 on the mongodb check, slow/failing
  writes. Driver retries reads/writes and times out fast (5s server
  selection) rather than hanging threads.
- Check Atlas status/metrics; verify Network Access allowlist and that
  the connection string secret is valid; check Atlas alerts.

### 4. Replica crash-loop on startup
- Almost always config: `StartupConfigValidation` refuses to start with
  missing/weak secrets. `docker compose logs api` shows exactly which
  key. Fix `/opt/mondial/.env`, redeploy.

### 5. TLS certificate not issued / renew failing
- `docker compose logs traefik` for ACME errors. Verify DNS A record →
  VPS IP and ports 80/443 open. Let's Encrypt rate limits: wait/retry.

### 6. High latency, no errors
- Check `/metrics`: request duration histogram, GC, thread pool.
- Scale replicas; check Atlas/Redis latency; inspect slow endpoints by
  correlation id.

## Scaling

- Horizontal: `docker compose up -d --scale api=N api`. App is
  stateless; Traefik load-balances and only routes to replicas passing
  `/health/ready`.
- Vertical: increase VPS resources; raise Kestrel
  `MaxConcurrentConnections` / Mongo `MaxConnectionPoolSize` if metrics
  show saturation.

## Backup & recovery

**MongoDB (only stateful business data):**
- Enable Atlas continuous/cloud backups with point-in-time recovery.
- Restore drill (do before launch and quarterly): restore a snapshot to
  a temp cluster, point a staging app at it, verify reads/writes.

**Redis:** ephemeral by design. `appendonly yes` + `redis-data` volume
persists across restarts. To survive host loss without forcing re-login,
snapshot the volume:
`docker run --rm -v mondial_redis-data:/d -v $PWD:/b alpine tar czf /b/redis.tgz -C /d .`

**Secrets:** stored only in `/opt/mondial/.env` (chmod 600). Keep an
offline copy in your password manager; they are not in git.

**Disaster recovery (VPS lost):**
1. New VPS → install Docker, open 80/443.
2. Restore `/opt/mondial/{docker-compose.yml,.env}` and (optionally)
   the Redis volume snapshot.
3. Point DNS to the new IP; `docker compose up -d --scale api=3`.
4. MongoDB is external (Atlas) — unaffected.

## Alerting

Prometheus rules in `ops/prometheus-alerts.yml`. Wire to your
Alertmanager/on-call. At minimum page on: target down, readiness
failing, 5xx burn, p99 SLO breach.
