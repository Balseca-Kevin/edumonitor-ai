<?php
// TA-02.1 · Configura los servicios web de Moodle para EduMonitor. Idempotente: se puede ejecutar varias veces.
//
// Crea o actualiza:
//   - el rol de sistema «edumonitor_lectura» con solo las capacidades de lectura que piden las 8 funciones permitidas,
//   - el usuario técnico «edumonitor_ws_user» con ese rol,
//   - el servicio externo «edumonitor_ws» con exactamente las 8 funciones permitidas y usuarios restringidos,
//   - un token permanente para el usuario técnico (solo si no existe; el token se imprime una única vez).
//
// Antes, habilitar servicios web y REST con la CLI estándar:
//   php admin/cli/cfg.php --name=enablewebservices --set=1
//   php admin/cli/cfg.php --name=webserviceprotocols --set=rest
//
// Uso (desde la raíz del repo):
//   docker cp moodle-lab/scripts/configurar_servicios_web.php edumonitor-moodle-1:/tmp/
//   docker exec edumonitor-moodle-1 php /tmp/configurar_servicios_web.php

define('CLI_SCRIPT', true);
require('/var/www/html/config.php');
require_once($CFG->dirroot . '/user/lib.php');
require_once($CFG->dirroot . '/webservice/lib.php');

const ROL = 'edumonitor_lectura';
const USUARIO = 'edumonitor_ws_user';
const SERVICIO = 'edumonitor_ws';

// Las únicas funciones que EduMonitor puede llamar (docs/CONTEXTO_IA.md).
const FUNCIONES = [
    'core_webservice_get_site_info',
    'core_enrol_get_users_courses',
    'core_course_get_contents',
    'core_enrol_get_enrolled_users',
    'gradereport_user_get_grade_items',
    'mod_assign_get_assignments',
    'mod_assign_get_submissions',
    'core_completion_get_activities_completion_status',
];

// Capacidades de lectura que necesitan esas funciones. Ninguna permite escribir ni calificar.
const CAPACIDADES = [
    'webservice/rest:use',                  // usar el protocolo REST
    'moodle/course:view',                   // ver cursos sin estar matriculado (core_course_get_contents, mod_assign_get_assignments)
    'moodle/course:viewparticipants',       // core_enrol_get_enrolled_users
    'moodle/user:viewdetails',              // datos básicos de matriculados y core_enrol_get_users_courses
    'moodle/site:accessallgroups',          // ver matriculados de todos los grupos
    'gradereport/user:view',                // gradereport_user_get_grade_items
    'moodle/grade:viewall',                 // notas de todos los estudiantes
    'mod/assign:view',                      // mod_assign_get_assignments
    'mod/assign:viewgrades',                // mod_assign_get_submissions (lectura de entregas y estados)
    'report/progress:view',                 // core_completion_get_activities_completion_status de otros usuarios
];

$sistema = context_system::instance();
$resumen = [];

// 1. Rol de solo lectura asignable en el contexto de sistema.
$rolid = $DB->get_field('role', 'id', ['shortname' => ROL]);
if (!$rolid) {
    $rolid = create_role('EduMonitor lectura', ROL, 'Solo lectura para el servicio web de EduMonitor (TA-02.1).');
    $resumen[] = 'rol creado';
}
set_role_contextlevels($rolid, [CONTEXT_SYSTEM]);
foreach (CAPACIDADES as $capacidad) {
    assign_capability($capacidad, CAP_ALLOW, $rolid, $sistema->id, true);
}
$resumen[] = 'rol con ' . count(CAPACIDADES) . ' capacidades';

// 2. Usuario técnico (contraseña aleatoria que nadie usa: entra solo con token).
$usuario = $DB->get_record('user', ['username' => USUARIO, 'mnethostid' => $CFG->mnet_localhost_id]);
if (!$usuario) {
    $id = user_create_user((object) [
        'username' => USUARIO,
        'auth' => 'manual',
        'confirmed' => 1,
        'mnethostid' => $CFG->mnet_localhost_id,
        'firstname' => 'EduMonitor',
        'lastname' => 'Servicio web',
        'email' => USUARIO . '@edumonitor.test',
        'password' => bin2hex(random_bytes(24)) . 'Aa1!',
    ]);
    $usuario = $DB->get_record('user', ['id' => $id], '*', MUST_EXIST);
    $resumen[] = 'usuario creado';
}
role_assign($rolid, $usuario->id, $sistema->id);
$resumen[] = 'rol asignado al usuario';

// 3. Servicio externo con exactamente las 8 funciones y acceso solo para usuarios autorizados.
$ws = new webservice();
$servicio = $DB->get_record('external_services', ['shortname' => SERVICIO]);
if (!$servicio) {
    $servicioid = $ws->add_external_service((object) [
        'name' => 'EduMonitor',
        'shortname' => SERVICIO,
        'enabled' => 1,
        'restrictedusers' => 1,
        'downloadfiles' => 0,
        'uploadfiles' => 0,
        'requiredcapability' => '',
    ]);
    $servicio = $DB->get_record('external_services', ['id' => $servicioid], '*', MUST_EXIST);
    $resumen[] = 'servicio creado';
}
$actuales = $DB->get_fieldset_select('external_services_functions', 'functionname', 'externalserviceid = ?', [$servicio->id]);
foreach (array_diff(FUNCIONES, $actuales) as $funcion) {
    $ws->add_external_function_to_service($funcion, $servicio->id);
}
foreach (array_diff($actuales, FUNCIONES) as $sobrante) {
    $ws->remove_external_function_from_service($sobrante, $servicio->id);
}
$resumen[] = 'servicio con ' . count(FUNCIONES) . ' funciones';

if (!$DB->record_exists('external_services_users', ['externalserviceid' => $servicio->id, 'userid' => $usuario->id])) {
    $ws->add_ws_authorised_user((object) ['externalserviceid' => $servicio->id, 'userid' => $usuario->id]);
    $resumen[] = 'usuario autorizado en el servicio';
}

// 4. Token permanente. Si ya existe no se regenera ni se vuelve a imprimir completo.
$token = $DB->get_record('external_tokens', ['userid' => $usuario->id, 'externalserviceid' => $servicio->id, 'tokentype' => 0]);
echo implode(PHP_EOL, $resumen) . PHP_EOL;
if ($token) {
    echo 'Token existente: ••••' . substr($token->token, -4) . ' (no se regenera)' . PHP_EOL;
} else {
    $nuevo = \core_external\util::generate_token(0, $servicio, $usuario->id, $sistema);
    echo 'TOKEN_NUEVO=' . $nuevo . PHP_EOL;
}
