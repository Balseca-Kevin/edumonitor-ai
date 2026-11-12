namespace EduMonitor.Application.Abstractions.Moodle;

// DTOs propios del cliente Moodle. Copian solo los campos que EduMonitor usa; el JSON crudo de Moodle no sale de Infrastructure.

/// <summary>Respuesta de <c>core_webservice_get_site_info</c>.</summary>
public sealed record InfoSitioMoodle(
    string Sitio,
    string Version,
    string UsuarioTecnico,
    IReadOnlyList<string> Funciones);

/// <summary>Curso de <c>core_enrol_get_users_courses</c>.</summary>
public sealed record CursoMoodle(int Id, string NombreCorto, string Nombre);

/// <summary>Sección de <c>core_course_get_contents</c>; en EduMonitor es una unidad.</summary>
public sealed record SeccionMoodle(int Id, int Numero, string Nombre, IReadOnlyList<ModuloMoodle> Modulos);

/// <summary>Módulo (actividad) dentro de una sección. <see cref="Instancia"/> es el id de la tarea, foro, etc.</summary>
public sealed record ModuloMoodle(int Id, int Instancia, string Nombre, string TipoModulo);

/// <summary>Matriculado de <c>core_enrol_get_enrolled_users</c>. Sin correo: no se copia del JSON de Moodle.</summary>
public sealed record MatriculadoMoodle(int Id, string Nombre, string Apellido, IReadOnlyList<string> Roles);

/// <summary>Ítems de calificación de un usuario (<c>gradereport_user_get_grade_items</c>).</summary>
public sealed record CalificacionesUsuarioMoodle(int UsuarioId, IReadOnlyList<ItemCalificacionMoodle> Items);

/// <summary>
/// Ítem del libro de calificaciones. <see cref="TipoItem"/> es <c>mod</c>, <c>category</c>, <c>course</c> o <c>manual</c>.
/// En los ítems <c>category</c>, <see cref="Instancia"/> es el id de la categoría y <see cref="Nombre"/> su nombre
/// (Exámenes, Tareas, APE); los demás ítems apuntan a ella con <see cref="CategoriaId"/>.
/// <see cref="Nota"/> es null cuando el estudiante no tiene nota; una nota 0 llega como 0.
/// </summary>
public sealed record ItemCalificacionMoodle(
    int Id,
    string Nombre,
    string TipoItem,
    string? Modulo,
    int? Instancia,
    int? CategoriaId,
    decimal? Peso,
    decimal? Nota,
    decimal? NotaMaxima);

/// <summary>Tarea de <c>mod_assign_get_assignments</c>. <see cref="ModuloCursoId"/> enlaza con <see cref="ModuloMoodle.Id"/>.</summary>
public sealed record TareaMoodle(int Id, int ModuloCursoId, string Nombre, DateTimeOffset? FechaLimite);

/// <summary>Entrega de <c>mod_assign_get_submissions</c>. <see cref="Estado"/>: <c>new</c>, <c>draft</c>, <c>submitted</c>...; <see cref="EstadoCalificacion"/>: <c>graded</c> o <c>notgraded</c>.</summary>
public sealed record EntregaMoodle(int TareaId, int UsuarioId, string Estado, string? EstadoCalificacion);

/// <summary>Estado de finalización de <c>core_completion_get_activities_completion_status</c> (0 incompleta, 1 completa, 2 aprobada, 3 reprobada).</summary>
public sealed record FinalizacionActividadMoodle(int ModuloCursoId, int Estado);
