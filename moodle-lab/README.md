# Moodle de laboratorio

Moodle 4.x con datos ficticios que replica el modelo académico de la UTA. Lo levanta `docker-compose.yml` de esta carpeta (TA-01.1, Kevin) como el entorno `aula-virtual`, aparte de EduMonitor (HU-20): ocupa el lugar del Moodle institucional y EduMonitor solo lo conoce por su URL y su token.

## Qué levanta

| Servicio | Imagen | Puerto en el host | Datos persistentes |
|---|---|---|---|
| `moodle-db` | `postgres:16-alpine` | ninguno (solo red interna) | volumen `edumonitor_moodle-db-data` |
| `moodle` | `erseco/alpine-moodle:v4.5.15` (Moodle 4.5 LTS, PHP 8.3) | `${MOODLE_PORT}` (8080) | volumen `edumonitor_moodledata` |

La base de EduMonitor (`sqlserver`, `EduMonitorDB`) ya no está aquí: corre en el entorno de EduMonitor, con el `docker-compose.yml` de la raíz. Moodle usa su propia PostgreSQL.

Los datos van en volúmenes con nombre de Docker y no en carpetas del repo, porque así se evitan los problemas de permisos de PostgreSQL y SQL Server con el disco de Windows. Los volúmenes conservan el nombre `edumonitor_…` del Sprint 1, declarado completo en el compose, para reutilizar los datos. Para verlos: `docker volume ls --filter name=edumonitor_`.

## Requisitos

- Docker Desktop con Docker Compose v2 (`docker compose version`).
- Un `.env` en la raíz del repo, copiado de `.env.example`, con contraseñas propias.

Todos los comandos se ejecutan **desde la raíz del repositorio**.

## Levantar

```powershell
docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d --wait
docker compose -f moodle-lab/docker-compose.yml --env-file .env ps
```

La primera vez Moodle se instala solo y tarda unos minutos. Para seguir el avance:

```powershell
docker compose -f moodle-lab/docker-compose.yml --env-file .env logs -f moodle
```

`moodle-db` y `moodle` deben aparecer como `healthy` en `ps`.

## Entrar a Moodle

- URL: <http://localhost:8080> (o el `MOODLE_PORT` de tu `.env`).
- Usuario y contraseña: `MOODLE_ADMIN_USER` y `MOODLE_ADMIN_PASSWORD` del `.env`.
- Comprobación rápida: `curl.exe -I http://localhost:8080/login/index.php` debe responder `200`.

Las credenciales del administrador solo se aplican en la primera instalación. Si cambias el `.env` después, la contraseña de Moodle no cambia; usa «Borrar todo» o cámbiala dentro de Moodle.

## Conectar SSMS a SQL Server

SQL Server corre en el entorno de EduMonitor (`docker compose up -d` en la raíz) y publica el 1433.

| Campo | Valor |
|---|---|
| Server name | `localhost,1433` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | `MSSQL_SA_PASSWORD` del `.env` |
| Encryption | Mandatory, con **Trust server certificate** marcado |

Desde la terminal, con `sqlcmd`: `sqlcmd -S localhost,1433 -U sa -C -Q "SELECT @@VERSION"` (pide la contraseña).

La API crea `EduMonitorDB` y aplica las migraciones al arrancar.

## Detener

```powershell
# Detiene y quita los contenedores; los volúmenes con los datos se conservan
docker compose -f moodle-lab/docker-compose.yml --env-file .env down
```

Al volver a levantar, Moodle conserva cursos, usuarios y configuración.

## Borrar todo para empezar de cero

Esto elimina Moodle (sitio, cursos, archivos) y su base. `EduMonitorDB` no se toca, porque está en el otro entorno. Después hay que repetir «Restaurar desde cero» y guardar el token nuevo en EduMonitor.

```powershell
# -v borra también los dos volúmenes de Moodle
docker compose -f moodle-lab/docker-compose.yml --env-file .env down -v
docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d
```

## Problemas conocidos

- **Puerto ocupado** (`port is already allocated`): cambia `MOODLE_PORT` en `.env`.
- **La instalación de Moodle falla o el sitio queda con un nombre cortado**: `MOODLE_SITE_NAME` no debe llevar espacios (usa `Moodle-Lab-UTA`), porque el instalador del contenedor no los maneja bien. Después de instalar, el nombre visible se puede cambiar dentro de Moodle.
- **Cambié una contraseña en `.env` y ya no conecta**: PostgreSQL y el administrador de Moodle solo toman sus contraseñas en la primera creación del volumen. Usa «Borrar todo» o cambia la contraseña dentro del servicio.

## Estructura acordada

| Elemento | Valor |
|---|---|
| Categorías | UTA › FISEI › Software |
| Cursos | AGIL-4B «Metodologías Ágiles (lab)» (id 2) · BDD-4B «Bases de Datos (lab)» (id 3) · MCS-4B «Manejo y Configuración del Software (lab)» (id 4, desde el 12 nov 2026) |
| Secciones | Parcial 1: Unidad 1, Unidad 2 · Parcial 2: Unidad 3, Unidad 4 |
| Libro | Exámenes 40 % · Tareas 30 % (incluye talleres) · APE 30 % |
| Parcial 1 | U1: Tarea 1, Tarea 2, APE 1, Taller 1 · U2: Tarea 3, Tarea 4, APE 2, Examen P1 |
| Parcial 2 | U3: Tarea 5, Tarea 6, APE 3, Taller 2 · U4: Tarea 7, Tarea 8, APE 4, Examen P2 |
| Estudiantes | est01 a est32, `estNN@edumonitor.test`, rol student en AGIL-4B y BDD-4B; est01 a est12 en MCS-4B |
| Docentes | doc01 en AGIL-4B y MCS-4B, doc02 solo en BDD-4B, rol editingteacher |

## Completar los cursos con el libro y los datos ficticios

Después de restaurar los cursos base (ver «Restaurar desde cero») y configurar los servicios web (TA-02.1), desde la raíz del repo:

```powershell
docker cp moodle-lab/scripts/cargar_datos_laboratorio.php aula-virtual-moodle-1:/tmp/
docker exec aula-virtual-moodle-1 php /tmp/cargar_datos_laboratorio.php
```

Tarda alrededor de un minuto y medio. Sobre los cursos base agrega el libro 40/30/30, las 16 tareas por curso, doc01, doc02, est01 a est32 y las notas (TA-01.3 a TA-01.5). Un curso que ya tiene tareas no se vuelve a cargar, y las categorías y los usuarios se reutilizan si ya existen. Si los cursos base no están, el script los crea con la misma estructura: AGIL-4B con id 2 y BDD-4B con id 3, que son los ids que esperan las semillas del backend.

El nombre de cada categoría del libro (Exámenes, Tareas, APE) queda también en el `idnumber` de su ítem, porque `gradereport_user_get_grade_items` devuelve `itemname: null` en las categorías y EduMonitor necesita ese nombre. Las unidades 1 a 3 tienen la fecha límite vencida y la unidad 4 vence en noviembre de 2026.

### Curso adicional MCS-4B (12 nov 2026)

MCS-4B «Manejo y Configuración del Software (lab)» tiene la misma estructura que los otros:
- UTA › FISEI › Software;
- 4 unidades en dos parciales;
- libro 40/30/30 y las mismas 16 tareas;
- doc01 como editingteacher y est01 a est12 como estudiantes.

El script lo crea al final, después de AGIL-4B y BDD-4B, con su propia semilla fija (2028). Las semillas de AGIL-4B (2026) y BDD-4B (2027), sus matrículas y su orden no cambian.
- Las fechas de entrega se comparan con una fecha de corte fija, el 1 nov 2026: las unidades 1 a 3 quedan vencidas y la 4 abierta, como en AGIL-4B, sin importar el día de la carga.
- Casos propios:

| Estudiante | Caso |
|---|---|
| est02 | Descenso en la unidad 3 |
| est04 | Atrasos: Tarea 5 y APE 3 |
| est06 | Nota 0 en Tarea 4 |
| est08 | Tarea 6 pendiente de calificar |
| est10 | Promedio bajo |

Para cargarlo solo a él en un Moodle que ya tiene AGIL-4B y BDD-4B, sin tocarlos:

```powershell
docker cp moodle-lab/scripts/cargar_datos_laboratorio.php aula-virtual-moodle-1:/tmp/
docker exec aula-virtual-moodle-1 php /tmp/cargar_datos_laboratorio.php --curso-mcs
```

Imprime `MCS-4B · id 4 · 16 actividades · 12 estudiantes · calificadas 140 (nota 0: 1) · pendientes 5 · sin entrega 47`.
- Si el id no es 4, avisa: EduMonitor espera 4 (`SembradorDatos.MoodleIdMcs`).
- No hace falta ningún paso en EduMonitor. Al arrancar, la API agrega MCS-4B y se lo asigna a doc01 si no existe. El docente lo ve en «Mis cursos» y lo sincroniza con el botón.

> En el Moodle de laboratorio actual, el curso con id 3 se llama `BDD-5B`; EduMonitor lo muestra como BDD-4B porque lo identifica por el id 3. Por eso, en ese Moodle **usa `--curso-mcs`**: sin la opción, el script no encuentra «BDD-4B» por nombre corto y crearía un BDD-4B duplicado con el id 4.

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

| Archivo | Contenido | Autor |
|---|---|---|
| `cursos_base_AGIL-4B.mbz`, `cursos_base_BDD-4B.mbz` | Cursos vacíos con 4 unidades y finalización activada, sin usuarios (TA-01.2) | Sebastián |

El libro, las actividades, los usuarios y las notas no van en un respaldo: los genera `scripts/cargar_datos_laboratorio.php` con una semilla fija, así que siempre salen iguales y no hace falta versionar un `.mbz` con datos de usuarios.

## Restaurar desde cero (TA-01.6)

Con el Moodle recién instalado («Levantar» o «Borrar todo»), desde la raíz del repo:

```powershell
# 1. Servicios web, usuario técnico y token (TA-02.1). Copia el TOKEN_NUEVO a Moodle__Token en .env
docker exec aula-virtual-moodle-1 php admin/cli/cfg.php --name=enablewebservices --set=1
docker exec aula-virtual-moodle-1 php admin/cli/cfg.php --name=webserviceprotocols --set=rest
docker cp moodle-lab/scripts/configurar_servicios_web.php aula-virtual-moodle-1:/tmp/
docker exec aula-virtual-moodle-1 php /tmp/configurar_servicios_web.php

# 2. Categorías UTA › FISEI › Software; imprime «Software=<id>»
docker cp moodle-lab/scripts/cargar_datos_laboratorio.php aula-virtual-moodle-1:/tmp/
docker exec aula-virtual-moodle-1 php /tmp/cargar_datos_laboratorio.php --categorias

# 3. Cursos base de Sebastián en Software. Primero AGIL-4B y después BDD-4B, para que queden con los ids 2 y 3.
docker cp moodle-lab/backups/cursos_base_AGIL-4B.mbz aula-virtual-moodle-1:/tmp/
docker cp moodle-lab/backups/cursos_base_BDD-4B.mbz aula-virtual-moodle-1:/tmp/
docker exec aula-virtual-moodle-1 php admin/cli/restore_backup.php --file=/tmp/cursos_base_AGIL-4B.mbz --categoryid=<id de Software>
docker exec aula-virtual-moodle-1 php admin/cli/restore_backup.php --file=/tmp/cursos_base_BDD-4B.mbz --categoryid=<id de Software>

# 4. Libro, actividades, usuarios y notas
docker exec aula-virtual-moodle-1 php /tmp/cargar_datos_laboratorio.php
```

Atajo: si se omiten los pasos 2 y 3, el paso 4 crea las categorías y los tres cursos con la misma estructura: AGIL-4B con id 2, BDD-4B con id 3 y MCS-4B con id 4.

Comprobación: `dotnet test backend/EduMonitor.sln --filter "FullyQualifiedName~MoodleClientIntegracion"` lee 4 unidades, 16 tareas, 32 estudiantes y el libro 40/30/30 de AGIL-4B.
