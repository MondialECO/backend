# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer) using the repo-local NuGet source.
COPY NuGet.config ./
COPY WebApp.csproj ./
RUN dotnet restore WebApp.csproj

# Build & publish only the web project (tests excluded by csproj globs).
COPY . .
RUN dotnet publish WebApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is needed for the container HEALTHCHECK; run as non-root.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .
RUN chown -R appuser /app
USER appuser

# Build provenance, surfaced at /version for incident triage.
ARG BUILD_SHA=local
ARG BUILD_TIME=unknown

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    BUILD_SHA=${BUILD_SHA} \
    BUILD_TIME=${BUILD_TIME}
EXPOSE 8080

# Liveness probe used by Docker; the reverse proxy uses /health/ready.
HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "WebApp.dll"]
