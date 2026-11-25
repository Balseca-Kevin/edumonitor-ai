import { useCallback, useEffect, useRef, useState } from "react";
import {
  Link,
  useOutletContext,
  useParams,
  useSearchParams,
} from "react-router-dom";
import FichaLateral from "../../components/FichaLateral";
import { formatearFecha, formatearNumero } from "../../components/formato";
import { nombreTramo, useTramo } from "../../components/tramo";
import {
  datoUsado,
  estudiantesMencionados,
  motivo,
  separarRespuesta,
} from "../../components/lenguaje";
import {
  listarConversaciones,
  obtenerConversacion,
  preguntarAlAsistente,
} from "../../services/agente";
import {
  ESTADO_PLAN,
  listarPlanes,
  nombrePlan,
  obtenerPlan,
} from "../../services/refuerzos";
import PlanBorrador from "../../components/PlanBorrador";
import { obtenerIndicadores } from "../../services/indicadores";
import { obtenerAlertas } from "../../services/alertas";
import { actividadesDelLibro, obtenerLibro } from "../../services/libro";
import { describirError } from "../../services/errores";
import { descargarInforme } from "../../services/informe";
import { guardarArchivo } from "../../components/archivo";

const LARGO_MAXIMO = 500;
const APROBACION = 7;

const SUGERENCIAS = [
  {
    texto: "¿Qué estudiantes requieren atención?",
    ayuda: "Alertas activas con su motivo",
  },
  {
    texto: "¿Quiénes tienen promedio bajo 7?",
    ayuda: "Promedio de exámenes 40 %, tareas 30 % y APE 30 %",
  },
  {
    texto: "¿Quién tiene más actividades sin entregar?",
    ayuda: "Entregas pendientes por estudiante",
  },
  { texto: "¿Qué cursos tengo asignados?", ayuda: "Tus cursos en EduMonitor" },
];

/** Nombres de los parámetros de las consultas en palabras del docente (dentro de «Cómo llegó a esta respuesta»). */
const PARAMETRO = {
  cursoId: "curso",
  estudianteId: "estudiante",
  nombre: "nombre",
  promedioMenorA: "promedio menor a",
  orden: "orden",
};

function usuarioActual() {
  try {
    return JSON.parse(sessionStorage.getItem("edumonitor_usuario") ?? "null");
  } catch {
    return null;
  }
}

/**
 * TA-23.A2 · Cada cifra verificada de la respuesta abre la celda del libro de donde sale (estudiante y actividad); una cifra que el
 * verificador no encontró en los datos se marca «no verificada». La pantalla no calcula nada: usa las cifras que devolvió el agente.
 */
export function ConCifras({ texto, cifras, cursoId }) {
  const lista = (cifras ?? []).filter((c) => c.valor);
  if (!texto || lista.length === 0) return texto ?? null;
  const porValor = new Map();
  for (const c of lista)
    if (!porValor.has(c.valor.replace("%", "")))
      porValor.set(c.valor.replace("%", ""), c);
  const escapar = (v) => v.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const valores = [...porValor.keys()]
    .sort((a, b) => b.length - a.length)
    .map(escapar);
  const patron = new RegExp(
    `(?<![\\w.,-])(${valores.join("|")})(?![\\w]|[.,]\\d)`,
    "g",
  );
  const partes = [];
  let desde = 0;
  for (const m of texto.matchAll(patron)) {
    const cifra = porValor.get(m[1]);
    partes.push(texto.slice(desde, m.index));
    if (!cifra.verificada) {
      partes.push(
        <span
          key={m.index}
          className="cifra-enlace cifra-enlace--no-verificada"
          title="Cifra no verificada: no está en los datos consultados"
        >
          {m[1]}
        </span>,
      );
    } else if (cifra.estudianteId) {
      const destino = new URLSearchParams({ estudiante: cifra.estudianteId });
      if (cifra.actividadId) destino.set("actividad", cifra.actividadId);
      partes.push(
        <Link
          key={m.index}
          className="cifra-enlace"
          to={`/cursos/${cursoId}/libro?${destino}`}
          title="Ver en el libro de calificaciones"
        >
          {m[1]}
        </Link>,
      );
    } else {
      partes.push(
        <span
          key={m.index}
          className="cifra-enlace cifra-enlace--verificada"
          title="Cifra verificada en los datos consultados"
        >
          {m[1]}
        </span>,
      );
    }
    desde = m.index + m[0].length;
  }
  partes.push(texto.slice(desde));
  return partes;
}

function Bloques({ texto, cifras, cursoId }) {
  const partes = separarRespuesta(texto);
  const conCifras = (t) => (
    <ConCifras texto={t} cifras={cifras} cursoId={cursoId} />
  );
  if (!partes) return <p className="msg-texto">{conCifras(texto)}</p>;
  return (
    <>
      {partes.antes && <p className="msg-texto">{conCifras(partes.antes)}</p>}
      <div className="respuesta-bloques num">
        {partes.hechos && (
          <div className="respuesta-bloque">
            <span>Hechos</span>
            <p>{conCifras(partes.hechos)}</p>
          </div>
        )}
        {partes.inferencia && (
          <div className="respuesta-bloque">
            <span>Inferencia</span>
            <p>{conCifras(partes.inferencia)}</p>
          </div>
        )}
        {partes.recomendacion && (
          <div className="respuesta-bloque respuesta-bloque--ia">
            <span>Recomendación</span>
            <p>{conCifras(partes.recomendacion)}</p>
          </div>
        )}
      </div>
    </>
  );
}

/** HU-23 · Quién respondió, en palabras del docente. */
export function quienRespondio(respuesta) {
  const modelo = respuesta.modelo;
  const proveedor =
    respuesta.proveedor ??
    (!modelo || modelo === "ninguno" ? "NINGUNO" : "LOCAL");
  if (proveedor === "NINGUNO")
    return "Respondió: EduMonitor, sin llamar al modelo";
  if (proveedor === "EXTERNO") return `Respondió: modelo externo · ${modelo}`;
  return respuesta.respaldo
    ? `Respondió: modelo local (respaldo) · ${modelo}`
    : `Respondió: modelo local · ${modelo}`;
}

function Estudiantes({ estudiantes, onAbrir }) {
  if (!estudiantes.length) return null;
  return (
    <div
      className="respuesta-estudiantes hoja"
      aria-label="Estudiantes mencionados"
    >
      <div className="respuesta-estudiantes-titulo">
        Estudiantes mencionados
      </div>
      {estudiantes.map((e) => {
        const bajo =
          e.promedioPonderado !== null && e.promedioPonderado < APROBACION;
        return (
          <div className="respuesta-estudiante num" key={e.id}>
            <b>
              {e.apellido}, {e.nombre}
            </b>
            <span className={`fuerte${bajo ? " arena-texto" : ""}`}>
              {formatearNumero(e.promedioPonderado) ?? "no disponible"}
            </span>
            <span className="ayuda">{motivo(e)}</span>
            <button
              type="button"
              className="boton boton--secundario boton--chico"
              onClick={() => onAbrir(e.id)}
            >
              Abrir ficha
            </button>
          </div>
        );
      })}
    </div>
  );
}

/** HU-17 · Desplegable cerrado: quién respondió, qué se consultó con qué parámetros, en cuánto tiempo y de qué sincronización. */
function ComoLlego({ respuesta, estudiantes, sincronizacion }) {
  const quien = quienRespondio(respuesta);
  const cifras = respuesta.cifras ?? [];
  const sinVerificar = cifras.filter((c) => !c.verificada);
  return (
    <details className="como-llego">
      <summary>Cómo llegó a esta respuesta</summary>
      <ul>
        <li>{quien}</li>
        {(respuesta.herramientas ?? []).map((h, i) => {
          const parametros = Object.entries(h.parametros ?? {})
            .map(([clave, valor]) => `${PARAMETRO[clave] ?? clave} ${valor}`)
            .join(" · ");
          return (
            <li key={`${h.nombre}-${i}`}>
              {datoUsado(h, estudiantes)}
              {parametros && ` · ${parametros}`}
            </li>
          );
        })}
        {!respuesta.herramientas?.length && (
          <li>Sin consultar datos del curso.</li>
        )}
        {cifras.length > 0 && (
          <li>
            Cifras verificadas en los datos:{" "}
            {cifras.length - sinVerificar.length} de {cifras.length}
            {sinVerificar.length > 0 &&
              ` · no verificadas: ${sinVerificar.map((c) => c.valor).join(", ")}`}
          </li>
        )}
        {respuesta.duracionMs != null && (
          <li>
            Tiempo total: {formatearNumero(respuesta.duracionMs / 1000, 1)} s
          </li>
        )}
        {sincronizacion?.fecha && (
          <li>
            Datos de la sincronización del{" "}
            {formatearFecha(sincronizacion.fecha)}
          </li>
        )}
      </ul>
    </details>
  );
}

/** TA-22.A2 · Copia el texto de la respuesta para pegarlo donde el docente quiera (por ejemplo, un enunciado corregido). */
function CopiarRespuesta({ texto }) {
  const [copiado, setCopiado] = useState(null);
  async function copiar() {
    try {
      await navigator.clipboard.writeText(texto);
      setCopiado("Copiado");
    } catch {
      setCopiado("No se pudo copiar; selecciona el texto a mano.");
    }
  }
  return (
    <>
      <button
        type="button"
        className="boton boton--secundario boton--chico"
        onClick={copiar}
      >
        Copiar respuesta
      </button>
      {copiado && (
        <span className="ayuda" role="status">
          {copiado}
        </span>
      )}
    </>
  );
}

/** HU-16 · Descarga la respuesta como informe del curso en PDF, con los indicadores del panel. */
function DescargarInforme({ cursoId, conversacionId }) {
  const [estado, setEstado] = useState({ ocupado: false, error: null });
  async function descargar() {
    setEstado({ ocupado: true, error: null });
    try {
      guardarArchivo(await descargarInforme(cursoId, conversacionId));
      setEstado({ ocupado: false, error: null });
    } catch (e) {
      setEstado({
        ocupado: false,
        error: describirError(e).detalle ?? "No se pudo descargar el informe.",
      });
    }
  }
  return (
    <div className="msg-informe">
      <button
        type="button"
        className="boton boton--secundario boton--chico"
        onClick={descargar}
        disabled={estado.ocupado}
      >
        {estado.ocupado
          ? "Preparando…"
          : "Descargar como informe del curso (PDF)"}
      </button>
      {estado.error && (
        <span className="ayuda mapa-error" role="alert">
          {estado.error}
        </span>
      )}
    </div>
  );
}

function Turno({ turno, estudiantes, sincronizacion, onAbrir, cursoId }) {
  return (
    <div className="msg">
      <div className="msg-docente">{turno.pregunta}</div>
      <div className="msg-asistente">
        <div className="msg-icono" aria-hidden="true">
          <span />
          <span />
          <span />
          <span />
        </div>
        <div className="msg-cuerpo">
          {turno.cargando && (
            <div className="pensando" role="status">
              <span className="pensando-texto">Consultando…</span>
              <span className="pensando-puntos" aria-hidden="true">
                <span />
                <span />
                <span />
              </span>
            </div>
          )}
          {turno.respuesta && (
            <>
              <Bloques
                texto={turno.respuesta.respuesta}
                cifras={turno.respuesta.cifras}
                cursoId={cursoId}
              />
              <span className="respondio ayuda">
                {quienRespondio(turno.respuesta)}
              </span>
              <Estudiantes
                estudiantes={estudiantesMencionados(
                  turno.respuesta.respuesta,
                  estudiantes,
                )}
                onAbrir={onAbrir}
              />
              <ComoLlego
                respuesta={turno.respuesta}
                estudiantes={estudiantes}
                sincronizacion={sincronizacion}
              />
              <div className="msg-informe">
                <CopiarRespuesta texto={turno.respuesta.respuesta} />
              </div>
              {turno.respuesta.conversacionId && (
                <DescargarInforme
                  cursoId={cursoId}
                  conversacionId={turno.respuesta.conversacionId}
                />
              )}
            </>
          )}
          {turno.error && (
            <div
              className={`aviso aviso--${turno.error.tipo === "no-disponible" ? "neutro" : "error"}`}
              role="alert"
            >
              <strong>{turno.error.titulo}</strong>
              {turno.error.detalle ?? "Intenta de nuevo en unos minutos."}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Historial del asistente: «Planes de refuerzo» con su estado (TA-15.A4, GET /api/cursos/{id}/refuerzos) y «Conversaciones»
 * guardadas en la base (GET /api/agente/conversaciones), de la más reciente a la más antigua, con su fecha.
 */
function Historial({
  conversaciones,
  planes,
  planActivo,
  abrirPlan,
  activa,
  abrir,
  nueva,
  consultando,
}) {
  return (
    <aside className="asistente-historial" aria-label="Historial del asistente">
      <button
        type="button"
        className="boton boton--ia"
        onClick={nueva}
        disabled={consultando}
      >
        Nueva conversación
      </button>
      {planes.length > 0 && (
        <nav aria-label="Planes de refuerzo">
          <span className="asistente-grupo mono">Planes de refuerzo</span>
          {planes.map((p) => (
            <button
              key={p.id}
              type="button"
              className="asistente-item"
              aria-current={planActivo === p.id ? "true" : undefined}
              onClick={() => abrirPlan(p.id)}
              disabled={consultando}
            >
              <span>{nombrePlan(p)}</span>
              <span
                className={`chip-estado chip-estado--${p.estado.toLowerCase()}`}
              >
                {ESTADO_PLAN[p.estado] ?? p.estado}
              </span>
            </button>
          ))}
        </nav>
      )}
      <nav aria-label="Conversaciones">
        <span className="asistente-grupo mono">Conversaciones</span>
        {conversaciones.length === 0 ? (
          <p className="ayuda">Todavía no hay preguntas en este curso.</p>
        ) : (
          conversaciones.map((c) => (
            <button
              key={c.id}
              type="button"
              className="asistente-item"
              aria-current={activa === c.id ? "true" : undefined}
              title={c.pregunta}
              onClick={() => abrir(c.id)}
            >
              <span>{c.pregunta}</span>
              {c.fecha && (
                <span className="ayuda">
                  {formatearFecha(c.fecha)}
                  {c.turnos > 1 ? ` · ${c.turnos} preguntas` : ""}
                </span>
              )}
            </button>
          ))
        )}
      </nav>
    </aside>
  );
}

/**
 * HU-14, HU-19 y R.6 · Asistente dentro del curso (docs/sprint-3/diseno.md): a la izquierda «Nueva conversación» y el historial; a
 * la derecha el chat con el contexto visible («Sabe: …» con el tramo y el estudiante elegidos), respuestas con Hechos, Inferencia y
 * Recomendación, tarjetas de los estudiantes nombrados con «Abrir ficha», y el desplegable cerrado «Cómo llegó a esta respuesta».
 * Desde una ficha llega con `?estudiante=` y la pregunta ya escrita. Un 503 se ve como «El asistente no está disponible».
 */
export default function Asistente() {
  const { id } = useParams();
  const tramo = useTramo();
  const [parametros, setParametros] = useSearchParams();
  const { curso, periodo } = useOutletContext() ?? {};
  const usuario = usuarioActual();
  const [mensaje, setMensaje] = useState("");
  const [turnos, setTurnos] = useState([]);
  const [activa, setActiva] = useState(null);
  const [historial, setHistorial] = useState([]);
  const [indicadores, setIndicadores] = useState(null);
  const [conAlertas, setConAlertas] = useState(null);
  const [ficha, setFicha] = useState({ id: null, libro: null });
  const [planes, setPlanes] = useState([]);
  const [plan, setPlan] = useState({ id: null, datos: null });
  const redactados = useRef(new Set());
  const refuerzoId = Number(parametros.get("refuerzo")) || null;
  const cuerpo = useRef(null);
  const consultando = turnos.some((t) => t.cargando);
  const estudiantes = indicadores?.estudiantes ?? [];
  const enfocado =
    estudiantes.find((e) => e.id === Number(parametros.get("estudiante"))) ??
    null;
  // HU-23 · Foco de la actividad abierta (desde el panel de actividad); su nombre sale del período del curso.
  const actividadFoco = Number(parametros.get("actividad")) || null;
  const actividadEnfocada =
    (periodo?.actividades ?? []).find((a) => a.id === actividadFoco) ?? null;

  useEffect(() => {
    let vigente = true;
    listarConversaciones(id)
      .then((lista) => vigente && setHistorial(lista))
      .catch(() => vigente && setHistorial([]));
    listarPlanes(id)
      .then((lista) => vigente && setPlanes(lista))
      .catch(() => vigente && setPlanes([]));
    const pedido = Number(parametros.get("estudiante"));
    obtenerIndicadores(id, tramo)
      .then((datos) => {
        if (!vigente) return;
        setIndicadores(datos);
        // Desde la ficha: la pregunta sobre ese estudiante queda escrita para que el docente la revise y la envíe.
        const escrita = parametros.get("pregunta");
        if (escrita)
          setMensaje((actual) => actual || escrita.slice(0, LARGO_MAXIMO));
        const elegido = datos?.estudiantes?.find((e) => e.id === pedido);
        if (elegido)
          setMensaje(
            (actual) =>
              actual ||
              `¿Cómo va ${elegido.nombre} ${elegido.apellido}? ¿En qué actividades le fue mal?`,
          );
      })
      .catch(() => vigente && setIndicadores(null));
    obtenerAlertas(id, "ACTIVA", tramo)
      .then(
        (alertas) =>
          vigente &&
          setConAlertas(new Set(alertas.map((a) => a.estudiante?.id))),
      )
      .catch(() => vigente && setConAlertas(null));
    return () => {
      vigente = false;
    };
  }, [id, tramo, parametros]);

  useEffect(() => {
    // Solo la columna de mensajes se desplaza: baja sola al último mensaje sin mover la página.
    const caja = cuerpo.current;
    if (caja && typeof caja.scrollTo === "function")
      caja.scrollTo({ top: caja.scrollHeight, behavior: "smooth" });
  }, [turnos]);

  // TA-15.A4 · Con ?refuerzo= el plan es el foco: se carga y, si todavía no tiene borrador, el asistente lo redacta una vez.
  useEffect(() => {
    if (!refuerzoId) return;
    let vigente = true;
    obtenerPlan(id, refuerzoId)
      .then((datos) => vigente && setPlan({ id: refuerzoId, datos }))
      .catch(() => vigente && setPlan({ id: refuerzoId, datos: null }));
    return () => {
      vigente = false;
    };
  }, [id, refuerzoId]);

  const planActual = plan.id === refuerzoId ? plan.datos : null;

  async function redactar(planId) {
    if (consultando) return;
    redactados.current.add(planId);
    const pregunta = "Redacta el borrador del plan de refuerzo.";
    setTurnos([{ pregunta, cargando: true }]);
    try {
      const respuesta = await preguntarAlAsistente(id, "", {
        refuerzoId: planId,
      });
      setTurnos([{ pregunta, respuesta }]);
      setActiva(respuesta.conversacionId);
      setHistorial((h) => [
        { id: respuesta.conversacionId, fecha: respuesta.fecha, pregunta },
        ...h.filter((c) => c.id !== respuesta.conversacionId),
      ]);
      const actualizado = await obtenerPlan(id, planId);
      setPlan({ id: planId, datos: actualizado });
      setPlanes((lista) =>
        lista.map((p) => (p.id === planId ? actualizado : p)),
      );
    } catch (error) {
      setTurnos([{ pregunta, error: describirError(error) }]);
    }
  }

  useEffect(() => {
    if (
      planActual &&
      planActual.estado === "ENVIADO" &&
      !planActual.texto &&
      !redactados.current.has(planActual.id)
    ) {
      redactar(planActual.id);
    }
    // Solo cuando llega un plan nuevo sin borrador.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [planActual]);

  function abrirPlan(planId) {
    const siguientes = new URLSearchParams(parametros);
    siguientes.set("refuerzo", String(planId));
    siguientes.delete("estudiante");
    setParametros(siguientes);
    setTurnos([]);
    setActiva(null);
  }

  function cambioPlan(actualizado) {
    setPlan({ id: actualizado.id, datos: actualizado });
    setPlanes((lista) =>
      lista.map((p) => (p.id === actualizado.id ? actualizado : p)),
    );
  }

  async function enviar(texto) {
    const pregunta = texto.trim();
    if (!pregunta || consultando) return;
    setMensaje("");
    setTurnos((t) => [...t, { pregunta, cargando: true }]);
    try {
      // HU-23 · Con una conversación abierta, la pregunta sigue su hilo y el asistente recuerda los últimos 4 turnos.
      const foco = { estudianteId: enfocado?.id, actividadId: actividadFoco };
      const respuesta = await preguntarAlAsistente(id, pregunta, foco, activa);
      setTurnos((t) => [...t.slice(0, -1), { pregunta, respuesta }]);
      const hilo = respuesta.hiloId ?? respuesta.conversacionId;
      setActiva(hilo);
      setHistorial((h) => {
        const existente = h.find((c) => c.id === hilo);
        const guardada = existente
          ? { ...existente, turnos: (existente.turnos ?? 1) + 1 }
          : { id: hilo, fecha: respuesta.fecha, pregunta, turnos: 1 };
        return [guardada, ...h.filter((c) => c.id !== hilo)];
      });
    } catch (error) {
      setTurnos((t) => [
        ...t.slice(0, -1),
        { pregunta, error: describirError(error) },
      ]);
    }
  }

  async function abrir(conversacionId) {
    if (consultando) return;
    setActiva(conversacionId);
    try {
      const c = await obtenerConversacion(conversacionId);
      const turnosDelHilo = c.turnos?.length ? c.turnos : [c];
      setActiva(c.hiloId ?? c.id);
      setTurnos(
        turnosDelHilo.map((t) => ({
          pregunta: t.pregunta,
          respuesta: {
            conversacionId: t.id,
            respuesta: t.respuesta,
            herramientas: t.herramientas,
            modelo: t.modelo,
            duracionMs: t.duracionMs,
            respaldo: t.respaldo,
            proveedor: t.proveedor,
            cifras: t.cifras,
          },
        })),
      );
    } catch (error) {
      setTurnos([
        { pregunta: "Conversación guardada", error: describirError(error) },
      ]);
    }
  }

  function nueva() {
    if (consultando) return;
    setTurnos([]);
    setActiva(null);
    setMensaje("");
    if (refuerzoId) {
      const siguientes = new URLSearchParams(parametros);
      siguientes.delete("refuerzo");
      setParametros(siguientes);
    }
  }

  const abrirFicha = useCallback(
    (estudianteId) => {
      setFicha((f) => ({ ...f, id: estudianteId }));
      if (!ficha.libro)
        obtenerLibro(id, { tramo })
          .then((libro) => setFicha((f) => ({ ...f, libro })))
          .catch(() => {});
    },
    [ficha.libro, id, tramo],
  );
  const cerrarFicha = useCallback(
    () => setFicha((f) => ({ ...f, id: null })),
    [],
  );

  const preguntadas = new Set(turnos.map((t) => t.pregunta));
  const seguimiento = SUGERENCIAS.filter(
    (s) => !preguntadas.has(s.texto),
  ).slice(0, 3);
  const codigo = curso?.nombreCorto ?? "este curso";
  const semanaPeriodo = periodo?.semana
    ? `semana ${periodo.semana.actual} de ${periodo.semana.total}`
    : null;
  const sabe = [
    semanaPeriodo,
    nombreTramo(tramo, indicadores?.unidades),
    enfocado ? `${enfocado.nombre} ${enfocado.apellido}` : null,
    actividadEnfocada ? actividadEnfocada.nombre : null,
    activa && turnos.length > 0
      ? `${turnos.length} ${turnos.length === 1 ? "pregunta" : "preguntas"} de esta conversación`
      : null,
    planActual
      ? `${planActual.tipo === "RETROALIMENTACION" ? "retroalimentación" : "plan de refuerzo"} de ${planActual.actividad.nombre}`
      : null,
  ]
    .filter(Boolean)
    .join(" · ");
  const estudianteFicha = estudiantes.find((e) => e.id === ficha.id) ?? null;
  const columnas = actividadesDelLibro(ficha.libro);
  const filaLibro = ficha.libro?.estudiantes.find((e) => e.id === ficha.id);
  const celdas = filaLibro
    ? columnas.map((a, i) => ({
        ...filaLibro.notas[i],
        nombre: a.nombre,
        fechaLimite: a.fechaLimite,
      }))
    : [];

  return (
    <div className="asistente">
      <Historial
        conversaciones={historial}
        planes={planes}
        planActivo={refuerzoId}
        abrirPlan={abrirPlan}
        activa={activa}
        abrir={abrir}
        nueva={nueva}
        consultando={consultando}
      />

      <section
        className="asistente-chat hoja"
        aria-label="Conversación con el asistente"
      >
        <header className="asistente-cabecera">
          <span className="fuerte">
            {turnos[0]?.pregunta ?? "Nueva conversación"}
          </span>
          <span className="asistente-contexto">Sabe: {sabe}</span>
        </header>

        <div className="chat-cuerpo" ref={cuerpo}>
          <div className="chat-hilo" aria-live="polite">
            {planActual && (
              <PlanBorrador
                key={`${planActual.id}|${planActual.estado}|${planActual.texto ?? ""}`}
                cursoId={id}
                plan={planActual}
                consultando={consultando}
                onRedactar={() => redactar(planActual.id)}
                onCambio={cambioPlan}
              />
            )}
            {turnos.length === 0 && !planActual && (
              <div className="chat-bienvenida">
                <h1>
                  Hola, <b>{usuario?.nombre ?? "docente"}</b>. ¿Qué quieres
                  saber de {codigo}?
                </h1>
                <div className="chat-sugerencias">
                  {SUGERENCIAS.map((s) => (
                    <button
                      key={s.texto}
                      type="button"
                      className="chat-sugerencia"
                      onClick={() => enviar(s.texto)}
                    >
                      <strong>{s.texto}</strong>
                      <span>{s.ayuda}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
            {turnos.map((turno, i) => (
              <Turno
                key={i}
                turno={turno}
                estudiantes={estudiantes}
                sincronizacion={curso?.ultimaSincronizacion}
                onAbrir={abrirFicha}
                cursoId={id}
              />
            ))}
          </div>
        </div>

        <div className="chat-pie">
          <div>
            {turnos.length > 0 && (
              <div
                className="chat-seguimiento"
                aria-label="Preguntas de seguimiento"
              >
                {seguimiento.map((s) => (
                  <button
                    key={s.texto}
                    type="button"
                    className="chip"
                    onClick={() => enviar(s.texto)}
                    disabled={consultando}
                  >
                    {s.texto}
                  </button>
                ))}
              </div>
            )}
            <form
              className="chat-form"
              onSubmit={(e) => {
                e.preventDefault();
                enviar(mensaje);
              }}
            >
              <span className="chat-curso">{curso?.nombreCorto}</span>
              <label htmlFor="pregunta" className="sr-only">
                Tu pregunta
              </label>
              <input
                id="pregunta"
                className="chat-input"
                maxLength={LARGO_MAXIMO}
                value={mensaje}
                onChange={(e) => setMensaje(e.target.value)}
                placeholder="Pregunta sobre notas, entregas o alertas del curso"
                autoComplete="off"
              />
              <button
                type="submit"
                className="chat-enviar"
                aria-label="Enviar pregunta"
                disabled={consultando || !mensaje.trim()}
              >
                ↑
              </button>
            </form>
          </div>
        </div>
      </section>

      {estudianteFicha && (
        <FichaLateral
          cursoId={id}
          estudiante={estudianteFicha}
          requiereAtencion={
            conAlertas ? conAlertas.has(estudianteFicha.id) : null
          }
          celdas={celdas}
          tramo={tramo}
          nombreTramo={nombreTramo(tramo, indicadores?.unidades)}
          onCerrar={cerrarFicha}
        />
      )}
    </div>
  );
}
