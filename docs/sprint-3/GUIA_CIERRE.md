# Guía de cierre del Sprint 3 para Claude Code

Archivo: `docs/sprint-3/GUIA_CIERRE.md`. Reemplaza cualquier guía de cierre anterior.

El código base del Sprint 3 ya está en `develop` (PR #53 a #70). Esta guía cubre lo que falta: los temas de AGIL-4B, los añadidos que la línea de corte había sacado, la evaluación del asistente con el modelo local, las capturas y el release a `main`.

## Modo de trabajo

1. Empieza ya. Kevin autoriza no presentar plan ni esperar confirmación en las tareas de esta guía. Esto reemplaza el punto 1 de «Cómo trabajar en este repositorio» de `CLAUDE.md` solo para este archivo.
2. Detente y pregunta solo en estos casos: una decisión que no se puede deshacer, una tarea que pida algo fuera de alcance (punto 10 de `CLAUDE.md`), o una prueba que sigue en rojo después de dos intentos de corrección.
3. No hay límite de tiempo. Prefiere la tarea bien hecha y probada a la tarea rápida. Prefiere también decir «no pude» a inventar un dato.
4. No escribas fechas nuevas en documentos ni en datos. Las fechas que muestra la aplicación salen de Moodle o del servidor.
5. No escribas `docs/sprint-3/daily.md` ni toques Jira. Kevin los descartó para este sprint. El paquete de OneDrive lo arma Kevin.
6. No leas ni escribas `.env`. `.env.example` solo se modifica si cambia una variable.
6a. Decisión de Kevin: el modelo externo no se activa. El sistema se prueba y se demuestra solo con el modelo local (Ollama). No pidas ni busques una API key, no cambies `MODELO_EXTERNO_*` y no quites el código del modelo externo, el respaldo ni el interruptor: ya están fusionados y con pruebas. Sin API key, el asistente responde con el modelo local por diseño.
7. Nunca `docker compose down -v` ni `docker volume rm`.
8. Git, autorizado por Kevin para esta guía:
   - ramas `feature/HU-xx-descripcion` desde `develop`, una por tarea o par de tareas;
   - commits Conventional Commits en español, sin línea de atribución de Claude;
   - `git push` de esas ramas, PR a `develop` de 30 archivos como máximo sin contar migraciones, y fusión con el bypass del ruleset cuando la integración continua esté en verde;
   - al final, el release a `main` del bloque H.
9. No se suben a ningún PR: `.claude/settings.json`, `scripts/*.bat`, `EduMonitorAI-Sprint2-v0.2.1.zip`.
10. Antes de cada PR corre `dotnet test backend/EduMonitor.sln`, `npm test -- --run` en `frontend/` y `python -m pytest` en `agent/`. Con el aula virtual encendida el backend debe dar 317 de 317 más las pruebas nuevas.
11. Reglas de pantalla que no cambian: todo se demuestra en la aplicación; las alertas dicen «Requiere atención»; no aparecen las palabras correlación, riesgo ni reprobación; sin emoji; un dato que falta se muestra «no disponible», nunca 0; nada se escribe en Moodle ni se envía al estudiante.
12. Un cambio de ruta o de JSON se detalla primero en `docs/contratos-api.md`, en el mismo PR que lo programa.

## Orden de los bloques

A, B, C1, C2, C3, C4, C5, C6, C7, C8, D, E, F, G, H. Los bloques B y C van antes de D, E y F porque las capturas se toman con la interfaz final. C6 y C7 son los más visibles para Kevin: hazlos con cuidado y antes de la evaluación del bloque D, porque cambian el prompt y la interfaz.

## Bloque A · Estado inicial

1. `git fetch`, `git checkout develop` y `git pull`. Confirma con `git diff develop feature/HU-23-memoria-respaldo` que la rama local no tiene nada que `develop` no tenga. Si el resultado es vacío, sigue desde `develop`. Si no, avisa qué archivos difieren antes de seguir.
2. Docker Desktop encendido. Aula virtual: `docker compose -f moodle-lab/docker-compose.yml --env-file .env up -d`. EduMonitor: `docker compose up -d --build`. Espera a que `http://localhost:5173` responda.
3. Corre las tres suites y anota los resultados de partida.

## Bloque B · Temas de AGIL-4B

Hoy AGIL-4B no muestra el plan de refuerzo, el mapa de calor ni «Refuerzos aplicados» porque sus actividades no tienen temas asociados. Kevin autoriza completar el mapeo.

1. Verifica primero qué hay. Revisa en la base si los temas del sílabo (`docs/sprint-3/silabos.md`, 4 unidades de AGIL-4B) existen para el curso. Si faltan, usa «Sincronizar y abrir» en la aplicación y vuelve a revisar. Si siguen sin aparecer, es un error de `TemasSilabo` o de la sincronización: corrígelo con prueba.
2. Arma la propuesta de mapeo con datos reales, no con suposiciones. Para cada actividad de AGIL-4B lee su enunciado y los criterios de su rúbrica tal como están en Moodle (vía la aplicación o la base) y asócialos a los temas de su unidad que de verdad evalúan. Las actividades nuevas (Tarea 9, APE 5, Proyecto integrador) ya traen en el script de Moodle los temas que cubren. Guarda la tabla en `docs/sprint-3/mapeo-agil-4b.md` con una línea de motivo por cada asociación.
3. Aplica el mapeo por el mismo camino que usa el docente: la pantalla «Mapa del curso» (o, si no se puede automatizar, `PUT /api/cursos/{id}/actividades/{aid}/temas` con el token de doc01 obtenido por el login con las credenciales de prueba del README). Los criterios de rúbrica también se asocian a su tema.
4. No cambies notas, estados de entrega ni los casos de la demo en Moodle. Si algo necesita una corrección en Moodle, repórtalo en el resumen final y no lo hagas.
5. Verifica en pantalla: «Plan de refuerzo» lista temas con su porcentaje de dominio y los estudiantes bajo 6,0; el mapa de calor estudiante × tema tiene datos; la ficha muestra temas débiles.
6. Comprueba el caso de la demo: un estudiante que falle el mismo tema en 3 actividades. Si con el mapeo honesto ese caso aparece, anota quién es y qué actividades. Si no aparece, no fuerces el mapeo: repórtalo con los datos que viste.
7. PR: `docs/sprint-3/mapeo-agil-4b.md` y las correcciones de código si hubo. El mapeo vive en la base local de Kevin, no en el repositorio, así que explica en el PR cómo repetirlo.

## Bloque C · Añadidos

Kevin los retoma por decisión propia, aunque la línea de corte los había sacado. En `docs/sprint-3/PLAN.md` se quita la nota de corte de estas tareas y se marca quién las hizo.

### C1 · TA-22.A1 · Criterio que no separa (HU-22, 3 h)

El dominio por criterio ya existe en `ActividadService` (porcentaje de estudiantes con 70 % del puntaje máximo o más). Falta la segunda mitad.

1. Contrato: cada criterio de `GET /api/cursos/{id}/actividades/{aid}` suma `separacion` (número de -1 a 1, o `null`) y `aviso` (`null`, `REVISAR_ENSENANZA` o `REVISAR_ENUNCIADO`).
2. `CalculadoraCriterio`, función pura en Application. `separacion` es el coeficiente de Pearson entre el puntaje del criterio y la nota total de la actividad sin ese criterio, sobre los estudiantes calificados. Devuelve `null` con menos de 8 estudiantes calificados, con varianza cero en alguno de los dos lados, o con un solo criterio en la rúbrica.
3. Umbrales como constantes con nombre, que Kevin puede cambiar: `DominioBajo = 0,30` y `SeparacionBaja = 0,20`. `aviso = REVISAR_ENSENANZA` si el dominio es menor o igual a `DominioBajo`. Si no, `aviso = REVISAR_ENUNCIADO` cuando `separacion` no es `null` y es menor o igual a `SeparacionBaja`. Si no, `null`. Anota los dos umbrales en el contrato.
4. Pruebas unitarias con casos conocidos: x = 1, 2, 3, 4, 5 contra y = 2, 4, 5, 4, 5 da 0,7746; una serie idéntica da 1; una invertida da -1; una constante da `null`; menos de 8 estudiantes da `null`; dominio 0,25 da `REVISAR_ENSENANZA` aunque la separación sea alta.
5. Revisa con los datos reales de AGIL-4B qué actividades con rúbrica disparan un aviso y anótalo en el resumen. Si ninguna lo dispara, no inventes un caso ni cambies notas: repórtalo.

### C2 · TA-22.A2 · Avisos y enunciado corregido (HU-22, 3 h)

1. En `Actividad.jsx`, bajo cada criterio con aviso, el texto exacto:
   - `REVISAR_ENSENANZA`: «Casi todo el curso falló este criterio: revisar cómo se enseñó».
   - `REVISAR_ENUNCIADO`: «Este criterio no separa a quienes van bien del resto: revisar el enunciado».
   Con 32 estudiantes la cifra es orientativa; la leyenda dice «revisar», no un veredicto. No aparece la palabra correlación.
2. Botón «Pedir enunciado corregido» en los criterios con aviso. Abre el asistente con la actividad como foco y un mensaje fijo que nombra el criterio. El asistente lee la actividad con `obtener_detalle_actividad` y redacta una propuesta de enunciado que cambia solo lo que toca a ese criterio. No inventa notas ni porcentajes. La respuesta se puede copiar. Nada se escribe en Moodle.
3. Agrega el aviso al resultado de `obtener_detalle_actividad` para que el asistente pueda mencionarlo.
4. Pruebas: componente (texto de cada aviso, ausencia de la palabra correlación), backend del aviso y del mensaje, agente (la herramienta devuelve el aviso).

### C3 · TA-16.A1 · Refuerzos aplicados en el informe (HU-16, 1 h)

1. `InformeService` agrega al PDF una sección «Refuerzos aplicados» con tema, estudiante, promedio antes, promedio ahora y estado (subió, igual, bajó, sin notas nuevas).
2. Redacta el cambio sin causalidad: «subió de 5,2 a 7,1 en el tema después del refuerzo». No escribas que el refuerzo lo causó.
3. Si no hay refuerzos aplicados, la sección dice «Aún no hay refuerzos aplicados», no 0.
4. Prueba del contenido del PDF y de la sección vacía.

### C4 · TA-15.B1 y TA-15.B2 · Grupos de refuerzo por tema (HU-15, 4 h)

Verifica primero cómo está hecho el plan de refuerzo (`PlanRefuerzoService`, `PedidoPlanRefuerzo`, `PlanRefuerzo`, `PlanRefuerzo.jsx`) y reutiliza lo que sirva, incluido el campo `Tipo` si ya existe.

1. Contrato nuevo: `GET /api/cursos/{id}/refuerzos/grupos` devuelve, por tema, los estudiantes con promedio del tema menor a 6,0, con ese promedio y cuántas notas lo componen. Solo entran temas con 2 estudiantes o más. Un estudiante sin notas del tema no cuenta y no se muestra como 0. 401, 403 y 409 como los demás endpoints del curso.
2. Pantalla: en «Plan de refuerzo», pestaña «Por tema» con cada grupo y el botón «Armar plan grupal».
3. El plan grupal usa el mismo flujo de TA-15.A3 y TA-15.A4: se envía al asistente, el asistente redacta un borrador para el grupo (tareas por tema, entrega sugerida antes de la próxima evaluación de ese tema, qué está en juego), el docente lo edita, lo aprueba y lo descarga en PDF y DOCX. Aparece en el historial.
4. Al aprobarlo se guarda la foto del tema por cada estudiante del grupo, para que «Refuerzos aplicados» compare antes y después después de la siguiente sincronización.
5. Hacia el modelo externo van códigos E-001, E-002…, nunca nombres. La pantalla muestra nombres.
6. Pruebas: agrupación con casos conocidos (umbral exacto en 6,0, tema con un solo estudiante, estudiante sin notas del tema), contrato, componente, seudonimización.

### C5 · TA-15.B3 · Borradores con el estilo del docente (HU-15, 2 h o más)

1. Migración: `PlanRefuerzo` guarda el texto generado por el asistente y el texto final aprobado.
2. Al aprobar, se guardan los dos.
3. Cuando el mismo docente tiene 2 o más borradores aprobados cuyo texto final difiere del generado, el agente recibe los 2 más recientes como ejemplos de estilo, con los nombres cambiados por códigos. La instrucción dice que se imiten el tono y la estructura, no el contenido; las notas y los criterios siguen saliendo solo de las herramientas, y el verificador de cifras sigue aplicando.
4. Los ejemplos son solo del docente que redacta. No se mezclan entre docentes.
5. Pantalla: el borrador muestra la marca «Con tu estilo» cuando se usaron ejemplos. Con menos de 2 ejemplos no aparece y el comportamiento no cambia.
6. Pruebas: selección de ejemplos, aislamiento por docente, seudonimización de los ejemplos, marca en pantalla.

### C6 · Cabecera compacta del curso (HU-22, TA-22.A5)

Kevin revisó la cabecera con la línea de tiempo en MCS-4B y no la acepta como está. Problemas que vio y que se corrigen:
- Ocupa media pantalla en todos los módulos (libro, mapa, alertas, plan de refuerzo, asistente), con el título del curso en letra enorme, la semana en cifra gigante, la línea de tiempo y tres tarjetas. Se siente invasiva.
- Las etiquetas de la línea de tiempo se pisan entre sí («T1PE1», «APE2 P1», «HOY 13 P2» ilegibles).
- La leyenda siempre visible y los tres bloques (vencidas, vence esta semana, siguiente) repiten datos que el docente no necesita en cada pantalla.

Cambio pedido. Los datos son los mismos de `GET /api/cursos/{id}/periodo`; el backend no cambia.

1. Barra de contexto compacta en todos los módulos del curso, de 48 px de alto como máximo, en una sola línea: código del curso en mono, nombre del curso en 15 a 16 px (se recorta con puntos suspensivos y muestra el nombre completo en un tooltip), «Semana 13 de 13», la unidad actual recortada, y a la derecha dos chips solo si aplican: «Vence esta semana: Examen Parcial 2» y «15 vencidas». Un botón «Ver período» al final.
2. El título grande desaparece de los módulos. Cada pantalla usa su propio título (por ejemplo «Libro de calificaciones») en un tamaño normal de título de página, no de portada. La letra grande queda solo en «Mis cursos» y en el inicio de sesión.
3. «Ver período» y los dos chips abren un panel flotante anclado a la barra («Período»), no una pantalla. Contiene la línea de tiempo, los tres bloques (vencidas con sus entregas faltantes, vence esta semana, siguiente) y la leyenda, todo con los datos reales del período. Se cierra con Esc, con clic afuera y con un botón. En pantallas pequeñas se muestra como panel inferior.
4. Línea de tiempo nueva, más ligera: una barra continua por semanas con los parciales y las unidades como segmentos, marcadores solo para evaluaciones (círculo para tarea o APE, rombo para examen), y el marcador «Hoy». Sin etiquetas fijas bajo los marcadores: el nombre, la fecha límite y las entregas («Tarea 3 · vence 29/09 · 11 de 12 entregaron») salen en un tooltip al pasar el cursor o al enfocar con el teclado. Así no hay textos que se pisen.
5. «Panel del curso» es la única pantalla donde la línea de tiempo se ve sin abrir nada, en una versión de 80 px de alto como máximo, debajo de la barra. En los demás módulos solo aparece la barra de 48 px.
6. Si el curso no tiene período calculado, la barra muestra «Sin sincronizar» y no el panel. El texto «no disponible» sigue siendo el de la regla de datos reales.
7. Librerías, autorizadas por Kevin para este cambio (el stack del frontend no se cambia sin acuerdo del equipo, y este es ese acuerdo): `@radix-ui/react-popover` y `@radix-ui/react-tooltip` para el panel y los tooltips con foco, teclado y posición correctos, y `motion` (antes `framer-motion`) para la entrada y salida del panel y de la barra. Elige las versiones compatibles con React 18 y Vite que estén vigentes, revisa que sus licencias sean MIT y corre `npm run build` para anotar en el PR cuánto subió el tamaño del paquete. Mantén las animaciones del diseño D y del prototipo, y respeta `prefers-reduced-motion`. Si una librería da problemas con Vitest o jsdom, usa la alternativa más simple en CSS y dilo en el PR.
8. Diseño: sigue `docs/sprint-3/diseno.md` (paleta, Archivo, JetBrains Mono para códigos). Actualiza ese documento con la nueva especificación de la cabecera. Archivos que se tocan: `CabeceraCurso.jsx` y su prueba, `CursoMarco.jsx`, `Marco.jsx`, `Panel.jsx`, `index.css` o `app.css`.
9. Pruebas de componente: la barra aparece en cada ruta del curso; el panel abre con el botón y con cada chip y cierra con Esc; cada marcador tiene nombre accesible y tooltip; la versión de «Panel del curso» y la de los demás módulos; el caso sin período; el caso con período terminado.
10. Comprueba en pantalla con MCS-4B (semana 13 de 13, 12 estudiantes) y AGIL-4B, en ancho de 1440 px y en 900 px.

### C7 · Asistente conversacional (HU-23)

Kevin revisó dos respuestas del asistente en MCS-4B y concluyó que está encerrado en los datos del curso. Es un asistente: tiene que responder bien cualquier pregunta, incluso la que no esperabas, y usar las herramientas solo cuando la pregunta necesita datos del curso.

Lo que se vio:
- «¿Qué cursos tengo asignados?»: los datos eran correctos, pero la respuesta repitió «cursoId 1002», un identificador interno que el docente no debe ver, y cerró con una pregunta genérica que no hacía falta.
- «¿Ya puedes generar tareas de refuerzo?»: es una pregunta sobre lo que puede hacer el sistema. El asistente respondió con un listado de alertas de 10 estudiantes. La respuesta correcta es que sí, y cómo: en «Plan de refuerzo» eliges la actividad, los temas y los estudiantes, el asistente redacta un borrador que se edita, se aprueba y se descarga en PDF o DOCX.
- Cifras que no coinciden dentro de la misma respuesta. El texto dice promedio 6.62 para Ana Acosta, 6.21 para Gabriela Guerrero y 5.42 para Javier Jácome, mientras las tarjetas «Estudiantes mencionados» de abajo dicen 6,39, 6,40 y 5,37. La URL de esa pantalla tiene `?tramo=u2`. Hipótesis: el texto usa la evidencia de la alerta (todo el ciclo) y las tarjetas usan el tramo de la unidad 2. Averigua la causa. El texto y las tarjetas deben usar el mismo tramo y la respuesta debe decir de cuál habla («en la unidad 2», «en todo el ciclo»).
- Para Bruno Benítez dice «últimos promedios 4, 4.1, 4.5 (descenso de -4.31)»: son notas, no promedios, y el orden da la impresión de que suben. Mientras, su tarjeta dice 8,87 y «bajando 0,5». Revisa la evidencia de la regla R1 que entrega la herramienta: etiquetas y orden sin ambigüedad (de la más reciente a la más antigua), y la redacción «sus últimas 3 notas fueron 4,5; 4,1 y 4,0, frente a un promedio anterior de 8,3» solo con cifras de la herramienta.
- Los decimales salen con punto («6.62»); la aplicación usa coma («6,39»). La respuesta final debe usar coma, con una conversión de código determinista que no toque la verificación de cifras.
- El texto pegado muestra asteriscos de negrita. Comprueba que la pantalla del asistente renderiza negritas y listas, y que no deja asteriscos a la vista.

Causa en el código (`agent/app/graph.py`, `PROMPT_SISTEMA`): el prompt convierte todo mensaje en una consulta de datos. Manda llamar una herramienta antes de responder, «de forma breve», «presenta como hechos solo los datos consultados», «si falta un identificador pide aclaración», y el modelo tiene todas las herramientas enlazadas en cada turno, así que el modelo de 7B tiende a llamar una. El mensaje al agente también le muestra «cursoId», y el modelo lo repite.

Cambio pedido:

1. Tres tipos de mensaje, y el asistente se comporta distinto en cada uno:
   - Datos del curso (notas, promedios, alertas, actividades, fechas, estudiantes, refuerzos): usa herramientas y la verificación de cifras, como ahora.
   - La aplicación y el trabajo del docente (qué puede hacer, cómo se arma un plan de refuerzo, qué significa «Requiere atención», cómo se sincroniza, qué muestra cada pantalla): responde directamente desde una guía de la aplicación incluida en el agente, sin herramientas. Crea `agent/app/guia_app.py` con esa guía, corta y exacta, con los nombres reales de las pantallas y botones de la aplicación final. Verifícala contra el frontend.
   - Cualquier otra pregunta, relacionada o no con el curso: conocimiento general, explicar un concepto de la materia (Scrum, Kanban, configuración de software…), ideas de actividades o de cómo enseñar un tema, ayuda para redactar una rúbrica o un enunciado, resumir o reescribir un texto, una conversación breve («hola», «gracias»). Responde como un asistente general, sin herramientas, con la extensión que pida la pregunta. No la rechaces ni la rediriges a los datos del curso por no ser de datos.
   - Mensajes mixtos («explícame Scrum y dime quién tiene más atrasos»): responde la parte general y llama a las herramientas solo para la parte de datos.
2. Enrutador determinista en código, antes del modelo y después de `alcance.py`. Decide con reglas (palabras de datos del curso, códigos E-001, nombres de actividades y estudiantes, verbos de consulta, preguntas de capacidad) si el turno es de datos, de aplicación, general o mixto. En los turnos que no son de datos, el modelo se invoca sin herramientas enlazadas. Con duda entre datos y general, enlaza las herramientas y deja que el prompt diga que se usan solo si la pregunta necesita datos. Las reglas van en un archivo propio con pruebas.
3. Prompt nuevo, en dos partes: una base conversacional (eres un asistente para el docente, responde cualquier pregunta, con claridad y la extensión adecuada) y un anexo de datos que solo se agrega en los turnos con herramientas. Quita «de forma breve» como regla general: la brevedad se ajusta a la pregunta.
4. Reglas de redacción del asistente:
   - Nunca muestra identificadores internos (`cursoId`, `estudianteId`, `refuerzoId`) ni nombres de herramientas. Habla del «curso MCS-4B» y de los estudiantes por su nombre. El mensaje que el backend arma para el agente puede seguir llevando los ids, pero el prompt prohíbe repetirlos.
   - Responde primero lo que se preguntó. Si la pregunta es ambigua, elige la lectura más probable y dilo en una cláusula; hace una pregunta solo cuando no puede responder sin ella. No cierra cada respuesta con una pregunta.
   - El curso de la pantalla es el curso por defecto; se nombra solo cuando ayuda.
   - Las cifras del curso salen solo de las herramientas. En la ruta general no se inventan datos del curso; si la respuesta general trae una nota o un nombre de estudiante, se descarta y se rehace por la ruta de datos.
   - Los límites de `alcance.py` no cambian: no evalúa aspectos clínicos ni psicológicos, no predice aprobación, no cambia notas ni escribe en Moodle, no envía mensajes.
5. La verificación de cifras solo corre en los turnos con herramientas. En los turnos generales, un número como «3 roles de Scrum» no se marca como «no verificada».
6. Calidad de la respuesta de datos: las listas largas (por ejemplo, 10 estudiantes con atrasos) se agrupan y resumen («10 de 12 estudiantes tienen atrasos en las tareas 7, 8 y APE 4») y el detalle queda en las tarjetas y en la ficha.
7. Pruebas pytest con un modelo simulado, sin Ollama, para el enrutador y las reglas de redacción. Mensajes y ruta esperada:
   - «¿Qué cursos tengo asignados?»: datos. La respuesta no contiene «cursoId».
   - «¿Ya puedes generar tareas de refuerzo?»: aplicación. Sin llamada a herramientas. Menciona «Plan de refuerzo».
   - «¿Qué puedes hacer?» y «¿Quién eres?»: aplicación.
   - «¿Qué es Scrum?», «Explícame Kanban en dos frases», «Dame ideas de actividades para repasar Kanban» y «¿Cuál es la capital de Francia?»: general, sin herramientas.
   - «Hola» y «Gracias»: general, respuesta corta.
   - «¿Quiénes tienen promedio bajo 7?»: datos.
   - «Explícame qué es Scrum y dime quién tiene más atrasos»: mixto, con herramientas solo para la segunda parte.
   - «¿Tiene ansiedad Ana Acosta?»: fuera de alcance, como hoy.
   - Decimales con coma en la respuesta final.
   No cambies el número de preguntas de «Calidad del asistente» (18) ni sus respuestas esperadas.
8. Recorrido manual con el modelo local: las dos preguntas de Kevin, las de la lista anterior y tres más que se te ocurran, y anota en el resumen cómo respondió el modelo de 7B. Si las respuestas generales son flojas, dilo con ejemplos; no las presentes como buenas. Agrega esas preguntas a `docs/PRUEBAS.md#sprint-3`.
9. Captura nueva en `docs/evidencias/sprint-3/HU-23/`: una conversación con una pregunta de datos, una de aplicación y una general en el mismo hilo.

### C8 · Velocidad y lógica del asistente (HU-23)

Kevin pide una mejora grande del asistente en velocidad de respuesta y en lógica, sin salirse de los lineamientos del proyecto. Se hace después de C7, porque usa su enrutador.

Contexto medido, no supuesto:
- La última evaluación con el modelo local dio 11,7 s por pregunta en promedio.
- El equipo de Kevin tiene una RTX 4050 de 6 GB y 16 GB de RAM. `qwen2.5:7b` ocupa cerca de 4,7 GB cuantizado, así que el margen de VRAM es corto. Si Ollama reparte capas entre GPU y CPU, las respuestas se vuelven mucho más lentas.
- `OLLAMA_KEEP_ALIVE=24h` ya está puesto. `num_ctx` y `num_predict` no están fijados. El contexto efectivo es el de Ollama por defecto.
- No hay streaming: el docente ve la pantalla quieta hasta que termina toda la respuesta.
- Todo turno enlaza las 10 herramientas y deja que el modelo elija parámetros. Eso produjo llamadas sin curso, órdenes inventados y respuestas que no respondían lo preguntado.

Dentro de los lineamientos:
- Los promedios, alertas, notas necesarias y órdenes los calcula el código. El modelo solo explica y recomienda.
- Una sola aplicación con un servicio de agente. Sin servicios nuevos.
- Solo lectura de Moodle. Nada se envía a estudiantes.

No se hace, por quedar fuera del alcance o por romper reglas del proyecto: entrenar o ajustar modelos, varios agentes que se hablan, Redis, colas, bases vectoriales o cualquier servicio nuevo, búsqueda por embeddings sobre las notas, escribir en Moodle, mensajes automáticos, predicción de aprobación, juicios clínicos o psicológicos, y enviar nombres, correos o ids de Moodle a un modelo externo.

Método: mide, cambia una cosa, mide otra vez. Una mejora que no baja el tiempo o no mejora el acierto se revierte y se anota.

**C8.0 · Línea base**
1. Cada respuesta guarda el tiempo de cada etapa: enrutador, armado del contexto, cada llamada al modelo (tokens de entrada, tokens de salida, segundos), cada herramienta, verificación de cifras. «Cómo llegó a esta respuesta» muestra el total por etapa.
2. Con la GPU encendida, corre `ollama ps` dentro del contenedor y comprueba que el modelo está al 100 % en GPU. Si reparte capas con la CPU, ese es el primer arreglo: baja `num_ctx`, prueba `OLLAMA_FLASH_ATTENTION=1` y `OLLAMA_KV_CACHE_TYPE=q8_0` en el servicio `ollama` del compose, y usa la cuantización más pequeña que no pierda aciertos en las 18 preguntas.
3. Corre las 18 preguntas y las preguntas generales de C7. Guarda mediana, promedio y percentil 90 por ruta (determinista, aplicación, general, datos, mixto) en `docs/evidencias/sprint-3/HU-18/`.
4. Metas propuestas por mí, a confirmar con la línea base: respuesta determinista en menos de 1 s, pregunta general en menos de 5 s y consulta de datos en menos de 6 s en el equipo de Kevin con GPU. Si la línea base muestra que no son realistas, dilo y propón la meta que sí lo sea.

**Velocidad**
- V1 · Ruta determinista sin modelo. Además de la fecha de C7: «¿qué puedes hacer?», «¿qué significa Requiere atención?» y la lista de pantallas. Y un conjunto cerrado de consultas de datos frecuentes que el código resuelve completo: alertas activas, quién tiene promedio bajo un valor, qué vence esta semana y la siguiente evaluación, nota necesaria de un estudiante, refuerzos aplicados, estado del período. El código llama la herramienta, y la respuesta sale de plantillas escritas en español natural, con coma decimal. Cada respuesta lleva el botón «Explicar» que pide al modelo una lectura de esos mismos datos. Las cifras salen verificadas por construcción.
- V2 · Solo las herramientas de la ruta. El enrutador enlaza al modelo únicamente las 1 a 3 herramientas que la pregunta necesita. Menos esquemas en el prompt reducen los tokens de entrada.
- V3 · Los parámetros los pone el código. Curso y tramo de la pantalla, estudiante por nombre (el filtro `nombre` ya existe), actividad por nombre, «esta semana», «la unidad 2», «el parcial 1». El modelo no elige `cursoId`, `orden` ni filtros. Si un nombre coincide con varios, la respuesta ofrece los candidatos como botones y no adivina.
- V4 · Datos compactos hacia el modelo. JSON sin campos nulos, con las cifras ya redondeadas como se muestran, y solo los campos que la pregunta necesita. Resúmenes precalculados en el backend para las listas largas.
- V5 · Historial compacto. De los turnos anteriores solo viajan la pregunta y el texto final de la respuesta, nunca los resultados de herramientas. Máximo 4 turnos, con tope de largo.
- V6 · Ajuste de Ollama. `num_ctx` fijo, el menor que quepa con el prompt más grande ya compacto; `num_predict` con tope por ruta; llamada de calentamiento al arrancar el agente para que la primera pregunta no cargue el modelo. Conserva solo lo que la medición justifique.
- V7 · Herramientas en paralelo. En un mensaje mixto o con varias consultas independientes, se llaman a la vez (`asyncio.gather` dentro del mismo grafo).
- V8 · Caché dentro de la API. En memoria del proceso, sin servicio nuevo, para las herramientas de lectura. Clave: curso, herramienta, parámetros, tramo, id de la última sincronización y versión de las reglas de alerta. Se invalida al sincronizar y al cambiar una regla.
- V9 · Streaming, solo después de V1 a V8 y con las pruebas en verde. El agente expone `POST /chat/stream` (eventos del servidor), la API lo reenvía en `POST /api/agente/chat/stream` con las mismas validaciones de curso y de token, nginx lo pasa sin búfer, y la pantalla muestra los pasos («Consultando alertas…», «Verificando cifras…», «Redactando…») y el texto a medida que llega. El navegador sigue sin hablar con el agente. La conversación se guarda al terminar. El resultado de la verificación de cifras llega como último evento; si obliga a redactar otra vez, la pantalla reemplaza el texto con una transición visible. `POST /api/agente/chat` se conserva para la evaluación y las pruebas. Detalla el contrato en `docs/contratos-api.md` antes de programarlo.

**Lógica**
- L1 · Entidades resueltas por código antes del modelo (parte de V3): estudiantes, actividades, temas y tramos por nombre. Si falta uno, la respuesta lo dice.
- L2 · Mismo tramo en el texto y en las tarjetas, y la respuesta dice de cuál habla (corrige la diferencia que se vio con `?tramo=u2`).
- L3 · «¿A quién atiendo primero?» devuelve un orden calculado en el backend con criterios visibles: temas bajo 6,0, actividades vencidas sin entrega, nota necesaria que ya no alcanza 7,0. Sin puntaje opaco, sin las palabras riesgo o predicción. Cada criterio enlaza a sus datos y el asistente solo lo explica. Es un orden de atención, no un pronóstico.
- L4 · Acciones sugeridas como botones. Tras una respuesta, de 1 a 3 botones que genera el código según el tipo de respuesta: «Armar plan de refuerzo para …», «Abrir ficha», «Ver la actividad», «Preguntar por el tema». Cada botón abre una pantalla o envía una pregunta predefinida. No envían nada a estudiantes ni escriben en Moodle.
- L5 · Más filtros en el backend, con el patrón de HU-23 (el modelo nunca filtra ni ordena listas): sin entrega por actividad, con ceros, por tema, por unidad, los N primeros, quiénes subieron o bajaron más. Cada filtro nuevo se detalla en `docs/contratos-api.md`.
- L6 · Comparaciones deterministas en el backend: estudiante frente al curso, y una actividad frente a otra. El asistente redacta con ese resultado.
- L7 · Verificación ampliada. Además de las cifras, se verifica que los nombres de estudiantes y de actividades de la respuesta existan en lo que devolvieron las herramientas. Si no, se redacta otra vez y, si persiste, se marca no verificada.
- L8 · Regresión. Un archivo pytest con 30 mensajes (enrutador, V1, V3, L1, L7) que corre en la integración continua. Las 18 preguntas de «Calidad del asistente» no cambian.

Orden: C8.0, V1, V3, V2, V4, V5, V6, L1, L2, L5, L4, L3, V7, V8, L6, L7, V9. Una rama y un PR por grupo de 2 a 4 ítems, de 30 archivos como máximo. Al terminar, repite la línea base y escribe en el resumen final la tabla de antes y después por ruta, con los números reales, y qué ítems revertiste y por qué.

## Bloque D · Calidad del asistente con el modelo local

Solo modelo local, por la decisión 6a. La última corrida dio 14 de 18 aciertos y 11,7 s por pregunta; el objetivo es subir esos aciertos corrigiendo causas reales.

1. Comprueba en pantalla con el modelo externo sin configurar: «Estado del sistema» muestra el modelo local como el activo y el externo como no configurado, sin error; cada respuesta dice «Respondió: modelo local (respaldo)» o la leyenda que corresponda; el interruptor «Usar solo el modelo local» sigue funcionando. Si alguna pantalla trata la falta de API key como un fallo (mensaje rojo, error 5xx, traza con error), corrígelo con prueba: sin key es un estado normal.
2. «Calidad del asistente»: corre las 18 preguntas con el modelo local. La columna del modelo externo debe decir «No configurado» y no «No respondió» ni contar como fallos.
3. Analiza cada pregunta que falle. Si la causa está en el código (herramienta, prompt, verificación de cifras, filtro, orden), corrígela con prueba. No cambies la respuesta esperada de una pregunta para que pase. Después de cada corrección, repite la corrida. Máximo 5 corridas completas; cada una tarda unos 4 minutos.
4. Tras el último cambio, guarda la corrida final (aciertos de 18, datos inventados, tiempo medio) en `docs/evidencias/sprint-3/HU-18/` y en el resumen final. Si quedan preguntas sin pasar, di cuáles son y por qué.
5. Las respuestas con nombres de estudiantes siguen mostrando nombres en pantalla.

## Bloque E · Recorrido completo

Recorre `docs/PRUEBAS.md#sprint-3` entero en la aplicación, con doc01 en AGIL-4B, doc02 y admin. Suma al recorrido las pantallas de los bloques B y C. Verifica también:

- 401 («Tu sesión venció»), 403 («Acceso no permitido») y los errores del agente caído y de Ollama caído («El asistente no está disponible», 503), todos en pantalla;
- el libro contra Moodle en 3 notas, con cero, sin entrega y pendiente diferenciados;
- una sincronización completa y el aviso con las cifras de cambios.

Todo fallo que encuentres se corrige con prueba antes de seguir.

## Bloque F · Capturas

Van al final del trabajo de código, con las imágenes de Docker reconstruidas con el código final (`docker compose up -d --build`).

1. Método: si tienes herramientas de navegador, úsalas. Si no, instala Playwright en una carpeta temporal fuera del repositorio y corre un script desde ahí. No agregues Playwright a `package.json`.
2. Viewport de 1440 × 900. Sesión con doc01 en AGIL-4B, o admin en las pantallas de administración. Credenciales de prueba del README, no de `.env`.
3. Una captura por criterio, con el nombre que fija el `README.md` de cada carpeta en `docs/evidencias/sprint-3/HU-xx/`. Para lo nuevo, agrega criterios a esos README:
   - HU-22: CA-4, avisos por criterio (C1 y C2) y el botón «Pedir enunciado corregido».
   - HU-15: CA-6, grupos por tema y plan grupal (C4); CA-7, borrador «Con tu estilo» (C5).
   - HU-16: informe con «Refuerzos aplicados» (C3).
   - HU-18: tabla de «Calidad del asistente» con la corrida final del modelo local (bloque D).
   - HU-23: «Estado del sistema» con el modelo local activo; respuesta con «Respondió: modelo local»; el interruptor «Usar solo el modelo local» activado; conversación con pregunta de datos, de aplicación y general (C7).
   - HU-22: la barra compacta en el libro, en el mapa y en el asistente, y el panel «Período» abierto (C6). Sirven para mostrar que la cabecera ya no ocupa la pantalla.
   - HU-08: mapa del curso con el mapeo de AGIL-4B (bloque B).
4. En cada `README.md`, reemplaza «Capturas pendientes» por una tabla con el enlace a cada archivo y el dato observado, copiado de lo que muestra la pantalla (por ejemplo, el nombre del estudiante y la nota). No describas nada que no hayas visto.
5. Revisa cada imagen antes de guardarla: sin API key, sin tokens, sin contraseñas.
6. Si un criterio no se puede mostrar con los datos reales, deja su fila marcada «no se pudo capturar» con el motivo.

## Bloque G · Documentos

1. `docs/sprint-3/PLAN.md`: estado de cada HU y de cada TA (hechas, quién las hizo), las tareas que Kevin retomó (TA-22.A1, TA-22.A2, TA-15.B1, TA-15.B2, TA-15.B3) y TA-16.A1. Quita las menciones a la línea de corte que ya no aplican.
2. `docs/contratos-api.md`: los contratos de los bloques B y C.
3. `docs/PRUEBAS.md`: pasos para las pantallas nuevas.
4. Matriz RF/RNF y registro de contribuciones: actualízalos con lo que se hizo en esta guía. En contribuciones pon quién lo hizo de verdad, sin repartir autoría para igualar «Planificada para».
5. `README.md`: pasos para repetir el mapeo de temas y para correr la evaluación con el modelo local. Aclara que el modelo externo está implementado pero no se activó en este sprint por falta de API key, y que el sistema funciona completo con el modelo local.
6. No toques `docs/sprint-3/daily.md`.

## Bloque H · Release a `main`

Solo cuando A a G estén terminados, con la integración continua en verde y todas las pruebas pasando.

1. `main` tiene commits que `develop` no tiene. Trae `main` a `develop` primero con un PR (`git merge origin/main`), resuelve los conflictos conservando el contenido de `develop` y comprueba que las tres suites siguen en verde.
2. Crea `release/sprint-3` desde `develop`. Sigue el procedimiento que usó `release/sprint-2.1` (revisa sus PR y commits): versión en el README y en el detalle de `GET /api/admin/estado`, y el cambio de versión que tocó entonces. La versión del Sprint 3 es 0.3.0, salvo que `release/sprint-2.1` muestre otro patrón.
3. PR de `release/sprint-3` a `main`, integración continua en verde, fusión con el bypass del ruleset. Crea el tag `v0.3.0`.
4. Confirma que `develop` y `main` quedan con el mismo contenido (`git diff main develop` vacío). No borres ramas.
5. Deja el `docker compose up -d --build` funcionando con la versión final.

## Resumen final

Al terminar, entrega este resumen, en prosa y con datos exactos:

1. Archivos cambiados por PR (número de PR y de archivos).
2. Resultado de las tres suites, con las cifras.
3. Resultado de «Calidad del asistente» con el modelo local: aciertos de 18, datos inventados y tiempo medio, y qué preguntas siguen sin pasar.
4. Qué se pudo capturar y qué no, con el motivo.
5. Qué añadidos quedaron completos y cuáles no, y por qué. Incluye C6 (cabecera: librerías añadidas y cuánto subió el paquete), C7 (causa de las cifras distintas entre el texto y las tarjetas, y cómo respondió el modelo local a las preguntas generales) y C8 (tabla de tiempos antes y después por ruta, con los números medidos, y qué mejoras se revirtieron).
6. Lo que le toca a Kevin: el informe, el video, el ensayo y el paquete de OneDrive.
7. Cualquier dato de Moodle que habría que corregir y que no corregiste.
