@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
title EduMonitor AI - Inicio

rem Levanta los dos entornos, en orden: el aula virtual (Moodle) y EduMonitor con su agente. Luego abre http://localhost:5173.

set "ESPERA=%SystemRoot%\System32\timeout.exe"
cd /d "%~dp0.."

if not exist ".env" (
  echo [ERROR] Falta el archivo .env en la raiz del repositorio. Copia .env.example y completalo.
  pause
  exit /b 1
)

docker info >nul 2>&1
if not errorlevel 1 goto docker_listo
echo [1/3] Iniciando Docker Desktop...
start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
set /a intentos=0
:esperar_docker
"%ESPERA%" /t 3 /nobreak >nul 2>&1
docker info >nul 2>&1
if not errorlevel 1 goto docker_listo
set /a intentos+=1
if !intentos! geq 60 (
  echo [ERROR] Docker no respondio en 3 minutos.
  pause
  exit /b 1
)
goto esperar_docker
:docker_listo

echo [2/3] Aula virtual: Moodle de laboratorio en http://localhost:8080 ...
docker compose -f moodle-lab\docker-compose.yml --env-file .env up -d --wait
if errorlevel 1 (
  echo [ERROR] El aula virtual no arranco. Revisa: docker compose -f moodle-lab\docker-compose.yml --env-file .env logs
  pause
  exit /b 1
)

echo [3/3] EduMonitor con su agente en http://localhost:5173 ...
docker compose up -d --build --wait
if errorlevel 1 (
  echo [ERROR] EduMonitor no arranco. Revisa: docker compose logs
  pause
  exit /b 1
)

start "" http://localhost:5173
echo Listo. EduMonitor en http://localhost:5173
"%ESPERA%" /t 5 /nobreak >nul 2>&1
