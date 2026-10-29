
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
          curso.descripcion,
          curso.docente,
        ]
          .filter(Boolean)
          .some((valor) =>
            String(valor).toLowerCase().includes(texto)
          );

      const coincidePeriodo =
        !periodoSeleccionado ||
        curso.periodo === periodoSeleccionado;

      const estado = String(
        curso.ultimaSincronizacion?.estado ||
          curso.estadoSincronizacion ||
          curso.estadoSync ||
          (curso.sincronizado || curso.sync
            ? "COMPLETADA"
            : "PENDIENTE")
      ).toUpperCase();

      const estaSincronizado = [
        "COMPLETADA",
        "PARCIAL",
        "SINCRONIZADO",
      ].includes(estado);

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
    return etiquetaEstado(
      curso.ultimaSincronizacion?.estado ||
        curso.estadoSincronizacion ||
        curso.estadoSync ||
        (curso.sincronizado || curso.sync
          ? "COMPLETADA"
          : "PENDIENTE")
    );
  }

  return (
    <main className="courses-page">
      <header className="courses-header">
        <div>
          <h1>Mis cursos</h1>
          <p>
            Consulta y administra tus cursos académicos.
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

      <section className="course-filters">
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
          <option value="sincronizado">Sincronizados</option>
          <option value="pendiente">Pendientes</option>
        </select>

        <button
          className="clear-filters-button"
          type="button"
          onClick={limpiarFiltros}
        >
          Limpiar filtros
        </button>
      </section>

      <section className="courses-summary">
        <p>
          {cursosFiltrados.length === 1
            ? "1 curso"
            : `${cursosFiltrados.length} cursos`}
        </p>
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
        <p role="status">Cargando cursos...</p>
      ) : cursosFiltrados.length === 0 ? (
        <div className="empty-courses">
          <h2>No hay cursos disponibles</h2>
          <p>Aún no tienes cursos</p>
        </div>
      ) : (
        <section className="courses-grid">
          {cursosFiltrados.map((curso) => {
            const estado = obtenerEstadoCurso(curso);
            const estaSincronizado = [
              "COMPLETADA",
              "PARCIAL",
              "SINCRONIZADO",
            ].includes(estado);

            const resultado = resultadoPorCurso[curso.id];
            const historial = historialPorCurso[curso.id];

            return (
              <article
                className="course-card"
                key={curso.id}
              >
                <div className="course-card-header">
                  <h2>
                    {curso.nombre ||
                      curso.nombreCorto ||
                      "Curso sin nombre"}
                  </h2>

                  <span
                    className={`sync-status ${
                      estaSincronizado
                        ? "synced"
                        : "pending"
                    }`}
                  >
                    {estado}
                  </span>
                </div>

                {curso.nombreCorto && (
                  <p className="course-short-name">
                    {curso.nombreCorto}
                  </p>
                )}

                {curso.descripcion && (
                  <p className="course-description">
                    {curso.descripcion}
                  </p>
                )}

                {curso.periodo && (
                  <p className="course-period">
                    <strong>Período:</strong>{" "}
                    {curso.periodo}
                  </p>
                )}

                {curso.docente && (
                  <p className="course-teacher">
                    <strong>Docente:</strong>{" "}
                    {curso.docente}
                  </p>
                )}

                {curso.ultimaSincronizacion && (
                  <div className="course-sync-details">
                    <p>
                      <strong>
                        Última sincronización:
                      </strong>{" "}
                      {formatearFecha(
                        curso.ultimaSincronizacion.fecha
                      )}
                    </p>
                  </div>
                )}

                <div className="course-actions">
                  <button
                    type="button"
                    onClick={() =>
                      manejarSincronizacion(curso)
                    }
                    disabled={sincronizandoId === curso.id}
                  >
                    {sincronizandoId === curso.id
                      ? "Sincronizando..."
                      : "Sincronizar curso"}
                  </button>

                  <button
                    type="button"
                    onClick={() => manejarHistorial(curso)}
                    disabled={
                      cargandoHistorialId === curso.id
                    }
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
      className="sync-warning"
      role="alert"
      style={{
        backgroundColor: "#fff3cd",
        border: "1px solid #ffecb5",
        borderRadius: "8px",
        color: "#664d03",
        padding: "12px",
        marginTop: "12px",
      }}
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
              <strong>{error.funcion || "Sistema"}:</strong>{" "}
              {error.mensaje || "Error sin descripción"}
            </li>
          ))}
        </ul>
      )}
    </div>
  )}
                {resultado && (
                  <section className="sync-result">
                    <h3>Resultado de sincronización</h3>

                    <p>
                      <strong>Estado:</strong>{" "}
                      {etiquetaEstado(resultado.estado)}
                    </p>

                    {resultado.inicio && (
                      <p>
                        <strong>Inicio:</strong>{" "}
                        {formatearFecha(resultado.inicio)}
                      </p>
                    )}

                    {resultado.fin && (
                      <p>
                        <strong>Fin:</strong>{" "}
                        {formatearFecha(resultado.fin)}
                      </p>
                    )}

                    {resultado.registros && (
                      <div>
                        <h4>Registros procesados</h4>
                        <ul>
                          {Object.entries(
                            resultado.registros
                          ).map(([nombre, cantidad]) => (
                            <li key={nombre}>
                              <strong>{nombre}:</strong>{" "}
                              {cantidad}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {resultado.errores?.length > 0 && (
                      <div>
                        <h4>Errores reportados</h4>
                        <ul>
                          {resultado.errores.map(
                            (item, indice) => (
                              <li key={indice}>
                                {item.funcion && (
                                  <strong>
                                    {item.funcion}:{" "}
                                  </strong>
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
      <div style={{ overflowX: "auto" }}>
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
                  >
                    <td>
                      {formatearFecha(
                        item.fin || item.inicio || item.fecha
                      )}
                    </td>

                    <td>
                      {etiquetaEstado(item.estado)}
                    </td>

                    <td>{totalRegistros}</td>

                    <td>
                      {item.errores?.length > 0 ? (
                        <ul>
                          {item.errores.map(
                            (error, errorIndice) => (
                              <li key={errorIndice}>
                                <strong>
                                  {error.funcion || "Sistema"}:
                                </strong>{" "}
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
