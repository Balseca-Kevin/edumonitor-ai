from pydantic import BaseModel, Field
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

class ChatPeticion(BaseModel):
    mensaje: str = Field(min_length=1)
    token: str = Field(min_length=1, repr=False)

class ChatRespuesta(BaseModel):
    respuesta: str
    herramientas: List[TrazaHerramienta]
    modelo: str
    duracionMs: int