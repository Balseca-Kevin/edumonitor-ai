<?php
// Bloque 0 del Sprint 3 · Devuelve los casos de la demo que alguien sobrescribió a mano en el libro de calificaciones de Moodle.
// Idempotente: si el caso ya está bien, no hace nada.
//
// Caso conocido: est05 (Elena Espinoza) tiene 0 en Tarea 3 de AGIL-4B (nota 0 calificada, distinta de sin entrega), pero en el libro
// se sobrescribió a mano a 5,5. La tarea conserva la nota 0; el script quita solo la sobrescritura del libro y vuelve a publicar esa
// nota, así que el libro muestra otra vez la nota de la tarea. No cambia la nota de la tarea ni ninguna otra.
//
// Uso (desde la raíz del repo):
//   docker cp moodle-lab/scripts/restaurar_casos_demo.php aula-virtual-moodle-1:/tmp/
//   docker exec aula-virtual-moodle-1 php /tmp/restaurar_casos_demo.php

define('CLI_SCRIPT', true);
require('/var/www/html/config.php');
require_once($CFG->libdir . '/gradelib.php');
require_once($CFG->dirroot . '/mod/assign/locallib.php');

\core\session\manager::set_user(get_admin());
$CFG->noemailever = true;

// [curso, actividad, estudiante]: la nota de la tarea manda sobre cualquier sobrescritura del libro.
const CASOS = [
    ['AGIL-4B', 'Tarea 3', 'est05'],
];

foreach (CASOS as [$corto, $actividad, $usuario]) {
    $curso = $DB->get_record('course', ['shortname' => $corto], '*', MUST_EXIST);
    $registro = $DB->get_record('assign', ['course' => $curso->id, 'name' => $actividad], '*', MUST_EXIST);
    $estudiante = $DB->get_record('user', ['username' => $usuario], '*', MUST_EXIST);
    $cm = get_coursemodule_from_instance('assign', $registro->id, $curso->id, false, MUST_EXIST);
    $assign = new assign(context_module::instance($cm->id), $cm, $curso);

    $item = grade_item::fetch(['courseid' => $curso->id, 'itemtype' => 'mod', 'itemmodule' => 'assign', 'iteminstance' => $registro->id]);
    $enLibro = grade_grade::fetch(['itemid' => $item->id, 'userid' => $estudiante->id]);
    $enTarea = $assign->get_user_grade($estudiante->id, false);
    if (!$enLibro || !$enTarea || $enTarea->grade === null || (float) $enTarea->grade < 0) {
        echo "$corto · $actividad · $usuario: sin nota en la tarea; no se toca" . PHP_EOL;
        continue;
    }

    $antes = $enLibro->finalgrade;
    if (!$enLibro->is_overridden() && (float) $antes === (float) $enTarea->grade) {
        echo sprintf('%s · %s · %s: ya está bien (%.2f)', $corto, $actividad, $usuario, $antes) . PHP_EOL;
        continue;
    }

    $enLibro->set_overridden(false, false);
    // Vuelve a publicar en el libro la nota que ya tiene la tarea (la misma; no se cambia).
    $assign->update_grade($enTarea);
    grade_regrade_final_grades($curso->id);

    $despues = grade_grade::fetch(['itemid' => $item->id, 'userid' => $estudiante->id]);
    echo sprintf('%s · %s · %s: el libro mostraba %.2f (sobrescrita); ahora %.2f, la nota de la tarea',
        $corto, $actividad, $usuario, $antes, $despues->finalgrade) . PHP_EOL;
}
