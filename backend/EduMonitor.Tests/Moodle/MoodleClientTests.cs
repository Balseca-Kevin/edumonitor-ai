using System.Net;
using System.Text;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Moodle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduMonitor.Tests.Moodle;

/// <summary>TA-02.2 · Cliente REST de Moodle contra respuestas JSON con la forma real del Moodle de laboratorio (4.5).</summary>
public class MoodleClientTests
{
    private const string TokenPrueba = "token-de-prueba-1234567890abcd";

    private sealed class MoodleFalso : HttpMessageHandler
    {
        public Queue<Func<HttpResponseMessage>> Respuestas { get; } = new();
        public List<Dictionary<string, string>> Peticiones { get; } = new();
        public List<Uri> Urls { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken ct)
        {
            Urls.Add(peticion.RequestUri!);
            var cuerpo = await peticion.Content!.ReadAsStringAsync(ct);
            Peticiones.Add(cuerpo.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1])));
            return Respuestas.Dequeue()();
        }

        public void Json(string json) => Respuestas.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    private static (MoodleClient Cliente, MoodleFalso Moodle) Crear(string token = TokenPrueba)
    {
        var moodle = new MoodleFalso();
        var opciones = new ConfiguracionMoodleEnMemoria(Options.Create(new MoodleOptions { Url = "http://moodle.test/", Token = token }), TimeProvider.System);
        var cliente = new MoodleClient(new HttpClient(moodle), opciones, NullLogger<MoodleClient>.Instance)
        {
            EsperaEntreReintentos = TimeSpan.Zero,
        };
        return (cliente, moodle);
    }

    [Fact]
    public async Task Envia_el_token_en_el_cuerpo_y_no_en_la_url()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"sitename":"Moodle-Lab-UTA","username":"edumonitor_ws_user","release":"4.5.15+ (Build: 20251003)","functions":[{"name":"core_webservice_get_site_info","version":"2024100700"}]}""");

        await cliente.ObtenerInfoSitioAsync();

        Assert.Equal("http://moodle.test/webservice/rest/server.php", moodle.Urls[0].ToString());
        Assert.DoesNotContain(TokenPrueba, moodle.Urls[0].ToString());
        Assert.Equal(TokenPrueba, moodle.Peticiones[0]["wstoken"]);
        Assert.Equal("core_webservice_get_site_info", moodle.Peticiones[0]["wsfunction"]);
        Assert.Equal("json", moodle.Peticiones[0]["moodlewsrestformat"]);
    }

    [Fact]
    public async Task Info_del_sitio_trae_version_corta_usuario_y_funciones()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"sitename":"Moodle-Lab-UTA","username":"edumonitor_ws_user","release":"4.5.15+ (Build: 20251003)","version":"2024100715","functions":[{"name":"core_webservice_get_site_info","version":"1"},{"name":"mod_assign_get_submissions","version":"1"}]}""");

        var info = await cliente.ObtenerInfoSitioAsync();

        Assert.Equal("Moodle-Lab-UTA", info.Sitio);
        Assert.Equal("4.5", info.Version);
        Assert.Equal("edumonitor_ws_user", info.UsuarioTecnico);
        Assert.Equal(new[] { "core_webservice_get_site_info", "mod_assign_get_submissions" }, info.Funciones);
    }

    [Fact]
    public async Task Error_de_moodle_con_http_200_lanza_no_disponible_sin_el_token()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"exception":"moodle_exception","errorcode":"invalidtoken","message":"Invalid token - token not found"}""");

        var error = await Assert.ThrowsAsync<MoodleNoDisponibleException>(() => cliente.ObtenerInfoSitioAsync());

        Assert.Equal("core_webservice_get_site_info", error.Funcion);
        Assert.Contains("invalidtoken", error.Message);
        Assert.DoesNotContain(TokenPrueba, error.Message);
        Assert.Single(moodle.Peticiones); // un error de Moodle no se reintenta
    }

    [Fact]
    public async Task Reintenta_ante_5xx_y_responde_si_moodle_se_recupera()
    {
        var (cliente, moodle) = Crear();
        moodle.Respuestas.Enqueue(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        moodle.Json("""[{"id":2,"shortname":"AGIL-4B","fullname":"Metodologías Ágiles (lab)"}]""");

        var cursos = await cliente.ObtenerCursosDeUsuarioAsync(4);

        Assert.Equal(2, moodle.Peticiones.Count);
        Assert.Equal("4", moodle.Peticiones[1]["userid"]);
        var curso = Assert.Single(cursos);
        Assert.Equal((2, "AGIL-4B", "Metodologías Ágiles (lab)"), (curso.Id, curso.NombreCorto, curso.Nombre));
    }

    [Fact]
    public async Task Moodle_caido_se_rinde_tras_los_reintentos()
    {
        var (cliente, moodle) = Crear();
        for (var i = 0; i <= MoodleClient.Reintentos; i++)
        {
            moodle.Respuestas.Enqueue(() => throw new HttpRequestException("Connection refused"));
        }

        var error = await Assert.ThrowsAsync<MoodleNoDisponibleException>(() => cliente.ObtenerMatriculadosAsync(2));

        Assert.Equal("core_enrol_get_enrolled_users", error.Funcion);
        Assert.Equal(MoodleClient.Reintentos + 1, moodle.Peticiones.Count);
    }

    [Fact]
    public async Task Sin_token_configurado_no_llama_a_moodle()
    {
        var (cliente, moodle) = Crear(token: "");

        var error = await Assert.ThrowsAsync<MoodleNoDisponibleException>(() => cliente.ObtenerInfoSitioAsync());

        Assert.Contains("Moodle__Token", error.Message);
        Assert.Empty(moodle.Peticiones);
    }

    [Fact]
    public async Task Matriculados_pide_solo_campos_minimos_y_pagina()
    {
        var (cliente, moodle) = Crear();
        var pagina = string.Join(",", Enumerable.Range(1, MoodleClient.TamanoPagina)
            .Select(i => $$"""{"id":{{i}},"firstname":"N{{i}}","lastname":"A{{i}}","fullname":"N A","roles":[{"roleid":5,"name":"","shortname":"student","sortorder":0}]}"""));
        moodle.Json($"[{pagina}]");
        moodle.Json("""[{"id":500,"firstname":"Docente","lastname":"Uno","fullname":"Docente Uno","roles":[{"roleid":3,"name":"","shortname":"editingteacher","sortorder":0}]}]""");

        var matriculados = await cliente.ObtenerMatriculadosAsync(2);

        Assert.Equal(MoodleClient.TamanoPagina + 1, matriculados.Count);
        Assert.Equal("id,firstname,lastname,roles", moodle.Peticiones[0]["options[0][value]"]);
        Assert.Equal("0", moodle.Peticiones[0]["options[1][value]"]);
        Assert.Equal(MoodleClient.TamanoPagina.ToString(), moodle.Peticiones[1]["options[1][value]"]);
        Assert.Equal(new[] { "editingteacher" }, matriculados[^1].Roles);
    }

    [Fact]
    public async Task Contenido_del_curso_trae_secciones_y_modulos()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""[{"id":1,"name":"General","section":0,"modules":[]},{"id":2,"name":"Unidad 1","summary":"Parcial 1","section":1,"modules":[{"id":1,"name":"Tarea 1","instance":1,"modname":"assign"}]}]""");

        var secciones = await cliente.ObtenerContenidoCursoAsync(2);

        Assert.Equal(2, secciones.Count);
        Assert.Equal((2, 1, "Unidad 1"), (secciones[1].Id, secciones[1].Numero, secciones[1].Nombre));
        var modulo = Assert.Single(secciones[1].Modulos);
        Assert.Equal((1, 1, "Tarea 1", "assign"), (modulo.Id, modulo.Instancia, modulo.Nombre, modulo.TipoModulo));
        Assert.Equal("Parcial 1", secciones[1].Resumen); // TA-08.A1: el resumen lleva los temas del sílabo
        Assert.Null(secciones[0].Resumen);
    }

    [Fact]
    public async Task Items_de_calificacion_distinguen_nota_cero_de_sin_nota_y_nombran_categorias()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""
            {"usergrades":[{"courseid":2,"userid":10,"gradeitems":[
              {"id":12,"itemname":"Tarea 3","itemtype":"mod","itemmodule":"assign","iteminstance":5,"categoryid":3,"weightraw":0.25,"graderaw":0,"grademax":10},
              {"id":13,"itemname":"Tarea 4","itemtype":"mod","itemmodule":"assign","iteminstance":6,"categoryid":3,"weightraw":0,"graderaw":null,"grademax":10},
              {"id":3,"itemname":null,"itemtype":"category","itemmodule":null,"iteminstance":3,"idnumber":"Tareas","categoryid":null,"weightraw":0.3,"graderaw":7.5,"grademax":10},
              {"id":1,"itemname":null,"itemtype":"course","itemmodule":null,"iteminstance":1,"idnumber":null,"categoryid":null,"graderaw":7.9,"grademax":10}
            ]}],"warnings":[]}
            """);

        var reporte = Assert.Single(await cliente.ObtenerItemsCalificacionAsync(2));

        Assert.Equal("0", moodle.Peticiones[0]["userid"]); // todos los estudiantes del curso
        Assert.Equal(10, reporte.UsuarioId);
        Assert.Equal(0m, reporte.Items[0].Nota);
        Assert.Null(reporte.Items[1].Nota);
        Assert.Equal((3, "Tareas", 0.3m), (reporte.Items[2].Instancia!.Value, reporte.Items[2].Nombre, reporte.Items[2].Peso!.Value));
        Assert.Equal(3, reporte.Items[0].CategoriaId);
        Assert.Equal("Total del curso", reporte.Items[3].Nombre);
    }

    [Fact]
    public async Task Tareas_incluyen_cursos_sin_matricula_y_fecha_limite()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"courses":[{"id":2,"assignments":[{"id":1,"cmid":1,"name":"Tarea 1","duedate":1788908340},{"id":2,"cmid":2,"name":"Sin fecha","duedate":0}]}],"warnings":[]}""");

        var tareas = await cliente.ObtenerTareasAsync(2);

        Assert.Equal("1", moodle.Peticiones[0]["includenotenrolledcourses"]);
        Assert.Equal("2", moodle.Peticiones[0]["courseids[0]"]);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1788908340), tareas[0].FechaLimite);
        Assert.Null(tareas[1].FechaLimite);
    }

    [Fact]
    public async Task Tareas_sin_permiso_no_se_confunden_con_un_curso_sin_tareas()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"courses":[],"warnings":[{"item":"course","itemid":2,"warningcode":"2","message":"User is not enrolled or does not have requested capability"}]}""");

        var error = await Assert.ThrowsAsync<MoodleNoDisponibleException>(() => cliente.ObtenerTareasAsync(2));

        Assert.Equal("mod_assign_get_assignments", error.Funcion);
    }

    [Fact]
    public async Task Entregas_traen_estado_y_estado_de_calificacion()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"assignments":[{"assignmentid":10,"submissions":[{"id":278,"userid":6,"status":"submitted","gradingstatus":"graded"},{"id":279,"userid":7,"status":"submitted","gradingstatus":"notgraded"}]}],"warnings":[]}""");

        var entregas = await cliente.ObtenerEntregasAsync(new[] { 10, 11 });

        Assert.Equal("11", moodle.Peticiones[0]["assignmentids[1]"]);
        Assert.Equal(2, entregas.Count);
        Assert.Equal((10, 7, "submitted", "notgraded"), (entregas[1].TareaId, entregas[1].UsuarioId, entregas[1].Estado, entregas[1].EstadoCalificacion));
    }

    [Fact]
    public async Task Entregas_sin_tareas_no_llaman_a_moodle()
    {
        var (cliente, moodle) = Crear();

        Assert.Empty(await cliente.ObtenerEntregasAsync(Array.Empty<int>()));
        Assert.Empty(moodle.Peticiones);
    }

    [Fact]
    public async Task Finalizacion_trae_estado_por_modulo()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"statuses":[{"cmid":1,"modname":"assign","instance":1,"state":1,"timecompleted":1793249199,"tracking":1},{"cmid":2,"modname":"assign","instance":2,"state":0,"timecompleted":0,"tracking":1}],"warnings":[]}""");

        var estados = await cliente.ObtenerFinalizacionAsync(2, 10);

        Assert.Equal(new[] { (1, 1), (2, 0) }, estados.Select(e => (e.ModuloCursoId, e.Estado)));
    }

    [Theory]
    [InlineData("4.5.15+ (Build: 20251003)", "4.5")]
    [InlineData("4.4", "4.4")]
    [InlineData("", "")]
    public void Version_corta(string release, string esperado) => Assert.Equal(esperado, MoodleClient.VersionCorta(release));

    // HU-08 · Formas tomadas del Moodle de laboratorio 4.5 (Tarea 3 de AGIL-4B), recortadas a un criterio.
    [Fact]
    public async Task Rubricas_trae_los_criterios_con_sus_niveles_en_texto_y_omite_las_tareas_sin_rubrica()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"areas":[{"cmid":5,"contextid":55,"component":"mod_assign","areaname":"submissions","activemethod":"rubric","definitions":[{"id":3,"method":"rubric","name":"Rúbrica · Tarea 3","status":20,"rubric":{"rubric_criteria":[{"id":10,"sortorder":2,"description":"Comparación de enfoques &amp; criterios","descriptionformat":1,"levels":[{"id":37,"score":0,"definition":"Insuficiente","definitionformat":1},{"id":40,"score":2.5,"definition":"Destacado","definitionformat":1}]}]}}]},{"cmid":6,"contextid":56,"component":"mod_assign","areaname":"submissions","activemethod":null}],"warnings":[]}""");

        var rubricas = await cliente.ObtenerRubricasAsync(new[] { 5, 6 });

        var rubrica = Assert.Single(rubricas);
        Assert.Equal((5, 3), (rubrica.ModuloCursoId, rubrica.DefinicionId));
        var criterio = Assert.Single(rubrica.Criterios);
        Assert.Equal((10, 2, "Comparación de enfoques & criterios"), (criterio.Id, criterio.Orden, criterio.Descripcion));
        Assert.Equal(new[] { (37, 0m, "Insuficiente"), (40, 2.5m, "Destacado") }, criterio.Niveles.Select(n => (n.Id, n.Puntaje, n.Definicion)));
        Assert.Equal(("5", "6", "submissions", "0"), (moodle.Peticiones[0]["cmids[0]"], moodle.Peticiones[0]["cmids[1]"], moodle.Peticiones[0]["areaname"], moodle.Peticiones[0]["activeonly"]));
    }

    [Fact]
    public async Task Calificaciones_con_rubrica_traen_el_id_de_la_nota_su_estado_y_el_nivel_de_cada_criterio()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"instances":[{"id":68,"raterid":4,"itemid":131,"rawgrade":null,"status":1,"feedback":"","feedbackformat":0,"timemodified":1794853730,"rubric":{"criteria":[{"id":269,"criterionid":9,"levelid":33,"remark":"","remarkformat":1},{"id":270,"criterionid":10,"levelid":37,"remark":"","remarkformat":1}]}}],"warnings":[]}""");

        var instancia = Assert.Single(await cliente.ObtenerCalificacionesRubricaAsync(3));

        Assert.Equal((68, 131, 1), (instancia.Id, instancia.ItemId, instancia.Estado));
        Assert.Equal(new[] { (9, 33), (10, 37) }, instancia.Niveles.Select(n => (n.CriterioId, n.NivelId)));
        Assert.Equal(("3", "0"), (moodle.Peticiones[0]["definitionid"], moodle.Peticiones[0]["since"]));
    }

    [Fact]
    public async Task Estado_de_la_entrega_trae_el_id_de_la_nota_y_el_comentario_del_docente_sin_html()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"gradingsummary":null,"lastattempt":{},"feedback":{"grade":{"id":131,"assignment":5,"userid":10,"attemptnumber":0,"grader":4,"grade":"0.00000"},"gradefordisplay":"0.00 / 10.00","plugins":[{"type":"comments","name":"Feedback comments","fileareas":[{"area":"feedback","files":[]}],"editorfields":[{"name":"comments","description":"Feedback comments","text":"<p>Revisa estos puntos antes de la siguiente entrega:</p><ul><li>Valores del Manifiesto Ágil: relaciona cada práctica.</li><li>Formato y entrega: sigue la plantilla.</li></ul>","format":1}]}]},"warnings":[]}""");

        var estado = await cliente.ObtenerEstadoEntregaAsync(5, 10);

        Assert.Equal(131, estado.CalificacionId);
        Assert.Equal("Revisa estos puntos antes de la siguiente entrega:\n• Valores del Manifiesto Ágil: relaciona cada práctica.\n• Formato y entrega: sigue la plantilla.", estado.Comentario);
        Assert.Equal(("5", "10"), (moodle.Peticiones[0]["assignid"], moodle.Peticiones[0]["userid"]));
    }

    [Fact]
    public async Task Estado_de_la_entrega_sin_nota_ni_comentario_devuelve_nulos()
    {
        var (cliente, moodle) = Crear();
        moodle.Json("""{"lastattempt":{},"warnings":[]}""");

        var estado = await cliente.ObtenerEstadoEntregaAsync(5, 11);

        Assert.Equal((null, null), (estado.CalificacionId, estado.Comentario));
    }
}
