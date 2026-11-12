# Daily Scrum · Sprint 2

Una entrada por persona y por día, antes de las 10:00. Formato fijo. Este archivo es evidencia del informe: no se reescriben entradas pasadas.

## Plantilla

### AAAA-MM-DD · Nombre

- Hice: TA-xx.x …
- Haré: TA-xx.x …
- Bloqueo: ninguno | descripción y a quién se le pidió ayuda

---

### 2026-11-11 · Kevin Balseca

- Hice:
  - TA-09.1 a 09.5: indicadores y «Panel del curso» (#23 y #26).
  - TA-13.1 y su resumen por estudiante, para que la herramienta quepa en el contexto del modelo (#24 y #28).
  - TA-14.1 a 14.4: asistente en la app (#25).
  - TA-20.1 a 20.6: entorno con Docker, «Estado del sistema», configuración de Moodle persistente y README de arranque (#27), y los scripts de inicio y apagado (#29).
  - Integré #21 (Jeremy, HU-13 en el agente) y #22. Cerré #16 y #20 sin fusionar.
  - Evidencias del Sprint 2 (#30) y actualización de la documentación (#31).
- Haré: release `release/sprint-2`, fusión a `main` con la etiqueta `v0.2.0`, y ensayo del guion de la Review del jueves 12.
- Bloqueo:
  - TA-13.2 espera HU-12 (Sebastián); HU-12 y HU-19 (Santiago) no tienen Pull Request.
  - Con `qwen2.5:7b`, la pregunta por las notas de un estudiante se responde con el resumen y no trae el detalle por actividad.

### 2026-11-12 · Kevin Balseca

- Hice:
  - HU-14: una pregunta con otro código de curso se responde sin llamar al agente (#36).
  - HU-13:
    - búsqueda por nombre en la herramienta de calificaciones, como parte del trabajo de Jeremy (#37);
    - TA-13.2, `GET /api/tools/alertas` en forma corta (#39);
    - TA-13.7, evaluación con 10 preguntas al asistente real (#40).
  - HU-12 de Sebastián: revisión, corrección de reglas por defecto y de alertas revisadas, y fusión (#38).
  - HU-19 de Santiago: diseño D en toda la aplicación sobre su PR y fusión (#35). El PR también trae el historial del asistente en la API y en el chat.
  - Cierre de la documentación y release v0.2.1.
- Haré: Sprint Review con el guion de `docs/PRUEBAS.md`.
- Bloqueo:
  - Ninguno.
  - Riesgo para la demo: si Ollama pierde la GPU responde lento; se reinicia el contenedor `ollama` antes de la Review y se cierra Brave para liberar memoria de video.
