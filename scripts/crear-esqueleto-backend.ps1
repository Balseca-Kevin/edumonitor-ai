# Crea la solución .NET 8 por capas en backend/. Se ejecuta una sola vez, desde la raíz del repo:
#   powershell -ExecutionPolicy Bypass -File scripts/crear-esqueleto-backend.ps1
$ErrorActionPreference = "Stop"

# Ejecuta un comando dotnet y corta el script si falla
function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "Falló: dotnet $($args -join ' ')" }
}

# 1. Comprobar que existe un SDK 8.0 antes de tocar nada
$sdks = & dotnet --list-sdks 2>$null
if (-not ($sdks | Where-Object { $_ -match '^8\.0\.' })) {
    Write-Host "No hay SDK de .NET 8 instalado. SDKs encontrados:" -ForegroundColor Red
    $sdks | ForEach-Object { Write-Host "  $_" }
    Write-Host "Instálalo con:  winget install Microsoft.DotNet.SDK.8" -ForegroundColor Yellow
    Write-Host "o desde https://dotnet.microsoft.com/en-us/download/dotnet/8.0 y vuelve a correr este script."
    exit 1
}

if (-not (Test-Path "backend")) { New-Item -ItemType Directory backend | Out-Null }
if (Test-Path "backend/EduMonitor.sln") { Write-Host "backend/EduMonitor.sln ya existe. No se hace nada."; exit 0 }

Push-Location backend
try {
    # Fija el SDK 8.0 aunque haya .NET 9 o 10 instalados
    Invoke-Dotnet new globaljson --sdk-version 8.0.100 --roll-forward latestFeature --force

    Invoke-Dotnet new sln -n EduMonitor
    Invoke-Dotnet new webapi   -n EduMonitor.Api            -o EduMonitor.Api            -f net8.0 --use-controllers
    Invoke-Dotnet new classlib -n EduMonitor.Application    -o EduMonitor.Application    -f net8.0
    Invoke-Dotnet new classlib -n EduMonitor.Domain         -o EduMonitor.Domain         -f net8.0
    Invoke-Dotnet new classlib -n EduMonitor.Infrastructure -o EduMonitor.Infrastructure -f net8.0
    Invoke-Dotnet new xunit    -n EduMonitor.Tests          -o EduMonitor.Tests          -f net8.0

    # Borra los archivos de ejemplo de las plantillas
    Remove-Item EduMonitor.Application/Class1.cs, EduMonitor.Domain/Class1.cs, EduMonitor.Infrastructure/Class1.cs -ErrorAction SilentlyContinue
    Remove-Item EduMonitor.Api/Controllers/WeatherForecastController.cs, EduMonitor.Api/WeatherForecast.cs -ErrorAction SilentlyContinue
    Remove-Item EduMonitor.Api/EduMonitor.Api.http -ErrorAction SilentlyContinue

    Invoke-Dotnet sln EduMonitor.sln add EduMonitor.Api EduMonitor.Application EduMonitor.Domain EduMonitor.Infrastructure EduMonitor.Tests

    # Dependencias entre capas
    Invoke-Dotnet add EduMonitor.Application    reference EduMonitor.Domain
    Invoke-Dotnet add EduMonitor.Infrastructure reference EduMonitor.Application EduMonitor.Domain
    Invoke-Dotnet add EduMonitor.Api            reference EduMonitor.Application EduMonitor.Infrastructure
    Invoke-Dotnet add EduMonitor.Tests          reference EduMonitor.Api EduMonitor.Application EduMonitor.Domain EduMonitor.Infrastructure

    Invoke-Dotnet build EduMonitor.sln
    Write-Host "Esqueleto creado y compilado." -ForegroundColor Green
}
catch {
    Write-Host $_ -ForegroundColor Red
    Write-Host "El esqueleto quedó incompleto. Borra la carpeta backend (salvo AGENTS.md y CLAUDE.md) y vuelve a correr el script." -ForegroundColor Yellow
    exit 1
}
finally { Pop-Location }
