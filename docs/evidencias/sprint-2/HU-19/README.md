# HU-19 · Identidad visual y pantallas de acceso

Responsable: Santiago Escobar (PR #35). El 12 nov 2026 Kevin aplicó el diseño D sobre la rama de Santiago.
- El motivo y la regla de cada pantalla están en [docs/sprint-2/diseno.md](../../../sprint-2/diseno.md).
- Los componentes base, la tira del período, el interceptor de 401 y la protección de las rutas de ADMIN son de Santiago.

Capturas de la aplicación en Docker (`http://localhost:5173`) del 12 nov 2026, entre las 08:10 y las 08:14:
- Edge sin interfaz, a 1440 × 900; las de móvil, a 390 px.
- Datos reales del aula virtual: doc01 en AGIL-4B, sincronizado a las 07:07. La fecha se ve en la cabecera del curso.
- La pantalla de ADMIN se capturó con `admin`.

| Criterio | Captura | Qué se ve |
|---|---|---|
| CA-1 · DADO un docente sin sesión, CUANDO abre EduMonitor, ENTONCES ve un inicio de sesión propio del sistema y entra con su usuario real | [inicio de sesión](CA-1_inicio-de-sesion.png) · [animando](CA-1_inicio-de-sesion-animando.png) · [error](CA-1_inicio-de-sesion-error.png) | Pantalla oscura:<br>«Edu**Monitor**» y el lema «Seguimiento académico y apoyo a tiempo.».<br>Los 32 puntos decorativos con la curva y la raya de 7,0.<br>«Pregúntale a tu curso» con las preguntas que rotan.<br>Con credenciales incorrectas: «Credenciales incorrectas», sin decir cuál falló. |
| CA-2 · DADO un curso, CUANDO el docente lo abre, ENTONCES ve la barra lateral con Panel, Alertas y Asistente y la cabecera con la tira del período | [estructura](CA-2_estructura-cabecera-y-tira.png) | Cabecera oscura con AGIL-4B y Metodologías Ágiles (lab).<br>Tira: PARCIAL 1 con U1 4/4 y U2 4/4; PARCIAL 2 con U3 4/4 y **U4 0/4 · actual**.<br>«Sincronizado 12/11/2026, 07:07 · COMPLETADA».<br>Barra lateral con «← Mis cursos», Panel, Alertas y Asistente. |
| CA-3 · Mis cursos, Configuración de Moodle y Estado del sistema con la misma identidad | [Mis cursos](CA-3_mis-cursos.png) · [Configuración de Moodle](CA-3_configuracion-moodle.png) · [Estado del sistema](CA-3_estado-del-sistema.png) | Tabla de cursos con «Abrir», «Sincronizar curso» y «Ver historial».<br>Formulario de Moodle con el token enmascarado («••••» y 4 caracteres).<br>Servicios con punto de color y texto. |
| CA-4 · DADO doc01, CUANDO abre la ruta de BDD-4B, ENTONCES ve «Acceso no permitido» en pantalla completa | [403](CA-4_acceso-no-permitido.png) | «Acceso no permitido. El curso 2 no está asignado a este docente.» con «Volver a Mis cursos». |
| CA-5 · DADO una sesión vencida, CUANDO el docente navega, ENTONCES ve «Tu sesión venció» | [401](CA-5_sesion-vencida.png) | Token vencido en el navegador → «Tu sesión venció» con «Iniciar sesión». |
| CA-6 · El panel y las alertas hablan como docente, sin términos estadísticos ni códigos | [panel](CA-6_panel-lenguaje-docente.png) · [alertas](CA-6_alertas-sin-codigos.png) | Panel:<br>«La mitad del curso tiene más de 7,73».<br>«las notas varían unos 1,2 puntos alrededor del promedio».<br>«Cómo se reparte el curso» (8 · 22 · 2).<br>Columnas «Frente al curso», «Con cero», «Sin entregar», «Por calificar» y «Últimas notas».<br>Alertas: «Promedio bajo», «Atrasos» y «Descenso reciente» con su evidencia en una frase, sin R1, R2 ni R4. |
| CA-7 · El asistente es un chat con historial | [bienvenida](CA-7_asistente-bienvenida.png) · [respuesta](CA-7_asistente-respuesta-y-datos-usados.png) · [historial](CA-7_asistente-historial.png) | «Hola, Docente Uno. ¿Qué quieres saber de AGIL-4B?» con sugerencias.<br>«¿Cómo le va a Karina Lara?» se responde con la tarjeta de Karina Lara (5,29, «4 sin entregar»).<br>«Datos usados» dice «Notas de Karina Lara · 70 ms», la sincronización y «Respondió el modelo local qwen2.5:7b en 12,2 s».<br>Historial de la base agrupado en «Hoy». |
| CA-8 · Sin movimiento y en móvil | [sin movimiento](CA-8_sin-movimiento.png) · [inicio en móvil](CA-8_inicio-movil.png) · [panel en móvil](CA-8_panel-movil.png) | Con `prefers-reduced-motion` todo aparece quieto y completo.<br>A 390 px no hay desplazamiento horizontal de la página. |

## Pruebas

`npm test -- --run` en `frontend/`: 71 pruebas en 12 archivos, todas aprobadas.
- Siguen las de Santiago: componentes base, tira del período, Layout con 401 y 403, interceptor y Mis cursos.
- Se suman la unidad «actual» y el lenguaje docente del panel, de las alertas y del asistente.
- También cubren el historial del asistente.

`npm run lint` y `npm run build` pasan sin errores.
