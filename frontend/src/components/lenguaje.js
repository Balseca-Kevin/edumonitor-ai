import { formatearFecha, formatearNumero } from "./formato";

// Lenguaje docente del diseño D: ningún término estadístico en pantalla (z, tendencia, desviación).

const NO_DISPONIBLE = "no disponible";

/** «1,5 bajo el promedio», «0,4 sobre el promedio» o «en el promedio»: la diferencia con el promedio del curso, sin z. */
export function frenteAlCurso(promedio, promedioCurso) {
  if (
    promedio === null ||
    promedio === undefined ||
    promedioCurso === null ||
    promedioCurso === undefined
  )
    return NO_DISPONIBLE;
  const diferencia = Math.round((promedio - promedioCurso) * 10) / 10;
  if (diferencia === 0) return "en el promedio";
  return `${formatearNumero(Math.abs(diferencia), 1)} ${diferencia < 0 ? "bajo" : "sobre"} el promedio`;
}

/** «↓ bajando 1,8», «↑ subiendo 0,2» o «→ se mantiene»: últimas 3 notas frente a las anteriores. Null con menos de 4 notas. */
export function ultimasNotas(tendencia) {
  if (tendencia === null || tendencia === undefined) return "aún sin datos";
  const valor = Math.round(tendencia * 10) / 10;
  if (valor === 0) return "→ se mantiene";
  return valor < 0
    ? `↓ bajando ${formatearNumero(-valor, 1)}`
    : `↑ subiendo ${formatearNumero(valor, 1)}`;
}

/** Qué vigila cada regla, en palabras del docente. El código (R1, R2, R4) es interno y no se muestra. */
export const DESCRIPCION_REGLA = {
  R1: "Avisa cuando las últimas 3 notas bajan al menos estos puntos frente a las anteriores.",
  R2: "Avisa cuando hay al menos estas actividades vencidas sin entregar.",
  R4: "Avisa cuando el promedio queda bajo este valor, con al menos 4 notas registradas.",
};

/** Evidencia de una alerta en una línea, con sus números (docs/contratos-api.md), según la regla que la activó. */
export function textoEvidencia(alerta) {
  const e = alerta.evidencia ?? {};
  const numero = (valor, decimales = 1) =>
    formatearNumero(valor, decimales) ?? "no disponible";

  switch (alerta.regla) {
    case "R1": {
      // C7 · De la más reciente a la más antigua, frente al promedio de las anteriores, como lo dice el asistente.
      const ultimas = [...(e.ultimas ?? [])].reverse().map((n) => numero(n));
      const anteriores = (e.anteriores ?? []).filter((n) => typeof n === "number");
      const anterior = anteriores.length ? numero(anteriores.reduce((s, n) => s + n, 0) / anteriores.length) : "no disponible";
      const lista = ultimas.length > 1 ? `${ultimas.slice(0, -1).join("; ")} y ${ultimas.at(-1)}` : ultimas.join("");
      return `Sus últimas ${ultimas.length} notas fueron ${lista}, frente a un promedio anterior de ${anterior}: bajó ${numero(
        Math.abs(e.diferencia ?? 0),
      )} puntos; el aviso salta desde ${numero(e.umbral)}`;
    }
    case "R2": {
      const actividades = e.actividades ?? [];
      return `${actividades.length} ${actividades.length === 1 ? "actividad vencida" : "actividades vencidas"} sin entregar: ${actividades
        .map(
          (a) => `${a.nombre} (${formatearFecha(a.fechaLimite).slice(0, 5)})`,
        )
        .join(" · ")}`;
    }
    case "R4":
      return `Promedio ${numero(e.promedio, 2)} con ${e.notas ?? "no disponible"} notas registradas; el mínimo es ${numero(e.umbral)}`;
    default:
      return "—";
  }
}

const normalizar = (texto) =>
  String(texto ?? "")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();

/** Estudiantes nombrados en la respuesta («Nombre Apellido»), buscados en los indicadores reales del curso, en el orden del texto. */
export function estudiantesMencionados(texto, estudiantes) {
  const plano = normalizar(texto);
  return (estudiantes ?? [])
    .map((e) => ({
      e,
      posicion: plano.indexOf(normalizar(`${e.nombre} ${e.apellido}`)),
    }))
    .filter((m) => m.posicion >= 0)
    .sort((a, b) => a.posicion - b.posicion)
    .map((m) => m.e);
}

/** Motivo de la tarjeta en una línea, con los conteos reales del panel. */
export function motivo(e) {
  const partes = [];
  const n = e.notas ?? {};
  if (n.ceros)
    partes.push(`${n.ceros} ${n.ceros === 1 ? "nota" : "notas"} en cero`);
  if (n.sinEntrega) partes.push(`${n.sinEntrega} sin entregar`);
  if (n.pendientes) partes.push(`${n.pendientes} por calificar`);
  if (e.tendencia !== null && e.tendencia !== undefined && e.tendencia <= -0.5)
    partes.push(`últimas notas ${ultimasNotas(e.tendencia)}`);
  return partes.length
    ? partes.join(" · ")
    : "Sin ceros ni entregas pendientes";
}

/** «Datos usados» en palabras del docente: qué consultó el asistente, sin nombres de funciones. */
export function datoUsado(herramienta, estudiantes) {
  const p = herramienta.parametros ?? {};
  let texto = "Datos del curso";
  if (herramienta.nombre === "listar_cursos_autorizados")
    texto = "Tus cursos asignados";
  else if (herramienta.nombre === "identificar_alertas")
    texto = "Alertas del curso";
  else if (herramienta.nombre === "consultar_estado_periodo")
    texto = "Semana del período y lo que vence";
  else if (herramienta.nombre === "obtener_desempeno_tematico")
    texto = "Desempeño por tema";
  else if (herramienta.nombre === "obtener_plan_refuerzo")
    texto = "Plan de refuerzo con sus notas y criterios";
  else if (herramienta.nombre === "calcular_nota_necesaria") {
    const porId = (estudiantes ?? []).find(
      (e) => e.id === Number(p.estudianteId),
    );
    texto = porId
      ? `Nota necesaria de ${porId.nombre} ${porId.apellido}`
      : "Nota necesaria de un estudiante";
  } else if (herramienta.nombre === "obtener_calificaciones") {
    const porId = p.estudianteId
      ? (estudiantes ?? []).find((e) => e.id === Number(p.estudianteId))
      : null;
    if (p.nombre) texto = `Notas de ${p.nombre}`;
    else if (porId) texto = `Notas de ${porId.nombre} ${porId.apellido}`;
    else if (p.estudianteId) texto = "Notas de un estudiante";
    else texto = "Calificaciones del curso";
  }
  return `${texto} · ${herramienta.duracionMs} ms${herramienta.estado === "OK" ? "" : " · no se pudo consultar"}`;
}

/**
 * Separa la respuesta en Hechos, Inferencia y Recomendación cuando el asistente las escribe con esos títulos (agent/AGENTS.md); si no,
 * la respuesta va entera. No cambia ni resume el texto del modelo.
 */
export function separarRespuesta(texto) {
  const titulos = /^\s*(hechos|inferencias?|recomendaci[oó]n(?:es)?)\s*:\s*/gim;
  const marcas = [...String(texto ?? "").matchAll(titulos)];
  if (marcas.length < 2) return null;
  const bloques = {};
  marcas.forEach((m, i) => {
    const desde = m.index + m[0].length;
    const hasta = i + 1 < marcas.length ? marcas[i + 1].index : texto.length;
    const clave = m[1].toLowerCase().startsWith("hecho")
      ? "hechos"
      : m[1].toLowerCase().startsWith("infer")
        ? "inferencia"
        : "recomendacion";
    bloques[clave] = texto.slice(desde, hasta).trim();
  });
  const antes = texto.slice(0, marcas[0].index).trim();
  return { antes, ...bloques };
}

/** TA-22.A1 · Texto del aviso de un criterio de la rúbrica. Orientativo: dice «revisar», no un veredicto. */
export const AVISO_CRITERIO = {
  REVISAR_ENSENANZA:
    "Casi todo el curso falló este criterio: revisar cómo se enseñó",
  REVISAR_ENUNCIADO:
    "Este criterio no separa a quienes van bien del resto: revisar el enunciado",
};

/** TA-22.A2 · Pregunta fija para pedir al asistente el enunciado corregido de un criterio. */
export function preguntaEnunciadoCorregido(actividad, criterio) {
  return `Propón una versión corregida del enunciado de ${actividad} para el criterio «${criterio}», más clara para los estudiantes y sin cambiar lo que se evalúa.`;
}

/**
 * TA-15.A1 · Lo que pasó con el tema después de aprobar el refuerzo, dicho como cambio y no como causa: «subió de 5,20 a 7,10 en el
 * tema después del refuerzo». Sin notas nuevas lo dice.
 */
export function textoResultadoRefuerzo(r) {
  const antes = formatearNumero(r.antes) ?? "sin nota";
  const ahora = formatearNumero(r.ahora) ?? "sin nota";
  switch (r.estado) {
    case "SUBIO":
      return `${r.estudiante}: subió de ${antes} a ${ahora} en ${r.tema} después del refuerzo`;
    case "BAJO":
      return `${r.estudiante}: bajó de ${antes} a ${ahora} en ${r.tema} después del refuerzo`;
    case "IGUAL":
      return `${r.estudiante}: sigue en ${ahora} en ${r.tema} después del refuerzo`;
    default:
      return `${r.estudiante}: sin notas nuevas en ${r.tema} desde que se aprobó (${antes})`;
  }
}
