@echo off
chcp 65001 >nul
title EduMonitor AI - Apagado

rem Detiene EduMonitor y el aula virtual sin borrar sus datos (volumenes).

cd /d "%~dp0.."

echo Deteniendo EduMonitor (web, api, sqlserver, agente, ollama)...
docker compose down

echo Deteniendo el aula virtual (Moodle)...
docker compose -f moodle-lab\docker-compose.yml --env-file .env down

echo.
echo Listo. No uses "down -v": borra la base, el token de Moodle, las llaves y los modelos.
pause
