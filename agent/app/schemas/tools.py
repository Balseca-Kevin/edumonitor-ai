from pydantic import BaseModel
from typing import List, Optional

class CursoSchema(BaseModel):
    id: int
    fullname: str
    shortname: str

class CursosAutorizadosResponse(BaseModel):
    success: bool
    data: Optional[List[CursoSchema]] = None
    error: Optional[str] = None