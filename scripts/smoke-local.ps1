# Local runtime smoke test. Boots the app with dummy-but-valid env-var
# config, probes the public surface end-to-end, and tears the process
# down. Designed to require no external infra: MongoClient is lazy and
# Redis uses AbortOnConnectFail=false (Phase 10), so the process starts
# even when Mongo/Redis are unreachable; /health/live then succeeds and
# /health/ready correctly reports unhealthy.
#
# Usage:   pwsh scripts/smoke-local.ps1 [-Port 5099]
# Exit:    0 = all probes passed, 1 = at least one failed

[CmdletBinding()]
param([int]$Port = 5099)

$ErrorActionPreference = 'Stop'
$env:Path = "$env:USERPROFILE\.dotnet;$env:Path"

$base = "http://127.0.0.1:$Port"
$logOut = Join-Path $env:TEMP "smoke-app.out.log"
$logErr = Join-Path $env:TEMP "smoke-app.err.log"
Remove-Item $logOut,$logErr -ErrorAction SilentlyContinue

# --- format-valid dummy config; never connects to real services. ---
$env:ASPNETCORE_URLS        = $base
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:MongoDbSettings__ConnectionString = "mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=2000"
$env:MongoDbSettings__DatabaseName     = "SmokeTest"
$env:JwtSettings__Issuer    = "smoke"
$env:JwtSettings__Audience  = "smoke"
$env:JwtSettings__Key       = ("k" * 48)
$env:EmailSettings__SmtpServer = "smtp.test.local"
$env:EmailSettings__Port    = "587"
$env:EmailSettings__Email   = "smoke@test.local"
$env:EmailSettings__Password = "pw"
$env:Redis__Configuration   = "127.0.0.1:6379,abortConnect=False,connectTimeout=2000,syncTimeout=2000"
$env:Redis__InstanceName    = "Smoke"

# Status-only HTTP probe that never throws on 4xx/5xx — avoids the PS 5.1
# vs PS 7 difference between [WebException] and [HttpResponseException].
function Get-HttpStatus([string]$url, [string]$method = 'GET', [string]$body = $null, [hashtable]$headers = $null) {
    try {
        $params = @{ Uri = $url; Method = $method; UseBasicParsing = $true; TimeoutSec = 15 }
        if ($body)    { $params.Body    = $body; $params.ContentType = 'application/json' }
        if ($headers) { $params.Headers = $headers }
        $r = Invoke-WebRequest @params
        return [int]$r.StatusCode
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { return [int]$resp.StatusCode }
        throw
    }
}

$results = [System.Collections.Generic.List[object]]::new()
function Probe([string]$name, [scriptblock]$body) {
    try {
        & $body
        $results.Add([pscustomobject]@{ name=$name; status='PASS'; detail='' })
        Write-Host ("  PASS  " + $name) -ForegroundColor Green
    } catch {
        $results.Add([pscustomobject]@{ name=$name; status='FAIL'; detail=$_.Exception.Message })
        Write-Host ("  FAIL  " + $name + " :: " + $_.Exception.Message) -ForegroundColor Red
    }
}

Write-Host "Starting WebApp on $base ..."
$p = Start-Process -FilePath dotnet `
    -ArgumentList "run","--no-build","--no-launch-profile","--project","WebApp.csproj","-c","Release" `
    -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput $logOut -RedirectStandardError $logErr

try {
    # --- wait for liveness, generous budget for cold start ---
    $deadline = (Get-Date).AddSeconds(45)
    $alive = $false
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri "$base/health/live" -Method GET -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { $alive = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $alive) {
        Write-Host "App did not become live within 45s. Tail of stderr:" -ForegroundColor Red
        if (Test-Path $logErr) { Get-Content $logErr -Tail 40 }
        if (Test-Path $logOut) { Get-Content $logOut -Tail 40 }
        throw "liveness timeout"
    }
    Write-Host "App is live. Running probes..." -ForegroundColor Cyan

    # ---------- probes ----------

    Probe '/health/live returns 200 + security headers + correlation id' {
        $r = Invoke-WebRequest "$base/health/live" -UseBasicParsing
        if ($r.StatusCode -ne 200) { throw "expected 200, got $($r.StatusCode)" }
        foreach ($h in @('X-Content-Type-Options','X-Frame-Options','Content-Security-Policy','Referrer-Policy','X-Correlation-ID')) {
            if (-not $r.Headers[$h]) { throw "missing header $h" }
        }
        if ($r.Headers['X-Content-Type-Options'] -notmatch 'nosniff') { throw "nosniff missing" }
        if ($r.Headers['X-Frame-Options']        -notmatch 'DENY')    { throw "X-Frame-Options not DENY" }
    }

    Probe 'caller-supplied X-Correlation-ID is echoed' {
        $cid = "smoke-" + [guid]::NewGuid().ToString('N')
        $r = Invoke-WebRequest "$base/health/live" -UseBasicParsing -Headers @{ 'X-Correlation-ID' = $cid }
        if ($r.Headers['X-Correlation-ID'] -ne $cid) { throw "got '$($r.Headers['X-Correlation-ID'])'" }
    }

    Probe '/health/ready correctly reports unhealthy (no Mongo/Redis)' {
        $code = Get-HttpStatus "$base/health/ready"
        if ($code -ne 503) { throw "expected 503, got $code" }
    }

    Probe '/version reports build metadata' {
        $r = Invoke-WebRequest "$base/version" -UseBasicParsing
        if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
        if ($r.Content -notmatch 'MondialBackend') { throw "service name missing" }
        if ($r.Content -notmatch 'environment')    { throw "environment field missing" }
    }

    Probe '/.well-known/security.txt served per RFC 9116' {
        $r = Invoke-WebRequest "$base/.well-known/security.txt" -UseBasicParsing
        if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
        if ($r.Headers['Content-Type'] -notmatch 'text/plain') { throw "wrong content type" }
        if ($r.Content -notmatch 'Contact:' -or $r.Content -notmatch 'Expires:') { throw "missing fields" }
    }

    Probe '/metrics serves Prometheus scrape content' {
        $r = Invoke-WebRequest "$base/metrics" -UseBasicParsing
        if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
        # OpenMetrics/Prometheus exposition format starts with # HELP / # TYPE
        if ($r.Content -notmatch '(?m)^# ') { throw "no Prometheus exposition lines" }
    }

    Probe 'response compression negotiates gzip/br' {
        $r = Invoke-WebRequest "$base/metrics" -UseBasicParsing -Headers @{ 'Accept-Encoding' = 'br, gzip' }
        $enc = $r.Headers['Content-Encoding']
        if (-not $enc) { throw "no Content-Encoding (compression not negotiated)" }
        if ($enc -notmatch '^(br|gzip)$') { throw "unexpected encoding '$enc'" }
    }

    Probe 'unknown route returns 404 (no stack trace leak)' {
        $code = Get-HttpStatus "$base/definitely/not/a/route"
        if ($code -ne 404) { throw "expected 404, got $code" }
    }

    Probe '/api/auth/login per-IP rate limit kicks in at the 6th request' {
        $body = '{"email":"smoke@test.com","password":"DefinitelyWrong1"}'
        $statuses = 1..6 | ForEach-Object { Get-HttpStatus "$base/api/auth/login" 'POST' $body }
        Write-Host ("       login statuses: " + ($statuses -join ',')) -ForegroundColor DarkGray
        if ($statuses[5] -ne 429) { throw "expected 429 at 6th, got $($statuses[5])" }
    }
}
finally {
    Write-Host "Stopping app ..."
    try { if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } } catch {}
    # Also clean up any orphan dotnet WebApp child processes
    Get-CimInstance Win32_Process -Filter "Name='WebApp.exe' OR Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'WebApp' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

$passed = ($results | Where-Object status -eq 'PASS').Count
$failed = ($results | Where-Object status -eq 'FAIL').Count
Write-Host ""
$color = if ($failed -eq 0) {'Green'} else {'Red'}
Write-Host "SMOKE RESULTS: $passed passed, $failed failed" -ForegroundColor $color
if ($failed -gt 0) { exit 1 } else { exit 0 }
