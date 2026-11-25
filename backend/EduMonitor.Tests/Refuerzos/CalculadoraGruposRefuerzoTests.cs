using EduMonitor.Application.Refuerzos;
using EduMonitor.Application.Tematicas;

namespace EduMonitor.Tests.Refuerzos;

/// <summary>
/// TA-15.B1 · Grupos de refuerzo por tema con casos conocidos. «Historias de usuario» se evalúa en la Tarea 1 y la Tarea 2; Ana (4,0),
/// Diego (5,5) y Elena (6,0) tienen nota en el tema. El tema «Scrum» solo tiene un estudiante bajo 6,0.
/// </summary>
public class CalculadoraGruposRefuerzoTests
{
    private static readonly UnidadTemaDto U1 = new(1, "Principios");
    private static readonly UnidadTemaDto U2 = new(2, "Marcos");

    private static EvaluacionTemaDto Ev(int actividad, string nombre) => new(actividad, nombre, "NOTA", Array.Empty<string>());

    private static EstudianteTemaDto Est(int id, string nombre, string apellido, decimal nota, params int[] actividades) =>
        new(id, nombre, apellido, nota, actividades.Select(a => new NotaTemaActividadDto(a, nota)).ToList());

    private static TemaDesempenoDto Tema(int id, string nombre, UnidadTemaDto unidad, IReadOnlyList<EvaluacionTemaDto> evaluaciones, params EstudianteTemaDto[] estudiantes) =>
        new(id, nombre, unidad, evaluaciones, estudiantes.Length, null, null, Array.Empty<EstudianteBajoDto>(), estudiantes, Array.Empty<ProximaEvaluacionDto>());

    [Fact]
    public void Agrupa_solo_a_quienes_quedan_bajo_6_y_deja_fuera_el_6_exacto()
    {
        var historias = Tema(30, "Historias de usuario", U1, new[] { Ev(1, "Tarea 1"), Ev(2, "Tarea 2") },
            Est(12, "Elena", "Espinoza", 6.0m, 1, 2),
            Est(11, "Diego", "Díaz", 5.5m, 1, 2),
            Est(10, "Ana", "Acosta", 4.0m, 1, 2));

        var grupo = Assert.Single(CalculadoraGruposRefuerzo.Agrupar(new TematicasDto(1, null, new[] { historias }, Array.Empty<ActividadSinTemaDto>())).Grupos);

        Assert.Equal(new[] { "Ana", "Diego" }, grupo.Estudiantes.Select(e => e.Nombre));
        Assert.Equal((4.0m, 2), (grupo.Estudiantes[0].Promedio, grupo.Estudiantes[0].Notas));
        Assert.Equal(new ActividadPlanDto(2, "Tarea 2"), grupo.Actividad);
    }

    [Fact]
    public void Un_tema_con_un_solo_estudiante_bajo_6_no_forma_grupo()
    {
        var scrum = Tema(31, "Scrum", U2, new[] { Ev(3, "Tarea 3") }, Est(10, "Ana", "Acosta", 3.0m, 3), Est(11, "Diego", "Díaz", 8.0m, 3));

        Assert.Empty(CalculadoraGruposRefuerzo.Agrupar(new TematicasDto(1, null, new[] { scrum }, Array.Empty<ActividadSinTemaDto>())).Grupos);
    }

    [Fact]
    public void Un_estudiante_sin_notas_del_tema_no_entra_como_cero()
    {
        var historias = Tema(30, "Historias de usuario", U1, new[] { Ev(1, "Tarea 1") },
            Est(10, "Ana", "Acosta", 4.0m, 1),
            new EstudianteTemaDto(13, "Luis", "Mora", 0m, Array.Empty<NotaTemaActividadDto>()));

        Assert.Empty(CalculadoraGruposRefuerzo.Agrupar(new TematicasDto(1, null, new[] { historias }, Array.Empty<ActividadSinTemaDto>())).Grupos);
    }

    [Fact]
    public void La_actividad_del_grupo_es_la_ultima_que_ya_tiene_nota_y_los_grupos_van_de_mas_a_menos_estudiantes()
    {
        // La Tarea 9 es del tema pero todavía no tiene notas: el plan se arma sobre la Tarea 1.
        var historias = Tema(30, "Historias de usuario", U1, new[] { Ev(1, "Tarea 1"), Ev(9, "Tarea 9") },
            Est(10, "Ana", "Acosta", 4.0m, 1), Est(11, "Diego", "Díaz", 5.0m, 1));
        var scrum = Tema(31, "Scrum", U2, new[] { Ev(3, "Tarea 3") },
            Est(10, "Ana", "Acosta", 3.0m, 3), Est(11, "Diego", "Díaz", 2.0m, 3), Est(12, "Elena", "Espinoza", 5.9m, 3));

        var grupos = CalculadoraGruposRefuerzo.Agrupar(new TematicasDto(1, null, new[] { historias, scrum }, Array.Empty<ActividadSinTemaDto>()));

        Assert.Equal((6.0m, 2), (grupos.Umbral, grupos.MinimoEstudiantes));
        Assert.Equal(new[] { "Scrum", "Historias de usuario" }, grupos.Grupos.Select(g => g.Tema));
        Assert.Equal(new[] { "Diego", "Ana", "Elena" }, grupos.Grupos[0].Estudiantes.Select(e => e.Nombre));
        Assert.Equal(1, grupos.Grupos[1].Actividad.Id);
    }
}
