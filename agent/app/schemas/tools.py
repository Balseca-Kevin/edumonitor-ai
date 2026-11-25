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


class ChatPeticion(BaseModel):
    mensaje: str = Field(min_length=1)
    token: str = Field(min_length=1, repr=False)

class ChatRespuesta(BaseModel):
    respuesta: str
    herramientas: List[TrazaHerramienta]
    modelo: str
    duracionMs: int
