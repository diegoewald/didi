from __future__ import annotations

from typing import Any, Literal

from pydantic import BaseModel, Field

SuspicionCategory = Literal[
    "Inconclusivo",
    "Suspeita leve",
    "Suspeita moderada",
    "Muito provável IA",
    "Evidência técnica forte de IA",
]


class Evidence(BaseModel):
    title: str
    severity: Literal["info", "low", "medium", "high", "critical"]
    description: str
    score_impact: float = Field(ge=-100, le=100)


class AnalysisMaps(BaseModel):
    ela: str
    noise: str
    edges: str
    sharpness: str
    fft: str


class HistogramPoint(BaseModel):
    bin: int
    r: int
    g: int
    b: int


class ForensicReport(BaseModel):
    filename: str
    mime_type: str
    width: int
    height: int
    suspicion_score: float = Field(ge=0, le=100)
    category: SuspicionCategory
    conclusion: str
    evidence: list[Evidence]
    metadata: dict[str, Any]
    jpeg: dict[str, Any]
    compression: dict[str, Any]
    entropy: dict[str, Any]
    texture: dict[str, Any]
    channels: dict[str, Any]
    dct: dict[str, Any]
    objects: dict[str, Any]
    histograms: list[HistogramPoint]
    maps: AnalysisMaps
