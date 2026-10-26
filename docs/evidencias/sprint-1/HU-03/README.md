# HU-03 · Modelo de datos con migraciones y datos semilla

**Responsable:** Sebastián Freire · **Estado:** Falta

**Evidencia que pide la planificación (6.3):** captura de la base creada y de los usuarios semilla.

| Archivo | Quién lo sube | Contenido |
|---|---|---|
| `HU-03_ssms_tablas.png` | Sebastián | SSMS conectado a `localhost,1433` con `EduMonitorDB` y sus tablas |
| `HU-03_usuarios_semilla.png` | Sebastián | `SELECT NombreUsuario, Rol FROM Usuario;` con admin, doc01 y doc02 |
| `HU-03_diccionario_datos.md` | Jeremy (TA-03.5) | Descripción de cada tabla y columna |

Base actual: las migraciones `Inicial_Identidad`, `Matriculados`, `Actividades_Calificaciones` y `Sincronizaciones` (PR #3 y #4) crean 8 tablas y las semillas se cargan al arrancar la API en Development. Lo que falta del Diagrama 5 está en el comentario de Kevin en el [PR #3](https://github.com/Balseca-Kevin/edumonitor-ai/pull/3).
