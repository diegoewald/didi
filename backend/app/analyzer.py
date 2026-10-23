from __future__ import annotations

import base64
import io
import math
import re
from dataclasses import dataclass
from typing import Any

import cv2
import numpy as np
from PIL import ExifTags, Image, ImageChops, ImageStat, IptcImagePlugin
from scipy.fftpack import dct
from scipy.stats import kurtosis, skew
from skimage.feature import graycomatrix, graycoprops, local_binary_pattern
from skimage.measure import shannon_entropy

from .schemas import Evidence, ForensicReport, HistogramPoint, AnalysisMaps

MAX_SIDE = 1600
ALLOWED_FORMATS = {"JPEG", "PNG", "WEBP"}


@dataclass
class ImageContext:
    filename: str
    mime_type: str
    raw: bytes
    pil: Image.Image
    rgb: np.ndarray
    gray: np.ndarray


def _data_url(array: np.ndarray, cmap: int | None = None) -> str:
    arr = array.astype(np.float32)
    if arr.ndim == 2:
        arr = cv2.normalize(arr, None, 0, 255, cv2.NORM_MINMAX).astype(np.uint8)
        if cmap is not None:
            arr = cv2.applyColorMap(arr, cmap)
        else:
            arr = cv2.cvtColor(arr, cv2.COLOR_GRAY2BGR)
    else:
        arr = np.clip(arr, 0, 255).astype(np.uint8)
        arr = cv2.cvtColor(arr, cv2.COLOR_RGB2BGR)
    ok, buf = cv2.imencode(".webp", arr, [int(cv2.IMWRITE_WEBP_QUALITY), 82])
    if not ok:
        raise ValueError("Não foi possível renderizar mapa de análise.")
    return "data:image/webp;base64," + base64.b64encode(buf.tobytes()).decode("ascii")


def _safe_float(value: Any) -> float:
    if value is None or (isinstance(value, float) and not math.isfinite(value)):
        return 0.0
    return float(value)


def load_image(filename: str, mime_type: str, raw: bytes) -> ImageContext:
    with Image.open(io.BytesIO(raw)) as img:
        if img.format not in ALLOWED_FORMATS:
            raise ValueError("Formato não suportado. Envie JPG, PNG ou WEBP.")
        pil = img.copy()
    pil.thumbnail((MAX_SIDE, MAX_SIDE), Image.Resampling.LANCZOS)
    rgb_img = pil.convert("RGB")
    rgb = np.asarray(rgb_img)
    gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
    return ImageContext(filename=filename, mime_type=mime_type, raw=raw, pil=pil, rgb=rgb, gray=gray)


def extract_metadata(ctx: ImageContext) -> dict[str, Any]:
    exif = {}
    try:
        exif_obj = ctx.pil.getexif()
        exif = {ExifTags.TAGS.get(k, str(k)): str(v) for k, v in exif_obj.items()} if exif_obj else {}
    except Exception:
        exif = {"error": "EXIF ilegível"}

    raw_text = ctx.raw[: min(len(ctx.raw), 4_000_000)]
    xmp = {}
    match = re.search(rb"<x:xmpmeta[\s\S]*?</x:xmpmeta>", raw_text)
    if match:
        xmp = {"present": True, "bytes": len(match.group(0))}
    iptc = {}
    try:
        iptc_data = IptcImagePlugin.getiptcinfo(ctx.pil) or {}
        iptc = {str(k): str(v) for k, v in iptc_data.items()}
    except Exception:
        iptc = {}

    icc = ctx.pil.info.get("icc_profile")
    software = exif.get("Software") or exif.get("ProcessingSoftware") or ""
    return {
        "format": ctx.pil.format,
        "mode": ctx.pil.mode,
        "exif": exif,
        "xmp": xmp or {"present": False},
        "iptc": iptc,
        "icc": {"present": bool(icc), "bytes": len(icc) if icc else 0},
        "software_hint": software,
        "has_camera_make_model": bool(exif.get("Make") or exif.get("Model")),
    }


def jpeg_quantization(ctx: ImageContext) -> dict[str, Any]:
    try:
        with Image.open(io.BytesIO(ctx.raw)) as img:
            qtables = getattr(img, "quantization", None) or {}
            return {
                "is_jpeg": img.format == "JPEG",
                "tables": {str(k): v for k, v in qtables.items()},
                "table_count": len(qtables),
                "quality_estimate": _estimate_jpeg_quality(qtables),
            }
    except Exception:
        return {"is_jpeg": False, "tables": {}, "table_count": 0, "quality_estimate": None}


def _estimate_jpeg_quality(qtables: dict[int, list[int]]) -> int | None:
    if not qtables:
        return None
    avg = float(np.mean([np.mean(v) for v in qtables.values()]))
    return int(np.clip(115 - avg * 1.25, 1, 100))


def compression_and_ela(ctx: ImageContext) -> tuple[dict[str, Any], np.ndarray]:
    source = ctx.pil.convert("RGB")
    buffer = io.BytesIO()
    source.save(buffer, "JPEG", quality=90)
    recompressed = Image.open(io.BytesIO(buffer.getvalue())).convert("RGB")
    diff = ImageChops.difference(source, recompressed)
    stat = ImageStat.Stat(diff)
    ela = np.asarray(diff)
    ela_gray = cv2.cvtColor(ela, cv2.COLOR_RGB2GRAY)
    block = _block_grid(ela_gray, 32, np.mean)
    variance = float(np.var(block)) if block.size else 0.0
    return {
        "ela_mean": round(float(np.mean(stat.mean)), 4),
        "ela_max": int(np.max(ela_gray)),
        "ela_block_variance": round(variance, 4),
        "double_compression_hint": variance > 8.0 and float(np.mean(ela_gray)) > 1.5,
        "description": "Regiões com ELA muito diferente podem indicar recompressão localizada ou edição composta.",
    }, ela_gray


def _block_grid(gray: np.ndarray, size: int, fn: Any) -> np.ndarray:
    h, w = gray.shape
    values = []
    for y in range(0, h - size + 1, size):
        row = []
        for x in range(0, w - size + 1, size):
            row.append(fn(gray[y : y + size, x : x + size]))
        if row:
            values.append(row)
    return np.asarray(values, dtype=np.float32)


def feature_maps(ctx: ImageContext) -> dict[str, Any]:
    gray = ctx.gray
    lap = cv2.Laplacian(gray, cv2.CV_64F)
    sharpness = _block_grid(np.abs(lap).astype(np.float32), 24, np.var)
    sobel_x = cv2.Sobel(gray, cv2.CV_32F, 1, 0)
    sobel_y = cv2.Sobel(gray, cv2.CV_32F, 0, 1)
    sobel = cv2.magnitude(sobel_x, sobel_y)
    canny = cv2.Canny(gray, 80, 160)
    denoised = cv2.fastNlMeansDenoisingColored(cv2.cvtColor(ctx.rgb, cv2.COLOR_RGB2BGR), None, 7, 7, 7, 21)
    denoised_rgb = cv2.cvtColor(denoised, cv2.COLOR_BGR2RGB)
    noise = np.abs(ctx.rgb.astype(np.int16) - denoised_rgb.astype(np.int16)).mean(axis=2).astype(np.float32)
    spectrum = np.log1p(np.abs(np.fft.fftshift(np.fft.fft2(gray))))
    return {
        "sharpness_map": sharpness,
        "sharpness_cv": _safe_float(np.std(sharpness) / (np.mean(sharpness) + 1e-6)),
        "edge_density": _safe_float(np.mean(canny > 0)),
        "sobel": sobel,
        "canny": canny,
        "noise": noise,
        "noise_mean": _safe_float(np.mean(noise)),
        "noise_cv": _safe_float(np.std(noise) / (np.mean(noise) + 1e-6)),
        "fft": spectrum,
        "fft_peak_ratio": _safe_float(np.max(spectrum) / (np.mean(spectrum) + 1e-6)),
    }


def entropy_texture(ctx: ImageContext) -> tuple[dict[str, Any], dict[str, Any]]:
    gray = ctx.gray
    local = _block_grid(gray, 32, shannon_entropy)
    lbp = local_binary_pattern(gray, P=8, R=1, method="uniform")
    hist, _ = np.histogram(lbp.ravel(), bins=np.arange(0, 11), density=True)
    reduced = (gray / 32).astype(np.uint8)
    glcm = graycomatrix(reduced, distances=[1, 3], angles=[0, np.pi / 4, np.pi / 2], levels=8, symmetric=True, normed=True)
    texture = {
        "lbp_uniformity": round(float(np.max(hist)), 5),
        "lbp_entropy": round(float(shannon_entropy(hist + 1e-9)), 5),
        "glcm_contrast": round(float(graycoprops(glcm, "contrast").mean()), 5),
        "glcm_homogeneity": round(float(graycoprops(glcm, "homogeneity").mean()), 5),
        "glcm_correlation": round(float(graycoprops(glcm, "correlation").mean()), 5),
    }
    entropy = {
        "global": round(float(shannon_entropy(gray)), 5),
        "local_mean": round(float(np.mean(local)), 5),
        "local_std": round(float(np.std(local)), 5),
        "block_size": 32,
    }
    return entropy, texture


def channel_dct(ctx: ImageContext) -> tuple[dict[str, Any], dict[str, Any]]:
    rgb = ctx.rgb.astype(np.float32)
    r, g, b = [rgb[:, :, i].ravel() for i in range(3)]
    ycc = cv2.cvtColor(ctx.rgb, cv2.COLOR_RGB2YCrCb).astype(np.float32)
    y_noise = cv2.Laplacian(ycc[:, :, 0], cv2.CV_32F).std()
    cb_noise = cv2.Laplacian(ycc[:, :, 2], cv2.CV_32F).std()
    cr_noise = cv2.Laplacian(ycc[:, :, 1], cv2.CV_32F).std()
    coeffs = []
    gray = ctx.gray.astype(np.float32) - 128
    for yy in range(0, gray.shape[0] - 8, 8):
        for xx in range(0, gray.shape[1] - 8, 8):
            block = dct(dct(gray[yy : yy + 8, xx : xx + 8], axis=0, norm="ortho"), axis=1, norm="ortho")
            coeffs.extend(block[1:, 1:].ravel())
    coeffs_np = np.asarray(coeffs) if coeffs else np.asarray([0.0])
    channels = {
        "rgb_correlation": {
            "rg": round(float(np.corrcoef(r, g)[0, 1]), 5),
            "rb": round(float(np.corrcoef(r, b)[0, 1]), 5),
            "gb": round(float(np.corrcoef(g, b)[0, 1]), 5),
        },
        "ycbcr_noise": {
            "luminance": round(float(y_noise), 5),
            "cb": round(float(cb_noise), 5),
            "cr": round(float(cr_noise), 5),
            "chroma_luma_ratio": round(float((cb_noise + cr_noise) / (2 * y_noise + 1e-6)), 5),
        },
    }
    dct_stats = {
        "mean_abs": round(float(np.mean(np.abs(coeffs_np))), 5),
        "zero_ratio": round(float(np.mean(np.abs(coeffs_np) < 1e-3)), 5),
        "skewness": round(_safe_float(skew(coeffs_np)), 5),
        "kurtosis": round(_safe_float(kurtosis(coeffs_np)), 5),
    }
    return channels, dct_stats


def prnu_focus_repeat(ctx: ImageContext, maps: dict[str, Any]) -> dict[str, Any]:
    noise = maps["noise"]
    autocorr = cv2.matchTemplate(noise.astype(np.float32), noise.astype(np.float32), cv2.TM_CCORR_NORMED)
    template_periodicity = float(np.max(autocorr)) if autocorr.size else 0.0
    focus_inconsistency = float(maps["sharpness_cv"])
    return {
        "prnu_estimate": {
            "noise_residual_mean": round(float(np.mean(noise)), 5),
            "spatial_uniformity": round(float(1 / (1 + maps["noise_cv"])), 5),
            "note": "Estimativa aproximada; PRNU confiável exige múltiplas imagens do mesmo sensor.",
        },
        "focus_depth_inconsistency": round(focus_inconsistency, 5),
        "repetitive_texture_hint": template_periodicity > 0.92 and float(np.std(noise)) < 8,
        "ocr": {"available": False, "text": "OCR requer binário Tesseract no ambiente de execução."},
        "faces_hands_landmarks": {"enabled": False, "note": "Endpoint preparado para integração opcional com MediaPipe/face detectors."},
    }


def histograms(ctx: ImageContext) -> list[HistogramPoint]:
    chans = [np.histogram(ctx.rgb[:, :, i], bins=64, range=(0, 256))[0] for i in range(3)]
    return [HistogramPoint(bin=i * 4, r=int(chans[0][i]), g=int(chans[1][i]), b=int(chans[2][i])) for i in range(64)]


def score_report(metadata: dict[str, Any], jpeg: dict[str, Any], compression: dict[str, Any], maps: dict[str, Any], entropy: dict[str, Any], texture: dict[str, Any], channels: dict[str, Any], objects: dict[str, Any]) -> tuple[float, str, str, list[Evidence]]:
    evidence: list[Evidence] = []

    def add(title: str, severity: str, description: str, impact: float) -> None:
        evidence.append(Evidence(title=title, severity=severity, description=description, score_impact=impact))

    if not metadata.get("has_camera_make_model"):
        add("Ausência de câmera nos metadados", "medium", "Não há Make/Model EXIF; isso é comum em imagens sintéticas, exportadas ou removidas por redes sociais.", 14)
    if metadata.get("xmp", {}).get("present") and re.search("ai|stable|midjourney|dall|comfy|firefly", str(metadata).lower()):
        add("Assinatura de ferramenta generativa", "critical", "Metadados contêm termos associados a fluxos de geração por IA.", 36)
    if jpeg.get("is_jpeg") and jpeg.get("table_count", 0) == 0:
        add("JPEG sem tabelas esperadas", "medium", "Arquivo JPEG não expôs tabelas de quantização pelo parser.", 10)
    if compression.get("double_compression_hint"):
        add("Possível dupla compressão", "medium", "A variação regional de ELA sugere múltiplas etapas de recompressão ou composição.", 12)
    if maps["noise_cv"] < 0.35:
        add("Ruído residual muito uniforme", "medium", "Texturas geradas tendem a suavizar ou homogeneizar ruído de sensor.", 12)
    if maps["fft_peak_ratio"] > 30:
        add("Picos anômalos no espectro FFT", "low", "O espectro apresenta concentração periódica que pode indicar padrões sintetizados ou reamostragem.", 7)
    if entropy["local_std"] < 0.35:
        add("Entropia local homogênea", "low", "Baixa variação de entropia entre blocos pode apontar renderização uniforme.", 6)
    if texture["glcm_homogeneity"] > 0.72 and texture["lbp_entropy"] < 2.0:
        add("Textura excessivamente regular", "medium", "LBP/GLCM indicam padrões locais pouco naturais para uma fotografia complexa.", 10)
    corr = channels["rgb_correlation"]
    if min(corr.values()) > 0.96:
        add("Canais RGB altamente correlacionados", "low", "Correlação extrema entre canais pode ocorrer em imagens renderizadas ou muito processadas.", 5)
    if objects["focus_depth_inconsistency"] > 1.4:
        add("Inconsistência de foco", "medium", "O mapa de nitidez varia fortemente entre regiões e pode sugerir composição ou profundidade artificial.", 9)
    if objects["repetitive_texture_hint"]:
        add("Padrões repetitivos de textura", "medium", "A análise residual encontrou indícios de repetição espacial.", 8)

    score = float(np.clip(18 + sum(e.score_impact for e in evidence), 0, 100))
    if score < 25:
        category = "Inconclusivo"
    elif score < 45:
        category = "Suspeita leve"
    elif score < 65:
        category = "Suspeita moderada"
    elif score < 85:
        category = "Muito provável IA"
    else:
        category = "Evidência técnica forte de IA"
    conclusion = f"Resultado {category.lower()} ({score:.1f}/100). Esta é uma estimativa probabilística baseada em artefatos técnicos; não constitui prova absoluta de origem por IA."
    return round(score, 1), category, conclusion, evidence


def analyze_image(filename: str, mime_type: str, raw: bytes) -> ForensicReport:
    ctx = load_image(filename, mime_type, raw)
    metadata = extract_metadata(ctx)
    jpeg = jpeg_quantization(ctx)
    compression, ela = compression_and_ela(ctx)
    maps = feature_maps(ctx)
    entropy, texture = entropy_texture(ctx)
    channels, dct_stats = channel_dct(ctx)
    objects = prnu_focus_repeat(ctx, maps)
    score, category, conclusion, evidence = score_report(metadata, jpeg, compression, maps, entropy, texture, channels, objects)
    edge_map = np.maximum(cv2.normalize(maps["sobel"], None, 0, 255, cv2.NORM_MINMAX), maps["canny"]).astype(np.uint8)
    rendered_maps = AnalysisMaps(
        ela=_data_url(ela, cv2.COLORMAP_INFERNO),
        noise=_data_url(maps["noise"], cv2.COLORMAP_TURBO),
        edges=_data_url(edge_map, cv2.COLORMAP_BONE),
        sharpness=_data_url(maps["sharpness_map"], cv2.COLORMAP_VIRIDIS),
        fft=_data_url(maps["fft"], cv2.COLORMAP_MAGMA),
    )
    return ForensicReport(
        filename=filename,
        mime_type=mime_type,
        width=int(ctx.rgb.shape[1]),
        height=int(ctx.rgb.shape[0]),
        suspicion_score=score,
        category=category,
        conclusion=conclusion,
        evidence=evidence,
        metadata=metadata,
        jpeg=jpeg,
        compression=compression,
        entropy=entropy,
        texture=texture,
        channels=channels,
        dct=dct_stats,
        objects=objects,
        histograms=histograms(ctx),
        maps=rendered_maps,
    )
