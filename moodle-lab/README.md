# Moodle de laboratorio

Moodle 4.x con datos ficticios que replica el modelo académico de la UTA. Lo levanta `docker-compose.yml` de esta carpeta (TA-01.1, Kevin).

## Qué levanta

| Servicio | Imagen | Puerto en el host | Datos persistentes |
|---|---|---|---|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | volumen `edumonitor_sqlserver-data` |
| `moodle-db` | `postgres:16-alpine` | ninguno (solo red interna) | volumen `edumonitor_moodle-db-data` |
| `moodle` | `erseco/alpine-moodle:v4.5.15` (Moodle 4.5 LTS, PHP 8.3) | `${MOODLE_PORT}` (8080) | volumen `edumonitor_moodledata` |

`sqlserver` es la base de EduMonitor (`EduMonitorDB`), no la de Moodle. Moodle usa su propia PostgreSQL.

Los datos van en volúmenes con nombre de Docker y no en carpetas del repo, porque así se evitan los problemas de permisos de PostgreSQL y SQL Server con el disco de Windows. El prefijo `edumonitor_` sale de `COMPOSE_PROJECT_NAME`. Para verlos: `docker volume ls --filter name=edumonitor_`.

## Requisitos

- Docker Desktop con Docker Compose v2 (`docker compose version`).
- Un `.env` en la raíz del repo, copiado de `.env.example`, con contraseñas propias. La de SQL Server necesita mayúscula, minúscula, número, símbolo y al menos 8 caracteres.

Todos los comandos se ejecutan **desde la raíz del repositorio**.

## Levantar

```powershell
docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
docker compose -f moodle-lab/docker-compose.yml --env-file .env ps
```

La primera vez Moodle se instala solo y tarda unos minutos. Para seguir el avance:

```powershell
docker compose -f moodle-lab/docker-compose.yml --env-file .env logs -f moodle
```

`sqlserver` y `moodle-db` deben aparecer como `healthy` en `ps`.

## Entrar a Moodle

- URL: <http://localhost:8080> (o el `MOODLE_PORT` de tu `.env`).
- Usuario y contraseña: `MOODLE_ADMIN_USER` y `MOODLE_ADMIN_PASSWORD` del `.env`.
- Comprobación rápida: `curl.exe -I http://localhost:8080/login/index.php` debe responder `200`.

Las credenciales del administrador solo se aplican en la primera instalación. Si cambias el `.env` después, la contraseña de Moodle no cambia; usa «Borrar todo» o cámbiala dentro de Moodle.

## Conectar SSMS a SQL Server

| Campo | Valor |
|---|---|
| Server name | `localhost,1433` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | `MSSQL_SA_PASSWORD` del `.env` |
| Encryption | Mandatory, con **Trust server certificate** marcado |

Desde la terminal, con `sqlcmd`: `sqlcmd -S localhost,1433 -U sa -C -Q "SELECT @@VERSION"` (pide la contraseña).

La base `EduMonitorDB` no existe hasta que el backend aplique las migraciones (HU-03).

## Detener

```powershell
# Detiene y quita los contenedores; los volúmenes con los datos se conservan
docker compose -f moodle-lab/docker-compose.yml --env-file .env down
```

Al volver a levantar, Moodle conserva cursos, usuarios y configuración, y SQL Server conserva sus bases.

## Borrar todo para empezar de cero

Esto elimina Moodle (sitio, cursos, archivos) **y** las bases de SQL Server, incluida `EduMonitorDB`.

```powershell
# -v borra también los tres volúmenes
docker compose -f moodle-lab/docker-compose.yml --env-file .env down -v
docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
```

## Problemas conocidos

- **Puerto ocupado** (`port is already allocated`): cambia `MOODLE_PORT` en `.env`, o detén el SQL Server local de Windows si ocupa el 1433.
- **`sqlserver` no pasa a `healthy`**: casi siempre la contraseña no cumple la política. Revisa `docker compose -f moodle-lab/docker-compose.yml --env-file .env logs sqlserver`.
- **La instalación de Moodle falla o el sitio queda con un nombre cortado**: `MOODLE_SITE_NAME` no debe llevar espacios (usa `Moodle-Lab-UTA`), porque el instalador del contenedor no los maneja bien. Después de instalar, el nombre visible se puede cambiar dentro de Moodle.
- **Cambié una contraseña en `.env` y ya no conecta**: PostgreSQL y el administrador de Moodle solo toman sus contraseñas en la primera creación del volumen. Usa «Borrar todo» o cambia la contraseña dentro del servicio.

## Estructura acordada

| Elemento | Valor |
|---|---|
| Categorías | UTA › FISEI › Software |
| Cursos | AGIL-4B «Metodologías Ágiles (lab)» · BDD-4B «Bases de Datos (lab)» |
| Secciones | Parcial 1: Unidad 1, Unidad 2 · Parcial 2: Unidad 3, Unidad 4 |
| Libro | Exámenes 40 % · Tareas 30 % (incluye talleres) · APE 30 % |
| Parcial 1 | U1: Tarea 1, Tarea 2, APE 1, Taller 1 · U2: Tarea 3, Tarea 4, APE 2, Examen P1 |
| Parcial 2 | U3: Tarea 5, Tarea 6, APE 3, Taller 2 · U4: Tarea 7, Tarea 8, APE 4, Examen P2 |
| Estudiantes | est01 a est32, `estNN@edumonitor.test`, rol student en ambos cursos |
| Docentes | doc01 solo en AGIL-4B, doc02 solo en BDD-4B, rol editingteacher |

## Cargar los cursos y los datos ficticios

Después de configurar los servicios web (TA-02.1), desde la raíz del repo:

```powershell
docker cp moodle-lab/scripts/cargar_datos_laboratorio.php edumonitor-moodle-1:/tmp/
docker exec edumonitor-moodle-1 php /tmp/cargar_datos_laboratorio.php
```

Tarda alrededor de un minuto y medio. Si los cursos ya existen, no hace nada. Crea la estructura de la tabla anterior con AGIL-4B como id 2 y BDD-4B como id 3, que son los ids que esperan las semillas del backend. Las unidades 1 a 3 tienen la fecha límite vencida y la unidad 4 vence en noviembre de 2026.

Casos de prueba en AGIL-4B (uno por regla de alerta):

| Estudiante | Caso |
|---|---|
| est03 | Descenso: alto en las unidades 1 y 2, bajo en la 3 |
| est05 | Nota 0 calificada en Tarea 3 (distinta de sin entrega) |
| est07 | Atrasos: Tarea 5, Tarea 6 y APE 3 vencidas sin entrega |
| est09 | Entregó Tarea 6 y Taller 2; pendientes de calificar |
| est11 | Promedio bajo (menor a 7,0) |
| est13 | Alejado del grupo |
| est15 | Baja finalización: casi nada entregado en las unidades 2 y 3 |
| est17 | Temática crítica: bajo en toda la unidad 3 |

Los demás estudiantes tienen notas entre 6 y 10 con alguna entrega faltante. Los datos se generan con una semilla fija, así que cada carga produce lo mismo.

## Respaldos (carpeta `backups/`)

1. `cursos_base.mbz`: categorías y cursos vacíos (Sebastián).
2. `cursos_estructura.mbz`: libro, actividades y usuarios (Santiago).
3. `curso_final.mbz`: con calificaciones y casos de prueba (Jeremy).

## Restaurar desde cero

Pendiente (TA-01.6).
