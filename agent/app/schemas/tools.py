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

    @field_validator("nombre", mode="before")
    @classmethod
    def vacio_es_ninguno(cls, valor):
        """El modelo manda nombre="" en las preguntas de todo el curso: se trata como si no viniera."""
        if isinstance(valor, str) and not valor.strip():
            return None
        return valor.strip() if isinstance(valor, str) else valor

class ChatPeticion(BaseModel):
    mensaje: str = Field(min_length=1)
    token: str = Field(min_length=1, repr=False)

class ChatRespuesta(BaseModel):
    respuesta: str
    herramientas: List[TrazaHerramienta]
    modelo: str
    duracionMs: int
