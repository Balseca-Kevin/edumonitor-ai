import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  obtenerCursos,
  sincronizarCurso,
  obtenerHistorialSincronizaciones,
} from "../services/cursos";
import { cerrarSesion } from "../services/auth";

function formatearFecha(fecha) {
  if (!fecha) return "No disponible";

  const fechaValida = new Date(fecha);

  if (Number.isNaN(fechaValida.getTime())) {
    return fecha;
  }

  return fechaValida.toLocaleString("es-EC", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

function etiquetaEstado(estado) {
  if (!estado) return "PENDIENTE";

  return String(estado).toUpperCase();
}

function claseDeEstado(estado) {
  if (estado === "COMPLETADA") return "synced";
  if (estado === "PARCIAL") return "partial";
  if (estado === "FALLIDA") return "failed";
  return "pending";
}

function leerDocente() {
  try {
    return JSON.parse(sessionStorage.getItem("edumonitor_usuario") || "null")?.nombre || "";
  } catch {
    return "";
  }
}

function prefiereMovimiento() {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    !window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

// Solo visual: cuenta desde 0 hasta el valor que devolvió la API, en unos 800 ms.
function Contador({ valor }) {
  const numero = Number(valor);
  const animar = Number.isFinite(numero) && numero > 0 && prefiereMovimiento();
  const [mostrado, setMostrado] = useState(animar ? 0 : valor);

  useEffect(() => {
    if (!animar) return undefined;

    let cuadro;
    const inicio = performance.now();

    function paso(ahora) {
      const avance = Math.min((ahora - inicio) / 800, 1);
      const suavizado = 1 - Math.pow(1 - avance, 3);
      setMostrado(Math.round(numero * suavizado));
      if (avance < 1) cuadro = requestAnimationFrame(paso);
    }

    cuadro = requestAnimationFrame(paso);
    return () => cancelAnimationFrame(cuadro);
  }, [animar, numero, valor]);

  return (
    <span className="registros-valor mono" aria-label={String(valor)}>
      {animar ? mostrado : valor}
    </span>
  );
}

// Indicador visual mientras la API sincroniza; recorre las etapas en el orden en que el backend importa.
const ETAPAS = ["Unidades", "Actividades", "Estudiantes", "Calificaciones"];

function EtapasSincronizacion() {
  const [actual, setActual] = useState(0);

  useEffect(() => {
    if (!prefiereMovimiento()) return undefined;
    const intervalo = setInterval(
      () => setActual((etapa) => Math.min(etapa + 1, ETAPAS.length - 1)),
      900
    );
    return () => clearInterval(intervalo);
  }, []);

  return (
    <span className="etapas" aria-hidden="true">
      {ETAPAS.map((etapa, indice) => (
        <span
          key={etapa}
          className={
            indice < actual ? "etapa hecha" : indice === actual ? "etapa actual" : "etapa"
          }
        >
          {etapa}
        </span>
      ))}
    </span>
  );
}

function obtenerMensajeError(error) {
  const estadoHTTP = error?.response?.status;

  if (estadoHTTP === 403) {
    return "Este curso no está asignado a tu cuenta";
  }

  if (estadoHTTP === 502) {
    return "Moodle no responde; intenta más tarde";
  }

  return (
    error?.response?.data?.detail ||
    error?.response?.data?.title ||
    (error?.response
      ? "No se pudo completar la operación."
      : "No se pudo conectar con el servidor. Intenta de nuevo.")
  );
}

export default function Cursos() {
  const navigate = useNavigate();

  const [cursos, setCursos] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [periodoSeleccionado, setPeriodoSeleccionado] = useState("");
  const [estadoSeleccionado, setEstadoSeleccionado] = useState("");

  const [sincronizandoId, setSincronizandoId] = useState(null);
  const [historialAbiertoId, setHistorialAbiertoId] = useState(null);
  const [historialPorCurso, setHistorialPorCurso] = useState({});
  const [cargandoHistorialId, setCargandoHistorialId] = useState(null);
  const [errorOperacion, setErrorOperacion] = useState("");
  const [resultadoPorCurso, setResultadoPorCurso] = useState({});

  useEffect(() => {
    let activo = true;

    async function cargarCursos() {
      setCargando(true);
      setError("");

      try {
        const datos = await obtenerCursos();

        if (activo) {
          setCursos(Array.isArray(datos) ? datos : []);
        }
      } catch {
        if (activo) {
          setError(
            "No fue posible cargar los cursos"
          );
        }
      } finally {
        if (activo) {
          setCargando(false);
        }
      }
    }

    cargarCursos();

    return () => {
      activo = false;
    };
  }, []);

  const periodos = useMemo(() => {
    return [
      ...new Set(
        cursos
          .map((curso) => curso.periodo)
          .filter(Boolean)
      ),
    ];
  }, [cursos]);

  const cursosFiltrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();

    return cursos.filter((curso) => {
      const coincideBusqueda =
        !texto ||
        [
          curso.nombre,
          curso.nombreCorto,
        ]
          .filter(Boolean)
          .some((valor) =>
            String(valor).toLowerCase().includes(texto)
          );

      const coincidePeriodo =
        !periodoSeleccionado ||
        curso.periodo === periodoSeleccionado;

      // Solo COMPLETADA cuenta como sincronizado: PARCIAL tiene datos no disponibles (RNF-03).
      const estaSincronizado = obtenerEstadoCurso(curso) === "COMPLETADA";

      const coincideEstado =
        !estadoSeleccionado ||
        (estadoSeleccionado === "sincronizado" &&
          estaSincronizado) ||
        (estadoSeleccionado === "pendiente" &&
          !estaSincronizado);

      return (
        coincideBusqueda &&
        coincidePeriodo &&
        coincideEstado
      );
    });
  }, [
    cursos,
    busqueda,
    periodoSeleccionado,
    estadoSeleccionado,
  ]);

  function limpiarFiltros() {
    setBusqueda("");
    setPeriodoSeleccionado("");
    setEstadoSeleccionado("");
  }

  async function manejarSincronizacion(curso) {
    setSincronizandoId(curso.id);
    setErrorOperacion("");

    try {
      const resultado = await sincronizarCurso(curso.id);

      setResultadoPorCurso((anterior) => ({
        ...anterior,
        [curso.id]: resultado,
      }));

      setCursos((anteriores) =>
        anteriores.map((item) =>
          item.id === curso.id
            ? {
                ...item,
                ultimaSincronizacion: {
                  id: resultado?.id,
                  fecha:
                    resultado?.fin ||
                    resultado?.fecha ||
                    new Date().toISOString(),
                  estado:
                    resultado?.estado || "COMPLETADA",
                },
              }
            : item
        )
      );

      try {
        const historial =
          await obtenerHistorialSincronizaciones(curso.id);

        setHistorialPorCurso((anterior) => ({
          ...anterior,
          [curso.id]: Array.isArray(historial)
            ? historial
            : [],
        }));
      } catch {
        // El resultado de la sincronización sigue disponible
        // aunque no se pueda actualizar el historial.
      }
    } catch (err) {
      setErrorOperacion(
        `No se pudo sincronizar "${curso.nombre || curso.nombreCorto || "el curso"}": ${obtenerMensajeError(err)}`
      );
    } finally {
      setSincronizandoId(null);
    }
  }

  async function manejarHistorial(curso) {
    if (historialAbiertoId === curso.id) {
      setHistorialAbiertoId(null);
      return;
    }

    setHistorialAbiertoId(curso.id);
    setErrorOperacion("");

    if (historialPorCurso[curso.id]) {
      return;
    }

    setCargandoHistorialId(curso.id);

    try {
      const historial =
        await obtenerHistorialSincronizaciones(curso.id);

      setHistorialPorCurso((anterior) => ({
        ...anterior,
        [curso.id]: Array.isArray(historial)
          ? historial
          : [],
      }));
    } catch (err) {
      setErrorOperacion(
        `No se pudo cargar el historial: ${obtenerMensajeError(err)}`
      );

      setHistorialPorCurso((anterior) => ({
        ...anterior,
        [curso.id]: [],
      }));
    } finally {
      setCargandoHistorialId(null);
    }
  }

  async function manejarCierreSesion() {
    try {
      await cerrarSesion();
    } finally {
      navigate("/login");
    }
  }

  function obtenerEstadoCurso(curso) {
    // Contrato GET /api/cursos: ultimaSincronizacion es null si el curso nunca se sincronizó.
    return etiquetaEstado(curso.ultimaSincronizacion?.estado);
  }

  const docente = leerDocente();

  return (
    <main className="courses-page">
      <header className="courses-header">
        <div>
          <p className="courses-eyebrow">
            {docente ? `Hola, ${docente}` : "Bienvenido"}
            {periodos.length > 0 && (
              <>
                {" · Período "}
                <span className="mono">{periodos.join(", ")}</span>
              </>
            )}
          </p>
          <h1>Mis cursos</h1>
          <p className="courses-count">
            <span className="courses-count-valor">
              {cursosFiltrados.length === 1
                ? "1 curso"
                : `${cursosFiltrados.length} cursos`}
            </span>
            {" "}en el registro
          </p>
        </div>

        <div className="courses-user">
          <button
            className="logout-button"
            type="button"
            onClick={manejarCierreSesion}
          >
            Cerrar sesión
          </button>
        </div>
      </header>

      <section className="course-filters" aria-label="Búsqueda y filtros">
        <input
          aria-label="Buscar cursos"
          className="course-search"
          placeholder="Buscar por nombre del curso..."
          type="search"
          value={busqueda}
          onChange={(event) =>
            setBusqueda(event.target.value)
          }
        />

        <select
          aria-label="Filtrar por período"
          className="course-filter"
          value={periodoSeleccionado}
          onChange={(event) =>
            setPeriodoSeleccionado(event.target.value)
          }
        >
          <option value="">Todos los períodos</option>

          {periodos.map((periodo) => (
            <option key={periodo} value={periodo}>
              {periodo}
            </option>
          ))}
        </select>

        <select
          aria-label="Filtrar por sincronización"
          className="course-filter"
          value={estadoSeleccionado}
          onChange={(event) =>
            setEstadoSeleccionado(event.target.value)
          }
        >
          <option value="">Todos los estados</option>
          <option value="sincronizado">Sincronización completa</option>
          <option value="pendiente">Incompleta o sin sincronizar</option>
        </select>

        <button
          className="clear-filters-button"
          type="button"
          onClick={limpiarFiltros}
        >
          Limpiar filtros
        </button>
      </section>

      {error && (
        <section className="courses-error" role="alert">
          <p>{error}</p>

          <button
            type="button"
            onClick={() => window.location.reload()}
          >
            Reintentar
          </button>
        </section>
      )}

      {errorOperacion && (
        <section className="courses-error" role="alert">
          <p>{errorOperacion}</p>
          <button
            type="button"
            onClick={() => setErrorOperacion("")}
          >
            Cerrar
          </button>
        </section>
      )}

      {cargando ? (
        <p className="courses-loading" role="status">Cargando cursos...</p>
      ) : cursosFiltrados.length === 0 ? (
        <div className="empty-courses">
          <h2>No hay cursos disponibles</h2>
          <p>Aún no tienes cursos</p>
        </div>
      ) : (
        <section className="courses-grid">
          {cursosFiltrados.map((curso) => {
            const estado = obtenerEstadoCurso(curso);
            const claseEstado = claseDeEstado(estado);

            const resultado = resultadoPorCurso[curso.id];
            const historial = historialPorCurso[curso.id];
            const sincronizando = sincronizandoId === curso.id;

            return (
              <article
                className="course-card"
                key={curso.id}
              >
                <div className="course-card-header">
                  <div>
                    {curso.nombreCorto && (
                      <p className="course-short-name mono">
                        {curso.nombreCorto}
                      </p>
                    )}
                    <h2>
                      {curso.nombre ||
                        curso.nombreCorto ||
                        "Curso sin nombre"}
                    </h2>
                  </div>

                  <div className={`sello ${claseEstado}`}>
                    <span className="sync-status">{estado}</span>
                    <span className="sello-fecha mono">
                      {curso.ultimaSincronizacion
                        ? formatearFecha(curso.ultimaSincronizacion.fecha)
                        : "Sin registro"}
                    </span>
                  </div>
                </div>

                <dl className="course-meta">
                  {docente && (
                    <div>
                      <dt>Docente</dt>
                      <dd>{docente}</dd>
                    </div>
                  )}
                  {curso.periodo && (
                    <div>
                      <dt>Período</dt>
                      <dd className="mono">{curso.periodo}</dd>
                    </div>
                  )}
                  {curso.ultimaSincronizacion && (
                    <div>
                      <dt>Última sincronización</dt>
                      <dd className="mono">
                        {formatearFecha(curso.ultimaSincronizacion.fecha)}
                      </dd>
                    </div>
                  )}
                </dl>

                <div className="course-actions">
                  <button
                    type="button"
                    className={`boton-primario ${sincronizando ? "en-curso" : ""}`}
                    onClick={() =>
                      manejarSincronizacion(curso)
                    }
                    disabled={sincronizando}
                    aria-busy={sincronizando}
                  >
                    {sincronizando
                      ? "Sincronizando..."
                      : "Sincronizar curso"}
                    {sincronizando && <EtapasSincronizacion />}
                  </button>

                  <button
                    type="button"
                    className="boton-secundario"
                    onClick={() => manejarHistorial(curso)}
                    disabled={
                      cargandoHistorialId === curso.id
                    }
                    aria-expanded={historialAbiertoId === curso.id}
                  >
                    {cargandoHistorialId === curso.id
                      ? "Cargando historial..."
                      : historialAbiertoId === curso.id
                        ? "Ocultar historial"
                        : "Ver historial"}
                  </button>
                </div>

                {resultado &&
                  ["PARCIAL", "FALLIDA"].includes(
                    String(resultado.estado).toUpperCase()
                  ) && (
                    <div
                      className={`sync-warning ${claseDeEstado(etiquetaEstado(resultado.estado))}`}
                      role="alert"
                    >
                      <strong>Sincronización incompleta</strong>

                      <p>
                        La sincronización no se completó correctamente.
                        Revisa los errores reportados.
                      </p>

                      {resultado.errores?.length > 0 && (
                        <ul>
                          {resultado.errores.map((error, indice) => (
                            <li key={indice}>
                              <code>{error.funcion || "Sistema"}</code>{" "}
                              {error.mensaje || "Error sin descripción"}
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  )}

                {resultado && (
                  <section className="sync-result">
                    <div className="sync-result-header">
                      <h3>Resultado de sincronización</h3>
                      <span className={`sello sello-compacto ${claseDeEstado(etiquetaEstado(resultado.estado))}`}>
                        {etiquetaEstado(resultado.estado)}
                      </span>
                    </div>

                    {resultado.registros && (
                      <div>
                        <h4 className="sr-only">Registros procesados</h4>
                        <ul className="registros">
                          {Object.entries(
                            resultado.registros
                          ).map(([nombre, cantidad]) => (
                            <li key={nombre}>
                              <Contador valor={cantidad} />
                              <span className="registros-nombre">{nombre}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    <p className="sync-result-tiempos mono">
                      {resultado.inicio && (
                        <span>
                          Inicio: {formatearFecha(resultado.inicio)}
                        </span>
                      )}
                      {resultado.fin && (
                        <span>
                          Fin: {formatearFecha(resultado.fin)}
                        </span>
                      )}
                    </p>

                    {resultado.errores?.length > 0 && (
                      <div className="sync-result-errores">
                        <h4>Errores reportados</h4>
                        <ul>
                          {resultado.errores.map(
                            (item, indice) => (
                              <li key={indice}>
                                {item.funcion && (
                                  <code>
                                    {item.funcion}:{" "}
                                  </code>
                                )}
                                {item.mensaje ||
                                  "Error sin descripción"}
                              </li>
                            )
                          )}
                        </ul>
                      </div>
                    )}
                  </section>
                )}

                {historialAbiertoId === curso.id && (
                  <section className="sync-history">
                    <h3>Historial de sincronizaciones</h3>

                    {cargandoHistorialId === curso.id ? (
                      <p role="status">Cargando historial...</p>
                    ) : !historial?.length ? (
                      <p>No hay sincronizaciones registradas.</p>
                    ) : (
                      <div className="table-scroll">
                        <table className="sync-history-table">
                          <thead>
                            <tr>
                              <th>Fecha</th>
                              <th>Estado</th>
                              <th>Registros</th>
                              <th>Errores</th>
                            </tr>
                          </thead>

                          <tbody>
                            {[...historial]
                              .sort(
                                (a, b) =>
                                  new Date(b.fin || b.inicio || b.fecha || 0) -
                                  new Date(a.fin || a.inicio || a.fecha || 0)
                              )
                              .map((item, indice) => {
                                const totalRegistros = Object.values(
                                  item.registros || {}
                                ).reduce(
                                  (total, cantidad) =>
                                    total + (Number(cantidad) || 0),
                                  0
                                );

                                return (
                                  <tr
                                    key={
                                      item.id ??
                                      `${curso.id}-${indice}`
                                    }
                                    className={claseDeEstado(etiquetaEstado(item.estado))}
                                  >
                                    <td className="mono">
                                      {formatearFecha(
                                        item.fin || item.inicio || item.fecha
                                      )}
                                    </td>

                                    <td>
                                      <span className="historial-estado">
                                        {etiquetaEstado(item.estado)}
                                      </span>
                                    </td>

                                    <td className="mono">{totalRegistros}</td>

                                    <td>
                                      {item.errores?.length > 0 ? (
                                        <ul>
                                          {item.errores.map(
                                            (error, errorIndice) => (
                                              <li key={errorIndice}>
                                                <code>
                                                  {error.funcion || "Sistema"}:
                                                </code>{" "}
                                                {error.mensaje ||
                                                  "Error sin descripción"}
                                              </li>
                                            )
                                          )}
                                        </ul>
                                      ) : (
                                        "Sin errores"
                                      )}
                                    </td>
                                  </tr>
                                );
                              })}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </section>
                )}
              </article>
            );
          })}
        </section>
      )}
    </main>
  );
}
