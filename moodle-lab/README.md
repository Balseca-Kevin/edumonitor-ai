# Moodle de laboratorio

Moodle 4.x con datos ficticios que replica el modelo académico de la UTA. Lo levanta `docker-compose.yml` de esta carpeta (TA-01.1, Kevin).

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

## Respaldos (carpeta `backups/`)

1. `cursos_base.mbz`: categorías y cursos vacíos (Sebastián).
2. `cursos_estructura.mbz`: libro, actividades y usuarios (Santiago).
3. `curso_final.mbz`: con calificaciones y casos de prueba (Jeremy).

## Restaurar desde cero

Pendiente (TA-01.6).
