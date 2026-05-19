# Deployment (Docker on a VPS)

Stateless API replicas behind Traefik (TLS via Let's Encrypt), sharing
Redis. MongoDB is external (Atlas). Scales with `--scale api=N`.

## One-time VPS setup

1. Install Docker Engine + compose plugin. Open ports 80 and 443.
2. Create the app directory and copy deployment files:
   ```
   sudo mkdir -p /opt/mondial && cd /opt/mondial
   # copy docker-compose.yml from this repo
   cp .env.example .env   # then edit with ROTATED secrets
   chmod 600 .env
   ```
3. Point DNS: an `A` record for `APP_DOMAIN` → the VPS IP (required
   before first run so Let's Encrypt can issue the certificate).
4. Add GitHub repo secrets: `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`.

## First deploy

```
cd /opt/mondial
docker compose up -d                       # traefik + redis
docker compose up -d --scale api=3 api     # 3 API replicas
docker compose ps
curl -fsS https://APP_DOMAIN/health/ready  # expect 200
```

After this, every push to `main` that passes the build/test workflow
auto-builds the image and redeploys via `.github/workflows/deploy.yml`.

## Scaling & zero-downtime

- Scale: `docker compose up -d --scale api=5 api`. Traefik only routes to
  replicas passing `/health/ready`, so new replicas take traffic once
  warm and old ones drain.
- Redis is required: a replica that cannot reach Redis fails readiness
  and is kept out of rotation (by design — see Phase 3).

## Rollback

```
docker compose pull api          # or set APP_IMAGE to a known-good sha
APP_IMAGE=ghcr.io/<repo>:<good-sha> docker compose up -d --scale api=3 api
```

## Notes

- `/metrics` is restricted to private IP ranges at Traefik; scrape it
  from an internal Prometheus, not the public internet.
- Secrets live only in `/opt/mondial/.env` (chmod 600) — never in git.
