<?php
// TA-08.A1 (HU-08) · Sílabos en el Moodle de laboratorio (docs/sprint-3/silabos.md). Idempotente: se puede correr varias veces.
//
// En AGIL-4B, MCS-4B y BDD-4B:
//   - nombre de cada sección con el nombre de la unidad del sílabo,
//   - resumen de cada sección con sus temas, como lista (<ul><li>1.1 …</li></ul>); la sincronización de EduMonitor los lee de
//     core_course_get_contents y los toma como temas iniciales de la unidad (HU-08),
//   - BDD-4B suma la sección 5 «Procesos transaccionales con bloques PL/SQL», sin actividades.
// En AGIL-4B, las 5 tareas con rúbrica (scripts/cargar_rubricas.php):
//   - enunciado y archivo de instrucciones alineados con los temas de su unidad,
//   - descripción de cada criterio de la rúbrica alineada con esos temas,
//   - comentario del docente rehecho con los criterios nuevos, según el nivel que ya tiene cada criterio.
//
// No cambia ninguna nota: no toca niveles, puntajes ni calificaciones, solo textos. Los casos de la demo siguen iguales:
//   - est04 falla el mismo tema en 3 actividades: el criterio 1 de Tarea 1, Tarea 3 y Tarea 5 evalúa los valores del Manifiesto Ágil
//     (tema 1.3), cada uno aplicado al tema de su unidad;
//   - casi todo el curso falla el criterio 2 de Tarea 3 (2.2 Enfoques ágiles);
//   - «Formato y entrega» sigue sin relación con la nota.
//
// Uso (desde la raíz del repo, después de cargar_datos_laboratorio.php y cargar_rubricas.php):
//   docker cp moodle-lab/scripts/cargar_silabos.php aula-virtual-moodle-1:/tmp/
//   docker exec aula-virtual-moodle-1 php /tmp/cargar_silabos.php

define('CLI_SCRIPT', true);
require('/var/www/html/config.php');
require_once($CFG->dirroot . '/course/lib.php');
require_once($CFG->dirroot . '/mod/assign/locallib.php');

\core\session\manager::set_user(get_admin());

// Id de Moodle de cada curso (los que espera SembradorDatos) => [nombres cortos aceptados, unidades del sílabo].
// En el aula virtual actual el curso 3 se llama BDD-5B; se reconoce por su id.
const SILABOS = [
    2 => [['AGIL-4B'], [
        1 => ['Principios fundamentales de las metodologías ágiles', ['1.1 Evolución del software', '1.2 Etapas del proceso de desarrollo de software', '1.3 Manifiesto Ágil', '1.4 Agilidad']],
        2 => ['Filosofía y enfoques ágiles de la actualidad', ['2.1 Filosofía ágil', '2.2 Enfoques ágiles']],
        3 => ['Principales metodologías ágiles del mercado', ['3.1 XP, Programación Extrema', '3.2 Scrum', '3.3 Kanban', '3.4 Otras metodologías']],
        4 => ['Herramientas ágiles del mercado', ['4.1 Herramientas para Scrum', '4.2 Herramientas para XP', '4.3 Herramientas para Kanban', '4.4 Herramientas para otras metodologías']],
    ]],
    3 => [['BDD-4B', 'BDD-5B'], [
        1 => ['Esquema conceptual', ['1.1 Esquema conceptual']],
        2 => ['Esquema lógico', ['2.1 Esquema lógico']],
        3 => ['Normalización', ['3.1 Normalización']],
        4 => ['Esquema físico en SQL', ['4.1 Esquema físico']],
        5 => ['Procesos transaccionales con bloques PL/SQL', ['5.1 Bloques PL/SQL']],
    ]],
    4 => [['MCS-4B'], [
        1 => ['Generalidades de la administración de la configuración del software', ['1.1 Introducción a la ACS', '1.2 Roles y beneficios de la ACS']],
        2 => ['Repositorio ACS y sus herramientas', ['2.1 Sistema de control de versiones']],
        3 => ['Fases del proceso de ACS', ['3.1 Proceso ACS', '3.2 Herramientas ACS']],
        4 => ['Administración de la configuración para WebApps', ['4.1 ACS para WebApps']],
    ]],
];

// Tareas con rúbrica de AGIL-4B: enunciado y criterios en el orden de la rúbrica (descripción, qué revisar si quedó bajo).
// El orden y la marca de cada criterio no cambian (1: caso de est04 en Tarea 1, 3 y 5; 2 de Tarea 3: casi todo el curso; 4: no separa).
const TAREAS = [
    'Tarea 1' => [
        'enunciado' => 'Elaboren una línea de tiempo de la evolución del software desde el modelo en cascada hasta las metodologías ágiles, describan las etapas del proceso de desarrollo de software en un proyecto de la facultad y expliquen con ejemplos los 4 valores y los 12 principios del Manifiesto Ágil.',
        'criterios' => [
            ['Valores y principios del Manifiesto Ágil', 'explica cada valor del Manifiesto Ágil con un ejemplo y relaciona los principios con el proyecto'],
            ['Evolución del software', 'ubica cada modelo en la línea de tiempo con el problema que intentó resolver'],
            ['Etapas del proceso de desarrollo de software', 'describe qué entra y qué sale de cada etapa del proceso'],
            ['Formato y entrega', 'sigue la plantilla de entrega del curso'],
        ],
    ],
    'Tarea 2' => [
        'enunciado' => 'Comparen el proceso tradicional y un proceso ágil en el mismo proyecto: cómo cambia cada etapa, qué significa agilidad para el equipo y cómo se trabaja en ciclos iterativos e incrementales.',
        'criterios' => [
            ['Concepto de agilidad', 'define la agilidad con la capacidad de responder al cambio, no con la rapidez'],
            ['Proceso tradicional frente a proceso ágil', 'compara etapa por etapa lo que cambia entre los dos procesos'],
            ['Ciclo iterativo e incremental', 'muestra qué se entrega al final de cada iteración'],
            ['Formato y entrega', 'sigue la plantilla de entrega del curso'],
        ],
    ],
    'Tarea 3' => [
        'enunciado' => 'Analicen la filosofía ágil de un equipo real a partir de los valores del Manifiesto Ágil y comparen dos enfoques ágiles de la actualidad (por ejemplo Lean y DevOps): en qué se parecen, en qué se diferencian y cuándo conviene cada uno.',
        'criterios' => [
            ['Valores del Manifiesto Ágil en la filosofía del equipo', 'relaciona cada práctica del equipo con el valor del Manifiesto Ágil que la sostiene'],
            ['Comparación de enfoques ágiles', 'compara los enfoques con los mismos criterios y concluye cuándo conviene cada uno'],
            ['Filosofía ágil en un caso real', 'identifica en el caso cómo el equipo prioriza a las personas y la colaboración'],
            ['Formato y entrega', 'sigue la plantilla de entrega del curso'],
        ],
    ],
    'Tarea 5' => [
        'enunciado' => 'Diseñen un sprint de Scrum para el proyecto de la facultad mostrando qué valor del Manifiesto Ágil sostiene cada práctica, organicen el trabajo en un tablero Kanban con límites WIP y propongan dos prácticas de XP para el equipo.',
        'criterios' => [
            ['Valores del Manifiesto Ágil en las prácticas de Scrum', 'cada práctica de Scrum debe nombrar el valor del Manifiesto Ágil que la sostiene'],
            ['Tablero Kanban y límite WIP', 'el límite WIP debe estar en cada columna y justificarse'],
            ['Prácticas de XP', 'explica cómo se aplicaría cada práctica de XP en el equipo'],
            ['Formato y entrega', 'sigue la plantilla de entrega del curso'],
        ],
    ],
    'Tarea 6' => [
        'enunciado' => 'Describan los eventos y artefactos de Scrum del proyecto, conduzcan una retrospectiva con el formato «qué funcionó, qué no, qué cambiamos» y comparen Scrum con otra metodología ágil del mercado (Crystal, FDD o DSDM).',
        'criterios' => [
            ['Eventos y artefactos de Scrum', 'indica el objetivo y el responsable de cada evento y artefacto'],
            ['Retrospectiva y mejora continua', 'cada mejora acordada necesita un responsable'],
            ['Otras metodologías ágiles', 'compara la otra metodología con Scrum en roles, ciclos y entregables'],
            ['Formato y entrega', 'sigue la plantilla de entrega del curso'],
        ],
    ],
];

foreach (SILABOS as $moodleId => [$cortos, $unidades]) {
    $curso = $DB->get_record('course', ['id' => $moodleId]);
    if (!$curso || !in_array($curso->shortname, $cortos, true)) {
        echo 'Curso ' . implode('/', $cortos) . " (id $moodleId) no está; se omite." . PHP_EOL;
        continue;
    }

    $creadas = 0;
    foreach ($unidades as $numero => [$nombre, $temas]) {
        $seccion = $DB->get_record('course_sections', ['course' => $curso->id, 'section' => $numero]);
        if (!$seccion) {
            $seccion = course_create_section($curso, $numero);
            $creadas++;
        }
        $resumen = '<ul>' . implode('', array_map(fn($t) => '<li>' . s($t) . '</li>', $temas)) . '</ul>';
        if ($seccion->name !== $nombre || $seccion->summary !== $resumen) {
            course_update_section($curso, $seccion, ['name' => $nombre, 'summary' => $resumen, 'summaryformat' => FORMAT_HTML]);
        }
    }
    echo sprintf('%s · %d secciones con el nombre y los temas del sílabo%s', $curso->shortname, count($unidades),
        $creadas ? " · $creadas sección nueva" : '') . PHP_EOL;
}

$agil = $DB->get_record('course', ['id' => 2, 'shortname' => 'AGIL-4B']);
foreach ($agil ? TAREAS : [] as $nombre => $config) {
    $registro = $DB->get_record('assign', ['course' => $agil->id, 'name' => $nombre]);
    if (!$registro) {
        echo "$nombre · no está en AGIL-4B; se omite" . PHP_EOL;
        continue;
    }
    $cm = get_coursemodule_from_instance('assign', $registro->id, $agil->id, false, MUST_EXIST);
    $contexto = context_module::instance($cm->id);

    poner_enunciado($registro, $contexto, $config['enunciado'], $nombre);

    $area = $DB->get_record('grading_areas', ['contextid' => $contexto->id, 'component' => 'mod_assign', 'areaname' => 'submissions']);
    $definicion = $area ? $DB->get_record('grading_definitions', ['areaid' => $area->id, 'method' => 'rubric']) : null;
    if (!$definicion) {
        echo "$nombre · enunciado actualizado; sin rúbrica (corre antes cargar_rubricas.php)" . PHP_EOL;
        continue;
    }

    // Criterios por orden de la rúbrica: solo cambia la descripción.
    $criterios = array_values($DB->get_records('gradingform_rubric_criteria', ['definitionid' => $definicion->id], 'sortorder'));
    foreach ($criterios as $i => $criterio) {
        if (isset($config['criterios'][$i]) && $criterio->description !== $config['criterios'][$i][0]) {
            $DB->update_record('gradingform_rubric_criteria', (object) ['id' => $criterio->id, 'description' => $config['criterios'][$i][0]]);
        }
    }
    $posicion = array_flip(array_map(fn($c) => $c->id, $criterios));

    // Comentario del docente rehecho con el nivel que ya tiene cada criterio (puntaje 0 o 1: hay que revisarlo).
    $comentarios = 0;
    foreach ($DB->get_records('grading_instances', ['definitionid' => $definicion->id, 'status' => 1]) as $instancia) {
        $rellenos = $DB->get_records_sql(
            'SELECT f.criterionid, l.score FROM {gradingform_rubric_fillings} f
               JOIN {gradingform_rubric_levels} l ON l.id = f.levelid
              WHERE f.instanceid = ?', [$instancia->id]);
        $revisar = [];
        foreach ($rellenos as $relleno) {
            $i = $posicion[$relleno->criterionid] ?? null;
            if ($i !== null && (float) $relleno->score <= 1) {
                $revisar[$i] = '<li>' . s($config['criterios'][$i][0]) . ': ' . s($config['criterios'][$i][1]) . '.</li>';
            }
        }
        ksort($revisar);
        $texto = $revisar
            ? '<p>Revisa estos puntos antes de la siguiente entrega:</p><ul>' . implode('', $revisar) . '</ul>'
            : '<p>Buen trabajo: cumpliste todos los criterios de la rúbrica.</p>';
        $existente = $DB->get_record('assignfeedback_comments', ['assignment' => $registro->id, 'grade' => $instancia->itemid]);
        if ($existente && $existente->commenttext !== $texto) {
            $DB->update_record('assignfeedback_comments', (object) ['id' => $existente->id, 'commenttext' => $texto, 'commentformat' => FORMAT_HTML]);
        }
        $comentarios += $existente ? 1 : 0;
    }
    echo sprintf('%s · enunciado, %d criterios y %d comentarios alineados con el sílabo (las notas no cambian)',
        $nombre, count($criterios), $comentarios) . PHP_EOL;
}

rebuild_course_cache(0, true);
echo 'Listo. Sincroniza los cursos en EduMonitor para traer los nombres y los temas de las unidades.' . PHP_EOL;

/** Enunciado y archivo «Instrucciones <tarea>.txt» con el texto nuevo (área introattachment de mod_assign). */
function poner_enunciado(stdClass $registro, context_module $contexto, string $enunciado, string $nombre): void {
    global $DB;
    $intro = '<p>' . s($enunciado) . '</p>';
    if ($registro->intro !== $intro) {
        $DB->update_record('assign', (object) ['id' => $registro->id, 'intro' => $intro, 'introformat' => FORMAT_HTML]);
    }
    $contenido = "$nombre · Metodologías Ágiles (lab)\n\n$enunciado\n\nEntrega: un documento en la tarea de Moodle antes de la fecha límite.\n";
    $archivos = get_file_storage();
    $ruta = [$contexto->id, 'mod_assign', ASSIGN_INTROATTACHMENT_FILEAREA, 0, '/', "Instrucciones $nombre.txt"];
    $actual = $archivos->get_file(...$ruta);
    if ($actual && $actual->get_content() === $contenido) {
        return;
    }
    if ($actual) {
        $actual->delete();
    }
    $archivos->create_file_from_string(array_combine(['contextid', 'component', 'filearea', 'itemid', 'filepath', 'filename'], $ruta), $contenido);
}
