param([switch]$SkipBuild,[switch]$WithDatabase)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
Push-Location (Join-Path $projectRoot 'mobile/rutasmart')
try {
    if (!$SkipBuild) {
        & (Join-Path $PSScriptRoot 'flutter.ps1') build web --release --dart-define=API_URL=
        if ($LASTEXITCODE -ne 0) { throw 'Falló el build Flutter.' }
    }
} finally { Pop-Location }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_WEBROOT = Join-Path $projectRoot 'mobile/rutasmart/build/web'
if ($WithDatabase) {
    $line=Get-Content (Join-Path $projectRoot '.env') | Where-Object { $_.StartsWith('POSTGRES_PASSWORD=') } | Select-Object -First 1
    if (!$line) { throw 'Configurá POSTGRES_PASSWORD en .env.' }
    $env:ConnectionStrings__Database='Host=localhost;Port=55438;Database=rutasmart;Username=rutasmart;Password='+$line.Substring(18)
}
Write-Host 'RutaSmart local: http://localhost:5080 — detener con Ctrl+C'
& dotnet run --project (Join-Path $projectRoot 'src/RutaSmart.Api') --no-launch-profile --urls http://localhost:5080
