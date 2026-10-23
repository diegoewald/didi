# DIDI Forensics

Sistema web de análise forense de imagens para estimar, de forma probabilística, indícios de geração por IA. A aplicação combina um front-end React/Next.js responsivo com um backend Python/FastAPI que extrai metadados, compressão, ruído, bordas, nitidez, espectro FFT, DCT, entropia, textura e correlações cromáticas.

> O relatório nunca afirma certeza absoluta. As categorias são: Inconclusivo, Suspeita leve, Suspeita moderada, Muito provável IA e Evidência técnica forte de IA.

## Front-end

```bash
npm install
npm run dev
```

A URL do backend pode ser configurada com `NEXT_PUBLIC_FORENSICS_API_URL`; por padrão usa `http://localhost:8000`.

## Backend

```bash
cd backend
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Endpoint principal: `POST /api/analyze` com campo multipart `file` (`image/jpeg`, `image/png` ou `image/webp`, até 16 MB).

## Recursos técnicos implementados

- EXIF, XMP, IPTC e ICC.
- Tabelas de quantização JPEG e estimativa de qualidade.
- Error Level Analysis e indício de dupla compressão.
- Mapas de nitidez, bordas, ruído e FFT.
- Distribuição DCT, entropia global/local, LBP e GLCM.
- Correlação RGB e ruído em YCbCr.
- Heurísticas de inconsistência de foco, PRNU aproximado e textura repetitiva.
- Campos preparados para OCR e landmarks opcionais.
