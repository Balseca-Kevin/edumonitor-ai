from app.schemas.tools import CursoSchema, CursosAutorizadosResponse

def test_cursos_schema():
    """Valida la creación correcta de esquemas Pydantic."""
    curso = CursoSchema(id=1, fullname="Metodologías Ágiles", shortname="AGIL-4B")
    assert curso.id == 1
    assert curso.shortname == "AGIL-4B"

def test_cursos_response_schema():
    """Valida la respuesta de cursos autorizados."""
    response = CursosAutorizadosResponse(success=True, data=[])
    assert response.success is True
    assert response.data == []