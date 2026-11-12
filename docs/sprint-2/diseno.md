# Sistema de diseño de EduMonitor · diseño D (Sprint 2)

Decisión de Kevin del 12 nov 2026, el día de la Review, aplicada a las 08:14: la interfaz cambia al **diseño D**.
- Reemplaza al estilo suizo con bento grid que se había definido el 8 nov.
- Se construye sobre el PR #35 de Santiago (HU-19). HU-19 sigue siendo suya; los componentes base, la tira del período y el manejo de 401 y 403 son de él.

Mockups de referencia, que no se sirven ni se copian:
- `docs/sprint-2/mockups/MK-19_inicio-sesion.dc.html`
- `docs/sprint-2/mockups/MK-09_panel.dc.html`
- `docs/sprint-2/mockups/MK-14_asistente.dc.html`

Motivo del cambio: el Product Owner vio genérico el inicio de sesión del Sprint 1 y pidió que la herramienta hable como un docente. El diseño D pone las cifras en lenguaje de aula, mueve la atención a quién apoyar y por qué, y hace del asistente un chat como los que el docente ya usa.

## Dónde vive

| Archivo | Qué tiene |
|---|---|
| `frontend/src/styles/tokens.css` | Único lugar con colores, fuentes, tamaños, espacios, radios y tiempos. Reemplaza al bloque `.s2` y a la paleta del Sprint 1 |
| `frontend/src/styles/app.css` | Clases de todas las pantallas; todo valor sale de `tokens.css` |
| `frontend/src/components/ui/ui.css` | Componentes base de Santiago (botón, campo, tarjeta, tabla, insignia, aviso) con los tokens del diseño D |
| `frontend/src/components/lenguaje.js` | Textos en lenguaje docente: «Frente al curso», «Últimas notas», evidencia de alertas, «Datos usados», unidad actual |
| `frontend/src/components/movimiento.js` | Cifras que cuentan una vez y detección de `prefers-reduced-motion` |

Ningún componente escribe un color, un tamaño o un espacio literal fuera de esos archivos. Hay dos excepciones, que son datos:
- las posiciones de los puntos decorativos del inicio de sesión;
- el ancho de las barras.

## Tipografía

- **Archivo**, de 300 a 800 (`@fontsource-variable/archivo`, OFL), para todo.
  - El nombre del sistema combina pesos: «Edu» en 300 y «Monitor» en 800.
  - Las cifras grandes van en 900 con espaciado −0,04 em.
- **JetBrains Mono** (`@fontsource-variable/jetbrains-mono`, OFL) solo para los códigos de curso (`AGIL-4B`) y datos técnicos menores, como el sello del inicio, la marca 7,0 o el período.
- Se quitaron `@fontsource/fraunces`, `@fontsource/ibm-plex-sans` e `@fontsource/ibm-plex-mono` de `package.json`.
- Cifras tabulares en toda la aplicación (`font-variant-numeric: tabular-nums`).
- Escala en px: 11, 12, 13, 14, 15, 16, 20, 24, 30, 34, 44, 60, 92.

## Color

Sin negro puro y sin ámbar intenso.

### Inicio de sesión (oscuro)

| Token | Valor | Uso |
|---|---|---|
| `--oscuro-fondo` | `#16181D` | Fondo |
| `--oscuro-texto` | `#E6E7EA` | Texto y botón «Ingresar» |
| `--oscuro-texto-2` | `#9AA0AB` | Texto secundario |
| `--oscuro-punto` | `#AEB6C6` | Puntos de 7,0 o más, curva, foco |
| `--oscuro-arena` | `#C9A877` | Puntos bajo 7,0 y borde del error |

### Aplicación (clara)

| Token | Valor | Uso |
|---|---|---|
| `--fondo` | `#FFFFFF` | Fondo |
| `--tinta` | `#1E2026` | Texto, reglas de sección, botón primario |
| `--tinta-2` | `#6B7080` | Texto secundario |
| `--cabecera` | `#1E2026` | Cabecera del curso con la tira del período |
| `--grafico` | `#5B6170` | Barras de los gráficos |
| `--arena` | `#D2B48A` | Valores bajo 6,0: barras, tarjetas del asistente, «Menos de 6,0» |
| `--arena-texto` | `#8A5A1C` | Texto de un promedio bajo 6,0 (contraste AA) |
| `--acento` | `#7B8BD6` | Único acento: foco y campo activo |
| `--acento-texto` | `#3F51B5` | Enlaces y línea de 7,0 |
| `--ok` / `--atencion` / `--error` | `#2F6B3A` / `#A35F00` / `#B3463F` | Estados con punto y texto, nunca solo color |

## Movimiento

- Todo entra **una sola vez** y termina en menos de 3 s:
  - las cifras cuentan desde 0 en 1,1 s;
  - las barras crecen en 0,8 s;
  - las reglas de 2 px se trazan;
  - las filas suben con un pequeño retraso.
- **Inicio de sesión:**
  - 32 puntos que suben y flotan suave;
  - la curva que se dibuja en 1,6 s;
  - la raya punteada de 7,0;
  - «Pregúntale a tu curso» con 3 preguntas que rotan cada 3 s.
  - Los 32 valores son **decorativos** y fijos, tomados del mockup. Están marcados así en el código: no son notas de nadie ni vienen de la API.
- **Asistente:** «Consultando…» con brillo y tres puntos mientras espera.
- **`prefers-reduced-motion`:** todo aparece quieto, en su estado final. En el inicio se ve solo la primera pregunta.

## Lenguaje docente

En pantalla no hay términos estadísticos, códigos de regla ni nombres de funciones. Los campos del contrato no cambian: solo cambia cómo se dicen.

| Dato del contrato | En pantalla |
|---|---|
| `curso.mediana` | «La mitad del curso tiene más de 7,73» |
| `curso.desviacion` | «las notas varían unos 1,2 puntos alrededor del promedio» |
| `z` | «Frente al curso»: promedio del estudiante menos promedio del curso, «1,5 bajo el promedio» o «0,4 sobre el promedio» |
| `grupos` | «Cómo se reparte el curso»: «8,5 o más», «Entre 6,0 y 8,4», «Menos de 6,0» |
| `tendencia` | «Últimas notas: ↓ bajando 1,8», «↑ subiendo 0,2», «→ se mantiene»; «aún sin datos» con menos de 4 notas |
| `notas.calificadas`, `ceros`, `sinEntrega`, `pendientes` | «Notas registradas», «Con cero», «Sin entregar», «Por calificar» |
| `tasaFinalizacion` | «Actividades completadas» |
| Regla R1, R2, R4 | «Descenso reciente», «Atrasos», «Promedio bajo», con la evidencia en una frase |
| Herramientas del agente | «Calificaciones del curso», «Notas de Karina Lara», «Alertas del curso», «Tus cursos asignados» |

Las alertas siempre dicen «Requiere atención», nunca «reprobará» ni «en riesgo». Un dato en `noDisponibles`, o una consulta que falla, se ve como «no disponible», nunca como 0.

## Estructura

- **Barra lateral clara** de 240 px:
  - nombre del sistema arriba, con el código del curso;
  - dentro de un curso: «← Mis cursos», Panel, Alertas y Asistente;
  - para ADMIN: Configuración de Moodle y Estado del sistema;
  - nombre del usuario y «Cerrar sesión» abajo.
- **Cabecera oscura del curso**, a todo el ancho:
  - código y período en letra técnica;
  - nombre del curso en 44 px peso 900;
  - la tira del período;
  - la fecha y el estado de la última sincronización.

### Tira del período (`TiraPeriodo.jsx`, de Santiago)

- Dos grupos: «PARCIAL 1» con U1 y U2, y «PARCIAL 2» con U3 y U4.
- Cada unidad muestra `calificadas/actividades` de `unidades` en `GET /api/cursos/{id}/indicadores`.
- Una unidad completa va llena.
- Un 409 o un error se ven como «Sin datos», nunca 0/0.

**Regla de la unidad «actual»:** es la primera unidad, en orden, con menos actividades calificadas que actividades. Se marca con «· actual», un relleno proporcional a su avance y un anillo suave. Las que siguen van con borde tenue. Si todas están completas, ninguna es la actual. Se calcula en el frontend (`unidadActual` en `lenguaje.js`); el backend no la envía.

## Pantallas

| Pantalla | Qué tiene |
|---|---|
| Inicio de sesión (`/login`) | Pantalla oscura.<br>Izquierda: sello «Aula virtual · UTA · FISEI · Software», «Edu**Monitor» en 92 px, el lema «Seguimiento académico y **apoyo a tiempo**.» con «Encuentra a quién apoyar en tu curso y por qué, con las notas reales del aula virtual.», las preguntas que rotan y los segmentos P1 y P2.<br>Derecha: formulario real (`POST /api/auth/login`) con «Ingresar →».<br>El error no dice si falló el usuario o la contraseña. |
| Mis cursos (`/cursos`) | Estructura de Santiago: filtros en una fila y tabla con «Abrir», «Sincronizar curso» y «Ver historial».<br>El resultado de la sincronización y su historial se quedan aquí. |
| Panel (`/cursos/:id/panel`) | Cuatro cifras con regla de 2 px: promedio, «La mitad del curso tiene más de», actividades completadas y «Requiere atención» con enlace a Alertas.<br>Distribución en 10 tramos con la línea de 7,0 y los tramos bajo 6 en arena.<br>«Cómo se reparte el curso».<br>Tabla de estudiantes con las columnas en lenguaje docente. |
| Alertas (`/cursos/:id/alertas`) | Lista agrupada por estudiante con «Requiere atención», el nombre de la regla, la evidencia en una frase con sus números y «Marcar como revisada».<br>Filtro Activas y Revisadas.<br>Panel «Cuándo avisar» con un campo por regla y «Guardar». |
| Asistente (`/cursos/:id/asistente`) | Chat de página completa. Detalle abajo. |
| Configuración de Moodle y Estado del sistema (ADMIN) | Estructura de Santiago y de HU-20 con los tokens del diseño D. Un servicio por fila, con punto de color y texto. |
| «Acceso no permitido» (403) y «Tu sesión venció» (401) | Página completa sin barra lateral: regla de 2 px, título en 44 px peso 300, el motivo y un botón para volver.<br>El 401 llega desde el interceptor de Axios (evento `edumonitor:sesion-vencida`, de Santiago). |

### Detalle del Asistente

- **Barra izquierda:**
  - navegación del curso;
  - «+ Nueva conversación»;
  - historial guardado en la base, agrupado en «Hoy», «Esta semana» y «Antes». Sale de `GET /api/agente/conversaciones?cursoId=`, y cada pregunta se abre con `GET /api/agente/conversaciones/{id}`.
- **Bienvenida:** «Hola, Docente Uno. ¿Qué quieres saber de AGIL-4B?» con cuatro tarjetas de sugerencia.
- **Mensajes:**
  - La pregunta del docente va en burbuja; la respuesta del asistente, en texto con su icono.
- **Tarjetas de estudiante:**
  - Aparecen debajo de la respuesta para cada estudiante nombrado en el texto, buscado en los indicadores reales del curso.
  - Llevan nombre, barra del promedio (arena bajo 6,0), motivo en una línea con sus conteos reales y el promedio.
- **Acciones:**
  - «Abrir en el panel».
  - «Copiar».
  - «Datos usados», que despliega en palabras del docente qué se consultó, con la duración, la fecha de sincronización y el tiempo de respuesta del modelo local.
- **Pie:**
  - chips de seguimiento;
  - caja redondeada con el código del curso, el campo «Tu pregunta» y el botón de enviar.

## Accesibilidad

- Contraste AA en todo texto. El arena de las barras es decorativo; el texto de un promedio bajo 6,0 usa `--arena-texto`.
- Foco visible: 2 px de `--acento` con 2 px de separación.
- Los estados llevan punto de color y texto, nunca solo color.
- El ancho de 390 px no tiene desplazamiento horizontal de la página: las tablas se desplazan dentro de su caja.
