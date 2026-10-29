# Spike Técnico: Rendimiento de Ollama (HU-07)

## Resultados de Evaluación de Modelos

| Modelo | Equipo (CPU/GPU/RAM) | Tiempo 1ª Respuesta | Tiempo Medio (5 preguntas) | ¿Invocó la Herramienta? | ¿Inventó datos con backend caído? |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `qwen2.5:7b` | Intel i7 / 16GB RAM / Integrated GPU | ~2.1s | ~1.8s | Sí (Excelente) | No (Responde error controlado) |
| `llama3.2:3b` | Intel i7 / 16GB RAM / Integrated GPU | ~1.4s | ~1.1s | Sí (Ocasional) | No (Responde error controlado) |

### Decisión Técnica
Se selecciona **`qwen2.5:7b`** como modelo principal por su consistencia comprobada al estructurar llamadas a herramientas (*tool calling*), manteniendo tiempos de respuesta dentro del margen aceptable para el entorno local.