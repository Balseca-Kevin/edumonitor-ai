<?php
// TA-01.2 a TA-01.5 · Carga la estructura académica de la UTA y los datos ficticios del Moodle de laboratorio.
// Idempotente: si los cursos ya existen, no hace nada. Para volver a cargar, «Borrar todo» (moodle-lab/README.md).
//
// Crea (ver «Estructura acordada» en moodle-lab/README.md):
//   - categorías UTA › FISEI › Software,
//   - los cursos AGIL-4B y BDD-4B (ids de Moodle 2 y 3, los que esperan las semillas del backend) con 4 unidades,
//   - el libro de calificaciones Exámenes 40 % · Tareas 30 % · APE 30 % (media ponderada, escala 0 a 10),
//   - 16 tareas por curso (assign) con fecha límite y seguimiento de finalización,
//   - doc01 (AGIL-4B) y doc02 (BDD-4B) como profesores, y est01 a est32 como estudiantes de ambos cursos,
//   - entregas y notas con un caso para cada regla de alerta: descenso, nota cero, sin entrega, pendiente de calificar,
//     promedio bajo, alejado del grupo y baja finalización.
//
// Los nombres son ficticios y los correos usan el dominio reservado edumonitor.test. Las contraseñas son aleatorias:
// nadie entra con estos usuarios, solo existen como datos.
//
// Uso (desde la raíz del repo):
//   docker cp moodle-lab/scripts/cargar_datos_laboratorio.php edumonitor-moodle-1:/tmp/
//   docker exec edumonitor-moodle-1 php /tmp/cargar_datos_laboratorio.php

define('CLI_SCRIPT', true);
require('/var/www/html/config.php');
require_once($CFG->libdir . '/testing/generator/lib.php');
require_once($CFG->libdir . '/gradelib.php');
require_once($CFG->libdir . '/completionlib.php');
require_once($CFG->dirroot . '/mod/assign/locallib.php');

\core\session\manager::set_user(get_admin());
// El laboratorio no envía correos: sin esto cada calificación intenta notificar al estudiante.
$CFG->noemailever = true;

const CURSOS = [
    ['AGIL-4B', 'Metodologías Ágiles (lab)', 'doc01'],
    ['BDD-4B', 'Bases de Datos (lab)', 'doc02'],
];
const ESTUDIANTES = 32;

// Categoría del libro => peso en el total del curso.
const CATEGORIAS = ['Exámenes' => 40, 'Tareas' => 30, 'APE' => 30];

// Unidad => [nombre de la actividad, categoría del libro, fecha límite]. Unidades 1 a 3 ya vencidas; la 4 vence en noviembre.
const ACTIVIDADES = [
    1 => [['Tarea 1', 'Tareas', '2026-09-08'], ['Tarea 2', 'Tareas', '2026-09-15'], ['APE 1', 'APE', '2026-09-18'], ['Taller 1', 'Tareas', '2026-09-22']],
    2 => [['Tarea 3', 'Tareas', '2026-09-29'], ['Tarea 4', 'Tareas', '2026-10-02'], ['APE 2', 'APE', '2026-10-06'], ['Examen Parcial 1', 'Exámenes', '2026-10-09']],
    3 => [['Tarea 5', 'Tareas', '2026-10-14'], ['Tarea 6', 'Tareas', '2026-10-19'], ['APE 3', 'APE', '2026-10-22'], ['Taller 2', 'Tareas', '2026-10-26']],
    4 => [['Tarea 7', 'Tareas', '2026-11-10'], ['Tarea 8', 'Tareas', '2026-11-17'], ['APE 4', 'APE', '2026-11-20'], ['Examen Parcial 2', 'Exámenes', '2026-11-25']],
];

const NOMBRES = ['Ana', 'Bruno', 'Carla', 'Diego', 'Elena', 'Fabián', 'Gabriela', 'Hugo', 'Isabel', 'Javier', 'Karina', 'Luis',
    'María', 'Nicolás', 'Olga', 'Pablo', 'Quilla', 'Raúl', 'Sofía', 'Tomás', 'Úrsula', 'Víctor', 'Wendy', 'Ximena',
    'Yolanda', 'Zacarías', 'Andrea', 'Bernardo', 'Camila', 'Daniel', 'Estefanía', 'Francisco'];
const APELLIDOS = ['Acosta', 'Benítez', 'Cevallos', 'Dávila', 'Espinoza', 'Flores', 'Guerrero', 'Herrera', 'Iza', 'Jácome',
    'Lara', 'Mora', 'Naranjo', 'Ortiz', 'Paredes', 'Quispe', 'Ríos', 'Salazar', 'Torres', 'Uvidia', 'Vaca', 'Villacís',
    'Yépez', 'Zambrano', 'Aguirre', 'Bravo', 'Castro', 'Duque', 'Endara', 'Freire', 'Gómez', 'Haro'];

if ($DB->record_exists('course', ['shortname' => CURSOS[0][0]])) {
    foreach (CURSOS as [$corto]) {
        nombrar_categorias((int) $DB->get_field('course', 'id', ['shortname' => $corto], MUST_EXIST));
    }
    echo 'Los cursos ya existen; solo se revisó el idnumber de las categorías. Para recargar, borrar los volúmenes (moodle-lab/README.md).' . PHP_EOL;
    exit(0);
}

set_config('enablecompletion', 1);
$generador = new testing_data_generator();

// TA-01.2 · Categorías.
$uta = $generador->create_category(['name' => 'UTA', 'idnumber' => 'UTA']);
$fisei = $generador->create_category(['name' => 'FISEI', 'idnumber' => 'FISEI', 'parent' => $uta->id]);
$software = $generador->create_category(['name' => 'Software', 'idnumber' => 'SOFTWARE', 'parent' => $fisei->id]);

// TA-01.4 · Usuarios ficticios.
$docentes = [];
foreach (['doc01' => ['Docente', 'Uno'], 'doc02' => ['Docente', 'Dos']] as $usuario => [$nombre, $apellido]) {
    $docentes[$usuario] = crear_usuario($generador, $usuario, $nombre, $apellido);
}
$estudiantes = [];
for ($n = 1; $n <= ESTUDIANTES; $n++) {
    $usuario = sprintf('est%02d', $n);
    $estudiantes[$n] = crear_usuario($generador, $usuario, NOMBRES[$n - 1], APELLIDOS[$n - 1]);
}

foreach (CURSOS as $indice => [$corto, $nombre, $docente]) {
    // TA-01.2 · Curso con 4 secciones: Parcial 1 (U1, U2) y Parcial 2 (U3, U4).
    $curso = $generador->create_course([
        'shortname' => $corto,
        'fullname' => $nombre,
        'category' => $software->id,
        'format' => 'topics',
        'numsections' => 4,
        'enablecompletion' => 1,
        'startdate' => strtotime('2026-09-01 08:00:00'),
    ]);
    foreach (array_keys(ACTIVIDADES) as $unidad) {
        $parcial = $unidad <= 2 ? 1 : 2;
        $DB->set_field('course_sections', 'name', "Unidad $unidad", ['course' => $curso->id, 'section' => $unidad]);
        $DB->set_field('course_sections', 'summary', "Parcial $parcial", ['course' => $curso->id, 'section' => $unidad]);
    }
    rebuild_course_cache($curso->id, true);

    // TA-01.3 · Libro de calificaciones 40/30/30 en escala 0 a 10.
    $total = grade_category::fetch_course_category($curso->id);
    $total->aggregation = GRADE_AGGREGATE_WEIGHTED_MEAN;
    $total->update();
    $itemtotal = $total->load_grade_item();
    $itemtotal->grademax = 10;
    $itemtotal->update();

    $categorias = [];
    foreach (CATEGORIAS as $categoria => $peso) {
        $registro = $generador->create_grade_category([
            'courseid' => $curso->id,
            'fullname' => $categoria,
            'aggregation' => GRADE_AGGREGATE_MEAN,
        ]);
        $categorias[$categoria] = grade_category::fetch(['id' => $registro->id]);
        $item = $categorias[$categoria]->load_grade_item();
        $item->aggregationcoef = $peso;
        $item->grademax = 10;
        $item->update();
    }
    nombrar_categorias($curso->id);

    // TA-01.3 · Actividades por unidad.
    $tareas = [];
    foreach (ACTIVIDADES as $unidad => $lista) {
        foreach ($lista as [$actividad, $categoria, $fecha]) {
            $modulo = $generador->create_module('assign', [
                'course' => $curso->id,
                'section' => $unidad,
                'name' => $actividad,
                'intro' => "$actividad · Unidad $unidad",
                'grade' => 10,
                'gradecat' => $categorias[$categoria]->id,
                'duedate' => strtotime("$fecha 23:59:00"),
                'allowsubmissionsfromdate' => strtotime('2026-09-01 08:00:00'),
                'cutoffdate' => 0,
                'gradingduedate' => 0,
                'assignsubmission_onlinetext_enabled' => 1,
                'completion' => COMPLETION_TRACKING_MANUAL,
            ]);
            $tareas[] = ['unidad' => $unidad, 'nombre' => $actividad, 'cmid' => $modulo->cmid, 'vencida' => strtotime("$fecha 23:59:00") < time()];
        }
    }

    // TA-01.4 · Matrículas.
    $generador->enrol_user($docentes[$docente]->id, $curso->id, 'editingteacher');
    foreach ($estudiantes as $estudiante) {
        $generador->enrol_user($estudiante->id, $curso->id, 'student');
    }

    // TA-01.5 · Entregas y notas. Semilla fija: cada carga produce los mismos datos.
    mt_srand(2026 + $indice);
    $contador = ['CALIFICADO' => 0, 'PENDIENTE' => 0, 'SIN_ENTREGA' => 0, 'NOTA_CERO' => 0];
    $completion = new completion_info($curso);
    foreach ($tareas as $tarea) {
        $cm = get_coursemodule_from_id('assign', $tarea['cmid'], $curso->id, false, MUST_EXIST);
        $assign = new assign(context_module::instance($cm->id), $cm, $curso);
        foreach ($estudiantes as $n => $estudiante) {
            $caso = caso_estudiante($n, $tarea, $indice);
            if ($caso === 'SIN_ENTREGA') {
                $contador['SIN_ENTREGA']++;
                continue;
            }

            $entrega = $assign->get_user_submission($estudiante->id, true);
            $entrega->status = ASSIGN_SUBMISSION_STATUS_SUBMITTED;
            $entrega->timemodified = time();
            $DB->update_record('assign_submission', $entrega);
            $completion->internal_set_data($cm, (object) [
                'id' => 0,
                'coursemoduleid' => $cm->id,
                'userid' => $estudiante->id,
                'completionstate' => COMPLETION_COMPLETE,
                'viewed' => 0,
                'overrideby' => null,
                'timemodified' => time(),
            ]);

            if ($caso === 'PENDIENTE') {
                $contador['PENDIENTE']++;
                continue;
            }

            $nota = $caso;
            $calificacion = $assign->get_user_grade($estudiante->id, true);
            $calificacion->grade = $nota;
            $calificacion->grader = $docentes[$docente]->id;
            $assign->update_grade($calificacion);
            $contador['CALIFICADO']++;
            if ($nota == 0) {
                $contador['NOTA_CERO']++;
            }
        }
    }
    grade_regrade_final_grades($curso->id);

    echo sprintf('%s · id %d · %d actividades · %d estudiantes · calificadas %d (nota 0: %d) · pendientes %d · sin entrega %d',
        $corto, $curso->id, count($tareas), count($estudiantes),
        $contador['CALIFICADO'], $contador['NOTA_CERO'], $contador['PENDIENTE'], $contador['SIN_ENTREGA']) . PHP_EOL;
}

echo 'Categorías UTA › FISEI › Software, docentes doc01 y doc02, estudiantes est01 a est' . ESTUDIANTES . '.' . PHP_EOL;

/**
 * gradereport_user_get_grade_items devuelve itemname null en los ítems de categoría, y ninguna otra función permitida
 * da el nombre de la categoría. Por eso el idnumber del ítem de cada categoría es su nombre (Exámenes, Tareas, APE).
 */
function nombrar_categorias(int $cursoid): void {
    foreach (grade_category::fetch_all(['courseid' => $cursoid]) ?: [] as $categoria) {
        if ($categoria->is_course_category()) {
            continue;
        }
        $item = $categoria->load_grade_item();
        if ($item->idnumber !== $categoria->fullname) {
            $item->idnumber = $categoria->fullname;
            $item->update();
        }
    }
}

function crear_usuario(testing_data_generator $generador, string $usuario, string $nombre, string $apellido): stdClass {
    return $generador->create_user([
        'username' => $usuario,
        'firstname' => $nombre,
        'lastname' => $apellido,
        'email' => "$usuario@edumonitor.test",
        'password' => bin2hex(random_bytes(16)) . 'Aa1!',
    ]);
}

/**
 * Qué tiene el estudiante n en una tarea: una nota (float), 'PENDIENTE' (entregó y falta calificar) o 'SIN_ENTREGA'.
 * Los casos fijos están en AGIL-4B (índice 0), que es el curso de la demostración; BDD-4B solo lleva variación aleatoria.
 */
function caso_estudiante(int $n, array $tarea, int $curso): float|string {
    $unidad = $tarea['unidad'];

    // Unidad 4 sin vencer: casi nadie entregó; algunos entregaron la Tarea 7 y esperan calificación.
    if (!$tarea['vencida']) {
        return ($tarea['nombre'] === 'Tarea 7' && $n % 4 === 0) ? 'PENDIENTE' : 'SIN_ENTREGA';
    }

    $base = 6.5 + ($n % 7) * 0.45;
    $nota = round(min(10, max(0, $base + (mt_rand(-10, 10) / 10))), 2);

    if ($curso === 0) {
        switch ($n) {
            case 3:  // R1 · descenso reciente: alto en las unidades 1 y 2, bajo en la 3.
                return $unidad <= 2 ? round(8.8 + mt_rand(0, 10) / 10, 2) : round(3.5 + mt_rand(0, 10) / 10, 2);
            case 5:  // Nota cero calificada (distinta de sin entrega).
                return $tarea['nombre'] === 'Tarea 3' ? 0.0 : $nota;
            case 7:  // R2 · atrasos: dos o más actividades vencidas sin entrega.
                return in_array($tarea['nombre'], ['Tarea 5', 'Tarea 6', 'APE 3'], true) ? 'SIN_ENTREGA' : $nota;
            case 9:  // Entregó y está pendiente de calificar.
                return in_array($tarea['nombre'], ['Tarea 6', 'Taller 2'], true) ? 'PENDIENTE' : $nota;
            case 11: // R4 · promedio bajo (< 7,0).
                return round(4.8 + mt_rand(0, 10) / 10, 2);
            case 13: // R6 · alejado del grupo (z ≤ −1).
                return round(2.8 + mt_rand(0, 10) / 10, 2);
            case 15: // R3 · baja finalización: no entregó la mayoría de la unidad 2 y 3.
                return in_array($unidad, [2, 3], true) && $tarea['nombre'] !== 'Examen Parcial 1' ? 'SIN_ENTREGA' : $nota;
            case 17: // R5 · temática crítica: bajo en todas las actividades de la unidad 3.
                return $unidad === 3 ? round(4.0 + mt_rand(0, 15) / 10, 2) : $nota;
        }
    }

    // Variación del resto: alguna entrega faltante y el último taller con calificaciones pendientes.
    if (mt_rand(1, 100) <= 3) {
        return 'SIN_ENTREGA';
    }
    if ($tarea['nombre'] === 'Taller 2' && mt_rand(1, 100) <= 15) {
        return 'PENDIENTE';
    }
    return $nota;
}
