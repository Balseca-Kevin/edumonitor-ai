# Sistema de diseño de EduMonitor (Sprint 2)

Estilo: diseño suizo en toda la aplicación y bento grid solo en el panel de indicadores. Reemplaza al estilo del Sprint 1 (serif Fraunces, fondo crema, cuadrícula azul), que el Product Owner vio como genérico.

Toda regla de color, tipografía o espaciado vive en `frontend/src/styles/tokens.css`. Ningún componente escribe un color, un tamaño de letra o un espacio literal.

## Prohibido

- Serif, segunda familia tipográfica, emoji.
- Sombras, degradados, desenfoque, transparencias tipo vidrio, relieves.
- Ilustraciones decorativas, fondos con patrón, cuadrículas de fondo.
- Más de un color de acento.
- Rojo para algo que no sea `FALLIDA` o un error.

## Tipografía

- Familia única: Archivo (`@fontsource-variable/archivo`, licencia OFL). Se quitan `@fontsource/fraunces` y `@fontsource/ibm-plex-*`.
- Cifras tabulares en toda nota, porcentaje, fecha y conteo: `font-variant-numeric: tabular-nums`.
- Escala (px): 12, 14, 16, 20, 28, 40, 64. Cuerpo 16 con interlineado 1,45. Títulos 1,1.
- Pesos: 400 texto, 500 etiquetas, 600 títulos, 700 cifra principal de cada tarjeta y nombre del sistema.
- Encabezados de tabla: 12 px, mayúsculas, espaciado de letras 0,04 em, color `--tinta-2`.

## Color

| Token | Valor | Uso |
|---|---|---|
| `--fondo` | `#ffffff` | Fondo de la app |
| `--fondo-2` | `#f4f4f3` | Barra lateral, filas alternas |
| `--tinta` | `#111111` | Texto, botón primario, regla de sección |
| `--tinta-2` | `#555555` | Texto secundario |
| `--tinta-3` | `#8a8a8a` | Texto deshabilitado |
| `--linea` | `#e3e3e1` | Bordes de 1 px |
| `--acento` | `#2346d8` | Selección, foco, enlaces, elemento activo de la barra lateral |
| `--ok` | `#2e7d32` | `COMPLETADA`, `OK` |
| `--atencion` | `#a35f00` | `PARCIAL`, «Requiere atención» |
| `--neutro` | `#6b7280` | `PENDIENTE`, `SIN_ENTREGA`, «no disponible» |
| `--error` | `#c62828` | `FALLIDA`, errores |

Contraste AA en todo texto. Los estados se muestran con punto de color y texto, nunca solo con color.

## Espacio y forma

- Unidad base 8 px. Escala: 4, 8, 16, 24, 32, 48, 64.
- Grilla de 12 columnas, ancho máximo 1280 px, medianil 24 px. Texto alineado a la izquierda.
- Radio 2 px. Bordes de 1 px `--linea`.
- Cada sección empieza con una regla de 2 px `--tinta` sobre su título.
- Foco visible: contorno de 2 px `--acento` con 2 px de separación.
- Animaciones de 150 ms como máximo, desactivadas con `prefers-reduced-motion`.

## Estructura de la aplicación

- Barra lateral de 240 px: nombre del sistema arriba; dentro de un curso, Panel, Alertas y Asistente; para ADMIN, Configuración de Moodle y Estado del sistema; cerrar sesión abajo. La sincronización y su historial se quedan en Mis cursos.
- Cabecera del curso: nombre corto en 12 px mayúsculas, nombre del curso en 28 px, y la tira del período.
- Tira del período (`TiraPeriodo.jsx`): dos grupos, «Parcial 1» con U1 y U2, «Parcial 2» con U3 y U4. Cada unidad es un segmento: lleno `--tinta` si todas sus actividades están calificadas, con borde si le faltan, y el texto «4/4» debajo. Sale de `unidades` de `GET /api/cursos/{id}/indicadores`. A la derecha: fecha de la última sincronización y su estado.

## Pantallas

### Inicio de sesión

Dos columnas en 5 y 7 de la grilla. Izquierda: «EduMonitor» en 64 px peso 700, debajo una línea en 20 px con lo que hace el sistema, y abajo «UTA · FISEI · Software» en 12 px. Derecha: formulario de 400 px de ancho con usuario, contraseña y botón primario negro a todo el ancho. Error en un aviso con borde `--error`, sin indicar si falló el usuario o la contraseña. En móvil, una columna.

### Mis cursos

Tabla, no tarjetas: nombre corto, nombre, período, última sincronización, estado y acciones «Abrir», «Sincronizar» e «Historial» (la sincronización y su historial se quedan en esta pantalla). Búsqueda y filtros en una fila arriba de la tabla.

### Panel del curso (bento grid)

| Fila | Tarjetas (columnas de 12) |
|---|---|
| 1 | Promedio del curso (4) · Mediana y desviación (4) · Requiere atención, conteo de alertas activas con enlace (4) |
| 2 | Distribución de notas, histograma de 10 tramos con la línea de 7,0 (8, dos filas de alto) · Grupos alto, medio y bajo (4) |
| 3 | Finalización (4), «no disponible» si viene en `noDisponibles` |
| 4 | Tabla de estudiantes (12): nombre, promedio ponderado, z, grupo, calificadas, ceros, sin entrega, pendientes, tendencia |

Cada tarjeta: título de 12 px en mayúsculas, cifra en 40 px peso 700 tabular, una línea de contexto en 14 px. Sin iconos decorativos.

### Alertas

Lista agrupada por estudiante. Cada alerta: «Requiere atención», código y nombre de la regla, evidencia en una línea con sus números, y botón «Marcar como revisada». A la derecha, panel de umbrales con un campo por regla y botón «Guardar».

### Asistente

Columna de conversación de 720 px. Debajo de cada respuesta, una tabla compacta de herramientas usadas: nombre, parámetros, estado y duración en ms. Mientras espera, el texto «Consultando…» y el botón deshabilitado. Si la API responde 503, aviso «El asistente no está disponible» con `--neutro`.

### Acceso no permitido y sesión vencida

Página completa, sin barra lateral. Título en 40 px, una línea con el motivo y el nombre del curso cuando aplica, y un botón para volver.

### Estado del sistema (ADMIN)

Tabla con un servicio por fila: nombre, estado con punto de color, detalle y hora de la consulta. Botón «Actualizar».
