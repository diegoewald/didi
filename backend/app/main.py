from __future__ import annotations

from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware

from .analyzer import analyze_image
from .schemas import ForensicReport

MAX_UPLOAD_BYTES = 16 * 1024 * 1024
ALLOWED_MIME_TYPES = {"image/jpeg", "image/png", "image/webp"}

app = FastAPI(
    title="DIDI Image Forensics API",
    description="API forense para estimar indícios técnicos de imagem gerada por IA, sem prometer certeza absoluta.",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5173"],
    allow_credentials=True,
    allow_methods=["POST", "GET", "OPTIONS"],
    allow_headers=["*"],
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/analyze", response_model=ForensicReport)
async def analyze(file: UploadFile = File(...)) -> ForensicReport:
    if file.content_type not in ALLOWED_MIME_TYPES:
        raise HTTPException(status_code=415, detail="Envie uma imagem JPG, PNG ou WEBP.")
    raw = await file.read(MAX_UPLOAD_BYTES + 1)
    if len(raw) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="Arquivo maior que 16 MB.")
    if not raw:
        raise HTTPException(status_code=400, detail="Arquivo vazio.")
    try:
        return analyze_image(file.filename or "imagem", file.content_type or "application/octet-stream", raw)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=500, detail="Falha ao analisar a imagem.") from exc
