# Backend EduMonitor · reglas de arquitectura

Monolito modular por capas (estilo Clean Architecture). La API expone controladores REST que devuelven JSON; no hay vistas ni Razor. La interfaz vive aparte en `frontend/` (React).

## Proyectos y qué va en cada uno

| Proyecto | Contiene | No contiene |
|---|---|---|
| `EduMonitor.Domain` | Entidades (`Actividad`, `ActividadTema`, `Alerta`, `Auditoria`, `Calificacion`, `ContenidoMinimo`, `Conversacion`, `Curso`, `CursoDocente`, `Estudiante`, `Matricula`, `ReglaAlerta`, `Sincronizacion`, `TareaRefuerzo`, `Tema`, `TrazaHerramienta`, `Unidad`, `Usuario`), enums (`EstadoCalificacion`, `EstadoSincronizacion`, `RolSistema`) | Referencias a EF Core, ASP.NET, HttpClient |
| `EduMonitor.Application` | Interfaces (`IMoodleClient`, `IJwtService`, `IHasherContrasenas`, repositorios o `IEduMonitorDbContext`), servicios de caso de uso (`AuthService`, `CursoService`, `SincronizacionService`), DTOs de entrada y salida, validaciones | Código de EF Core, llamadas HTTP, controladores |
| `EduMonitor.Infrastructure` | `EduMonitorDbContext`, configuraciones Fluent API, migraciones, semillas, `MoodleClient` (HttpClient tipado), implementación de JWT y hashing, `DependencyInjection.cs` con `AddInfrastructure()` | Lógica de negocio, controladores |
| `EduMonitor.Api` | `Program.cs`, controladores delgados en `Controllers/`, políticas de autorización (`CursoAutorizado`), manejo de errores a `ProblemDetails`, Swagger, CORS para `http://localhost:5173` | Consultas a la base o a Moodle directas |
| `EduMonitor.Tests` | xUnit: unitarias de servicios, pruebas de permisos 200/401/403 con `WebApplicationFactory`, prueba de integración con Moodle | Datos reales |

Dependencias: Api → Application, Infrastructure · Infrastructure → Application, Domain · Application → Domain · Domain → ninguna.

## Convenciones

- Un controlador por recurso: `AuthController`, `CursosController`, `SincronizacionController`, `ConfiguracionMoodleController`, `ToolsController`. El controlador valida entrada, llama a un servicio de Application y traduce el resultado a HTTP. Sin lógica de negocio.
- Configuración con el patrón Options: `MoodleOptions` (sección `Moodle`), `JwtOptions` (sección `Jwt`), cadena `ConnectionStrings:EduMonitorDB`. En desarrollo los valores llegan desde el `.env` de la raíz del repo, cargado con el paquete DotNetEnv al iniciar en `Development`. En CI llegan como variables de entorno.
- La autorización por curso se implementa una sola vez como política (`IAuthorizationHandler` con el id del curso como recurso) y se aplica a todos los endpoints `/api/cursos/{id}/*`.
- `IMoodleClient` tiene un método por función de Moodle permitida y devuelve DTOs propios, no el JSON crudo. El token se lee de `MoodleOptions` y se enmascara en cualquier log (`••••` + últimos 4).
- Escritura idempotente en la sincronización: upsert por id de Moodle con índices únicos.
- Contraseñas con hash (ASP.NET Core Identity `PasswordHasher<T>` o BCrypt), nunca en texto plano.

## Comandos

```
dotnet build backend/EduMonitor.sln
dotnet test backend/EduMonitor.sln
dotnet run --project backend/EduMonitor.Api
dotnet ef migrations add <Nombre> --project backend/EduMonitor.Infrastructure --startup-project backend/EduMonitor.Api
dotnet ef database update --project backend/EduMonitor.Infrastructure --startup-project backend/EduMonitor.Api
```

## Sprint 2

- Controladores nuevos: `IndicadoresController` (HU-09), `AlertasController` con reglas y alertas (HU-12), `AgenteController` con `POST /api/agente/chat` (HU-14), `EstadoController` con `GET /api/admin/estado` (HU-20). `ToolsController` suma `calificaciones` y `alertas` (HU-13).
- Servicios nuevos en Application: `Indicadores/IndicadorService`, `Alertas/` (una clase por regla: R1, R2, R4), `Agente/AgenteService` con `IAgenteClient` (HttpClient tipado en Infrastructure).
- Los cálculos de indicadores y alertas viven solo en Application y se prueban con casos conocidos. Ningún controlador calcula.
- Token de servicio para el agente: rol `AGENTE`, claims `docente` y `curso`, 2 minutos. La política `CursoAutorizado` toma el docente del claim cuando el rol es `AGENTE`.
- La configuración de Moodle se guarda en `ConfiguracionMoodle` con el token protegido por Data Protection; las llaves en `/keys` (volumen `api-keys`).
- Al cierre del Sprint 2 no están en `develop` `AlertasController`, la carpeta `Alertas/` ni `GET /api/tools/alertas` (HU-12 y TA-13.2).
- Dentro de Docker la API escucha en `http://+:5000` y usa `Server=sqlserver,1433`. Moodle está en otro entorno (`moodle-lab/`): la API lo llama por `Moodle__Url` = `http://localhost:8080`; dentro de Docker la API usa la red del servicio `moodle-puente`, que lleva ese puerto a `host.docker.internal:8080`, porque Moodle redirige toda llamada que no use su `SITE_URL`.
