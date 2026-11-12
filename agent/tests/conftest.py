import sys
from pathlib import Path

# Añade la carpeta 'agent/' al sys.path para que 'app' sea visible
agent_dir = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(agent_dir))