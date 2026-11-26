from pydantic import BaseModel, Field, field_validator
from typing import Optional, List, Literal
from datetime import datetime

class CursoAutorizado(BaseModel):
    id: int
    nombreCorto: str
    nombre: str
    ultimaSincronizacion: Optional[datetime] = None

class CursosAutorizadosRespuesta(BaseModel):
    herramienta: Literal["listar_cursos_autorizados"] = "listar_cursos_autorizados"
    generado: datetime
    cursos: List[CursoAutorizado]

class TrazaHerramienta(BaseModel):
    nombre: str
    parametros: dict
    estado: Literal["OK", "ERROR"]
    duracionMs: int

class ParametrosAlertas(BaseModel):
    model_config = {"extra": "forbid"}
    cursoId: int = Field(gt=0, strict=True)

class ParametrosCalificaciones(ParametrosAlertas):
    estudianteId: Optional[int] = Field(default=None, gt=0, strict=True)
    nombre: Optional[str] = Field(default=None, min_length=2, max_length=80, strict=True)
    # HU-23: filtro y orden del resumen hechos por el backend (docs/contratos-api.md, bloque Sprint 3).
    promedioMenorA: Optional[float] = Field(default=None, ge=0, le=10)
    orden: Optional[Literal["promedio", "sinEntrega"]] = None

    @field_validator("promedioMenorA", "orden", mode="before")
    @classmethod
    def filtro_vacio_es_ninguno(cls, valor):
        """El modelo manda "" cuando no quiere filtrar ni ordenar: se trata como si no viniera."""
        if isinstance(valor, str) and not valor.strip():
            return None
        return valor

    @field_validator("orden", mode="before")
    @classmethod
    def orden_conocido_o_por_defecto(cls, valor):
        """El modelo local a veces inventa el orden («asc», «promedioPonderado»). Un orden desconocido no anula la consulta: se usa
        el orden por defecto del backend (de menor a mayor promedio)."""
        if not isinstance(valor, str):
            return valor
        clave = valor.replace("_", "").replace(" ", "").lower()
        return {"promedio": "promedio", "sinentrega": "sinEntrega"}.get(clave)

    @field_validator("nombre", mode="before")
    @classmethod
    def vacio_es_ninguno(cls, valor):
        """El modelo manda nombre="" en las preguntas de todo el curso: se trata como si no viniera."""
        if isinstance(valor, str) and not valor.strip():
            return None
        return valor.strip() if isinstance(valor, str) else valor

class ParametrosNotaNecesaria(ParametrosAlertas):
    """TA-10.A1 · El backend calcula la nota necesaria; el modelo solo pasa el curso y el estudiante."""
    estudianteId: int = Field(gt=0, strict=True)


class ParametrosDesempenoTematico(ParametrosAlertas):
    """HU-08 · Desempeño por tema del curso o, con actividadId, de una actividad."""
    actividadId: Optional[int] = Field(default=None, gt=0, strict=True)


class ParametrosDetalleActividad(ParametrosAlertas):
    """HU-22 · Panel de una actividad: entregas, criterios de rúbrica, temas y quién requiere atención."""
    actividadId: int = Field(gt=0, strict=True)


class ParametrosRefuerzosAplicados(ParametrosAlertas):
    """TA-15.A2 · Refuerzos aprobados del curso con la nota del tema antes y después."""


class ParametrosPlanRefuerzo(ParametrosAlertas):
    """TA-15.A4 · Plan de refuerzo que el docente envió al asistente."""
    refuerzoId: int = Field(gt=0, strict=True)


class TurnoAnterior(BaseModel):
    """HU-23 · Un turno guardado de la conversación (pregunta y respuesta, ya seudonimizadas por la API)."""
    pregunta: str
    respuesta: str


class ChatPeticion(BaseModel):
    mensaje: str = Field(min_length=1)
    token: str = Field(min_length=1, repr=False)
    cursoId: Optional[int] = None
    # HU-23 · Últimos 4 turnos de la conversación, del más antiguo al más reciente.
    historial: List[TurnoAnterior] = Field(default_factory=list, max_length=4)
    # HU-23 · auto: externo con respaldo local; local: «Usar solo el modelo local»; externo: solo para la evaluación (TA-18.A1).
    modo: Literal["auto", "local", "externo"] = "auto"
    # C7 · Tramo de la barra del curso (todo, p1, p2, u1…): el agente lo pone en calificaciones y alertas, como las tarjetas.
    tramo: str = Field(default="todo", pattern=r"^(todo|p[12]|u[1-9][0-9]?)$")


class Cifra(BaseModel):
    """TA-23.A1 · Una cifra de la respuesta, de dónde sale y si quedó verificada."""
    valor: str
    herramienta: Optional[str] = None
    campo: Optional[str] = None
    estudianteId: Optional[int] = None
    actividadId: Optional[int] = None
    verificada: bool


class ChatRespuesta(BaseModel):
    respuesta: str
    herramientas: List[TrazaHerramienta]
    modelo: str
    duracionMs: int
    # HU-23 · EXTERNO, LOCAL o NINGUNO (respuesta por reglas, sin modelo). respaldo: el local respondió por una falla del externo.
    proveedor: Literal["EXTERNO", "LOCAL", "NINGUNO"] = "LOCAL"
    respaldo: bool = False
    cifras: List[Cifra] = Field(default_factory=list)
