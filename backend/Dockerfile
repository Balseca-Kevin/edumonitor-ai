# API de EduMonitor (ASP.NET Core 8). Imagen en dos etapas: compila con el SDK y corre solo con el runtime.
# Se construye desde docker-compose.yml de la raíz (contexto: backend/). Ningún secreto entra a la imagen:
# las variables llegan del entorno del contenedor.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS compilacion
WORKDIR /src

# Primero los proyectos, para que la restauración quede en caché mientras no cambien las dependencias.
COPY global.json ./
COPY EduMonitor.Domain/EduMonitor.Domain.csproj EduMonitor.Domain/
COPY EduMonitor.Application/EduMonitor.Application.csproj EduMonitor.Application/
COPY EduMonitor.Infrastructure/EduMonitor.Infrastructure.csproj EduMonitor.Infrastructure/
COPY EduMonitor.Api/EduMonitor.Api.csproj EduMonitor.Api/
RUN dotnet restore EduMonitor.Api/EduMonitor.Api.csproj

COPY . .
RUN dotnet publish EduMonitor.Api/EduMonitor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# curl solo para el healthcheck de Docker Compose (GET /api/health).
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=compilacion /app .

# Dentro de Docker la API escucha en el puerto 5000 de todas las interfaces de la red interna.
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "EduMonitor.Api.dll"]
