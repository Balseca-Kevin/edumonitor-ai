<?php
// Sprint 3 · dependencia del lunes 16 nov 2026 (Kevin) · Rúbricas, comentarios del docente y enunciados en AGIL-4B.
// Lo necesitan HU-08 (Sebastián: mapeo de criterios a temas), HU-22 (panel de actividad), HU-10 y HU-15 (comentarios).
//
// En 5 tareas de AGIL-4B (Tarea 1, Tarea 2, Tarea 3, Tarea 5 y Tarea 6):
//   - enunciado completo y un archivo adjunto con las instrucciones (mod_assign_get_assignments: intro e introattachments),
//   - rúbrica de 4 criterios con 4 niveles (0 · 1 · 2 · 2,5 puntos; total 10) como método de calificación activo
//     (core_grading_get_definitions),
//   - cada nota existente se califica con la rúbrica (core_grading_get_gradingform_instances): se eligen los niveles cuya suma
//     queda más cerca de la nota de Moodle y la nota pasa a ser esa suma (la diferencia se imprime; como mucho unas décimas),
//   - un comentario del docente en cada entrega calificada (mod_assign_get_submission_status, plugin «comments»).
// Sin nota (sin entrega o pendiente de calificar) no hay rúbrica ni comentario: siguen igual.
//
// Casos fijos para la demostración (docs/sprint-3/PLAN.md):
//   - est04 falla el tema «Historias de usuario» en 3 actividades: Tarea 1, Tarea 3 y Tarea 5 (nivel Insuficiente o En proceso).
//   - Tarea 3 · «Estimación con puntos de historia»: casi todo el curso queda en Insuficiente o En proceso (TA-22.A1: «revisar
//     cómo se enseñó»).
//   - «Formato y entrega»: Logrado o Destacado al azar, sin relación con la nota total (TA-22.A1: «este criterio no separa a
//     quienes van bien del resto»).
//   - La nota 0 de est05 en Tarea 3 queda en 0 (todos los criterios en Insuficiente).
//
// Idempotente: una tarea que ya tiene la rúbrica activa con calificaciones no se vuelve a tocar. Semilla fija.
// No cambia BDD-4B ni MCS-4B.
//
// Uso (desde la raíz del repo, con los datos de cargar_datos_laboratorio.php ya cargados):
//   docker cp moodle-lab/scripts/cargar_rubricas.php aula-virtual-moodle-1:/tmp/
//   docker exec aula-virtual-moodle-1 php /tmp/cargar_rubricas.php

define('CLI_SCRIPT', true);
require('/var/www/html/config.php');
require_once($CFG->libdir . '/gradelib.php');
require_once($CFG->dirroot . '/mod/assign/locallib.php');
require_once($CFG->dirroot . '/grade/grading/lib.php');
require_once($CFG->dirroot . '/grade/grading/form/rubric/lib.php');

\core\session\manager::set_user(get_admin());
// El laboratorio no envía correos: sin esto cada calificación intenta notificar al estudiante.
$CFG->noemailever = true;

const CURSO = 'AGIL-4B';
const DOCENTE = 'doc01';
const ESTUDIANTE_DEMO = 'est04';
const SEMILLA = 4040;

// Nivel => [puntos, nombre]. 4 criterios × 2,5 = 10, la escala de las tareas.
const NIVELES = [[0, 'Insuficiente'], [1, 'En proceso'], [2, 'Logrado'], [2.5, 'Destacado']];

// Marcas de los criterios: 'demo' (est04 lo falla), 'mayoria' (casi todo el curso lo falla), 'no_separa' (no depende de la nota).
// El tema que va entre corchetes en el comentario es la sugerencia para el mapeo de HU-08; el docente lo decide en EduMonitor.
const RUBRICAS = [
    'Tarea 1' => [
        'enunciado' => 'Redacten 6 historias de usuario para el sistema de préstamos de la biblioteca de la facultad, con el formato «Como [rol], se requiere [acción] para [beneficio]». Identifiquen el Product Owner, el equipo y los artefactos de Scrum del proyecto y prioricen el backlog con MoSCoW.',
        'criterios' => [
            ['Redacción de historias de usuario', 'demo', 'cada historia necesita el rol, la acción y el beneficio («Como…, se requiere… para…»)'], // [Historias de usuario]
            ['Roles y artefactos de Scrum', '', 'distingue qué decide el Product Owner y qué decide el equipo'], // [Marco Scrum]
            ['Priorización del Product Backlog', '', 'justifica el orden del backlog con el valor para el usuario'], // [Product Backlog]
            ['Formato y entrega', 'no_separa', 'sigue la plantilla de entrega del curso'], // [Presentación]
        ],
    ],
    'Tarea 2' => [
        'enunciado' => 'A partir del backlog de la Tarea 1, ordenen las historias por valor y riesgo, escriban la Definición de Terminado del equipo y describan qué ocurre en cada evento de Scrum durante el primer sprint.',
        'criterios' => [
            ['Priorización del Product Backlog', '', 'justifica el orden del backlog con el valor para el usuario'], // [Product Backlog]
            ['Definición de terminado', '', 'la Definición de Terminado debe poder comprobarse en cada historia'], // [Marco Scrum]
            ['Eventos de Scrum', '', 'indica el objetivo y la duración de cada evento'], // [Marco Scrum]
            ['Formato y entrega', 'no_separa', 'sigue la plantilla de entrega del curso'], // [Presentación]
        ],
    ],
    'Tarea 3' => [
        'enunciado' => 'Escriban los criterios de aceptación (Dado, Cuando, Entonces) de las 4 historias de mayor prioridad, estímenlas con puntos de historia en la serie de Fibonacci y armen el Sprint Backlog del primer sprint con su objetivo.',
        'criterios' => [
            ['Criterios de aceptación de las historias', 'demo', 'escribe cada criterio como Dado, Cuando, Entonces y que se pueda probar'], // [Historias de usuario]
            ['Estimación con puntos de historia', 'mayoria', 'los puntos comparan el esfuerzo entre historias; no son horas'], // [Estimación]
            ['Planificación del sprint', '', 'el objetivo del sprint debe unir las historias elegidas'], // [Planificación]
            ['Formato y entrega', 'no_separa', 'sigue la plantilla de entrega del curso'], // [Presentación]
        ],
    ],
    'Tarea 5' => [
        'enunciado' => 'Dividan las 2 historias más grandes del backlog con los criterios INVEST, organicen el trabajo en un tablero Kanban con límites WIP y presenten la velocidad y el burndown de los dos sprints simulados.',
        'criterios' => [
            ['División de historias con INVEST', 'demo', 'cada historia dividida debe entregar valor por sí sola (INVEST)'], // [Historias de usuario]
            ['Tablero Kanban y límite WIP', '', 'el límite WIP debe estar en cada columna y justificarse'], // [Kanban]
            ['Velocidad y burndown', '', 'la velocidad sale de los puntos terminados, no de los planificados'], // [Métricas]
            ['Formato y entrega', 'no_separa', 'sigue la plantilla de entrega del curso'], // [Presentación]
        ],
    ],
    'Tarea 6' => [
        'enunciado' => 'Estimen el siguiente sprint con Planning Poker (registren las rondas), conduzcan una retrospectiva con el formato «qué funcionó, qué no, qué cambiamos» y comparen la velocidad con la del sprint anterior.',
        'criterios' => [
            ['Estimación con Planning Poker', '', 'registra las rondas y cómo se llegó al consenso'], // [Estimación]
            ['Retrospectiva y mejora continua', '', 'cada mejora acordada necesita un responsable'], // [Mejora continua]
            ['Velocidad y burndown', '', 'la velocidad sale de los puntos terminados, no de los planificados'], // [Métricas]
            ['Formato y entrega', 'no_separa', 'sigue la plantilla de entrega del curso'], // [Presentación]
        ],
    ],
];

$curso = $DB->get_record('course', ['shortname' => CURSO], '*', MUST_EXIST);
$docente = $DB->get_record('user', ['username' => DOCENTE], '*', MUST_EXIST);
$contextoCurso = context_course::instance($curso->id);
$estudiantes = array_values(array_filter(
    get_enrolled_users($contextoCurso, '', 0, 'u.id, u.username', 'u.username'),
    fn($u) => str_starts_with($u->username, 'est')));

mt_srand(SEMILLA);
foreach (RUBRICAS as $nombre => $config) {
    $registro = $DB->get_record('assign', ['course' => $curso->id, 'name' => $nombre], '*', MUST_EXIST);
    $cm = get_coursemodule_from_instance('assign', $registro->id, $curso->id, false, MUST_EXIST);
    $contexto = context_module::instance($cm->id);
    $assign = new assign($contexto, $cm, $curso);

    $manager = get_grading_manager($contexto, 'mod_assign', 'submissions');
    $previo = $manager->get_active_method() === 'rubric' ? $manager->get_controller('rubric') : null;
    if ($previo && $previo->is_form_defined() && $DB->record_exists('grading_instances', ['definitionid' => $previo->get_definition()->id])) {
        echo "$nombre · ya tiene rúbrica con calificaciones (no se toca)" . PHP_EOL;
        continue;
    }

    poner_enunciado($registro, $contexto, $config['enunciado'], $nombre);

    // Rúbrica como método de calificación activo.
    $manager->set_active_method('rubric');
    $controlador = $manager->get_controller('rubric');
    $definicion = new stdClass();
    $definicion->name = "Rúbrica · $nombre";
    $definicion->description_editor = ['text' => '', 'format' => FORMAT_HTML, 'itemid' => file_get_unused_draft_itemid()];
    $definicion->status = gradingform_controller::DEFINITION_STATUS_READY;
    $criterios = [];
    foreach ($config['criterios'] as $i => [$descripcion]) {
        $niveles = [];
        foreach (NIVELES as $j => [$puntos, $nivel]) {
            $niveles['NEWID' . ($j + 1)] = ['score' => $puntos, 'definition' => $nivel];
        }
        $criterios['NEWID' . ($i + 1)] = ['sortorder' => $i + 1, 'description' => $descripcion, 'levels' => $niveles];
    }
    $definicion->rubric = ['criteria' => $criterios, 'options' => gradingform_rubric_controller::get_default_options()];
    $controlador->update_definition($definicion, $docente->id);

    $controlador = $manager->get_controller('rubric');
    $controlador->set_grade_range(make_grades_menu((int) $registro->grade), true);
    // Ids de Moodle: criterio por orden y nivel por puntos.
    $ids = [];
    foreach ($controlador->get_definition(true)->rubric_criteria as $criterioId => $criterio) {
        $niveles = [];
        foreach ($criterio['levels'] as $nivelId => $nivel) {
            $niveles[(string) (float) $nivel['score']] = $nivelId;
        }
        $ids[(int) $criterio['sortorder'] - 1] = ['id' => $criterioId, 'niveles' => $niveles];
    }

    $comentarios = $assign->get_feedback_plugin_by_type('comments');
    if (!$comentarios->is_enabled()) {
        $comentarios->enable();
    }

    $calificadas = 0;
    $diferencias = [];
    foreach ($estudiantes as $estudiante) {
        $nota = $DB->get_record('assign_grades', ['assignment' => $registro->id, 'userid' => $estudiante->id, 'attemptnumber' => 0]);
        if (!$nota || $nota->grade === null || (float) $nota->grade < 0) {
            continue; // sin entrega o pendiente de calificar
        }

        $objetivo = (float) $nota->grade;
        $niveles = elegir_niveles($objetivo, $config['criterios'], $estudiante->username === ESTUDIANTE_DEMO);

        $relleno = ['criteria' => []];
        foreach ($niveles as $i => $nivel) {
            $puntos = (string) (float) NIVELES[$nivel][0];
            $relleno['criteria'][$ids[$i]['id']] = ['levelid' => $ids[$i]['niveles'][$puntos], 'remark' => ''];
        }
        $instancia = $controlador->create_instance($docente->id, $nota->id);
        $nueva = round((float) $instancia->submit_and_get_grade($relleno, $nota->id), 2);

        $calificacion = $assign->get_user_grade($estudiante->id, true);
        $calificacion->grade = $nueva;
        $calificacion->grader = $docente->id;
        $assign->update_grade($calificacion);

        $texto = comentario($config['criterios'], $niveles);
        $existente = $DB->get_record('assignfeedback_comments', ['assignment' => $registro->id, 'grade' => $calificacion->id]);
        if ($existente) {
            $existente->commenttext = $texto;
            $existente->commentformat = FORMAT_HTML;
            $DB->update_record('assignfeedback_comments', $existente);
        } else {
            $DB->insert_record('assignfeedback_comments', (object) [
                'assignment' => $registro->id, 'grade' => $calificacion->id, 'commenttext' => $texto, 'commentformat' => FORMAT_HTML,
            ]);
        }

        $calificadas++;
        $diferencias[] = abs($nueva - $objetivo);
        if ($estudiante->username === ESTUDIANTE_DEMO) {
            echo sprintf('  %s en %s: %s → nota %.2f (antes %.2f)', ESTUDIANTE_DEMO, $nombre,
                implode(' · ', array_map(fn($i, $n) => $config['criterios'][$i][0] . ' ' . NIVELES[$n][1], array_keys($niveles), $niveles)),
                $nueva, $objetivo) . PHP_EOL;
        }
    }

    echo sprintf('%s · rúbrica de %d criterios · %d calificaciones con rúbrica y comentario · diferencia media %.2f, máxima %.2f',
        $nombre, count($config['criterios']), $calificadas,
        $diferencias ? array_sum($diferencias) / count($diferencias) : 0, $diferencias ? max($diferencias) : 0) . PHP_EOL;
}
grade_regrade_final_grades($curso->id);
echo 'Listo. Sincroniza AGIL-4B en EduMonitor para traer las notas nuevas.' . PHP_EOL;

/**
 * Niveles de los 4 criterios (índice de NIVELES) cuya suma queda más cerca de la nota de Moodle. Los criterios marcados se fijan
 * antes: 'no_separa' en Logrado o Destacado al azar, 'mayoria' en Insuficiente o En proceso para el 85 % del curso y 'demo' en
 * Insuficiente o En proceso para el estudiante de la demostración. Una nota 0 deja todos en Insuficiente.
 * Empate: la combinación más pareja entre criterios.
 */
function elegir_niveles(float $objetivo, array $criterios, bool $esDemo): array {
    $fijos = [];
    foreach ($criterios as $i => [, $marca]) {
        if ($marca === 'no_separa') {
            $fijos[$i] = mt_rand(2, 3);
        } elseif ($marca === 'mayoria' && mt_rand(1, 100) <= 85) {
            $fijos[$i] = mt_rand(0, 1);
        } elseif ($marca === 'demo' && $esDemo) {
            $fijos[$i] = mt_rand(0, 1);
        }
    }
    if ($objetivo == 0.0) {
        return array_fill(0, count($criterios), 0);
    }

    $libres = array_values(array_diff(array_keys($criterios), array_keys($fijos)));
    $mejor = null;
    $total = count(NIVELES) ** count($libres);
    for ($k = 0; $k < $total; $k++) {
        $niveles = $fijos;
        $resto = $k;
        foreach ($libres as $i) {
            $niveles[$i] = $resto % count(NIVELES);
            $resto = intdiv($resto, count(NIVELES));
        }
        ksort($niveles);
        $puntos = array_map(fn($n) => NIVELES[$n][0], $niveles);
        $clave = [abs(array_sum($puntos) - $objetivo), max($puntos) - min($puntos)];
        if ($mejor === null || $clave < $mejor[0]) {
            $mejor = [$clave, $niveles];
        }
    }
    return $mejor[1];
}

/** Comentario del docente: qué revisar en los criterios con Insuficiente o En proceso, o un reconocimiento si no hay ninguno. */
function comentario(array $criterios, array $niveles): string {
    $revisar = [];
    foreach ($niveles as $i => $nivel) {
        if ($nivel <= 1) {
            $revisar[] = '<li>' . $criterios[$i][0] . ': ' . $criterios[$i][2] . '.</li>';
        }
    }
    return $revisar
        ? '<p>Revisa estos puntos antes de la siguiente entrega:</p><ul>' . implode('', $revisar) . '</ul>'
        : '<p>Buen trabajo: cumpliste todos los criterios de la rúbrica.</p>';
}

/** Enunciado completo y un archivo de instrucciones adjunto (área introattachment de mod_assign). */
function poner_enunciado(stdClass $registro, context_module $contexto, string $enunciado, string $nombre): void {
    global $DB;
    $DB->update_record('assign', (object) ['id' => $registro->id, 'intro' => "<p>$enunciado</p>", 'introformat' => FORMAT_HTML]);
    $archivos = get_file_storage();
    $archivo = [
        'contextid' => $contexto->id, 'component' => 'mod_assign', 'filearea' => ASSIGN_INTROATTACHMENT_FILEAREA,
        'itemid' => 0, 'filepath' => '/', 'filename' => 'Instrucciones ' . $nombre . '.txt',
    ];
    if (!$archivos->file_exists(...array_values($archivo))) {
        $archivos->create_file_from_string($archivo,
            "$nombre · Metodologías Ágiles (lab)\n\n$enunciado\n\nEntrega: un documento en la tarea de Moodle antes de la fecha límite.\n");
    }
}
