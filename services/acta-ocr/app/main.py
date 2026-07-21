from __future__ import annotations

import io
import re
from typing import Any

from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image
from pydantic import BaseModel, Field

app = FastAPI(title="Acta OCR", version="1.0.0")
_ocr: Any | None = None


class Entry(BaseModel):
    label: str
    value: int = Field(ge=0)
    type: str = "Other"
    confidence: float | None = None


class OcrResponse(BaseModel):
    provider: str = "paddleocr"
    rawText: str
    confidence: float = Field(ge=0, le=1)
    entries: list[Entry]
    errors: list[str]


def get_ocr() -> Any:
    global _ocr
    if _ocr is None:
        from paddleocr import PaddleOCR

        _ocr = PaddleOCR(use_doc_orientation_classify=True, use_doc_unwarping=True, use_textline_orientation=True)
    return _ocr


def classify_label(label: str) -> str:
    normalized = label.casefold()
    if "blanco" in normalized:
        return "Blank"
    if "nulo" in normalized:
        return "Null"
    if "total" in normalized:
        return "Total"
    return "Candidate"


def parse_entries(lines: list[tuple[str, float]]) -> list[Entry]:
    entries: list[Entry] = []
    pattern = re.compile(r"^(?P<label>.+?)\s+(?P<value>\d{1,7})$")
    for text, confidence in lines:
        match = pattern.match(text.strip())
        if not match:
            continue
        entries.append(
            Entry(
                label=match.group("label").strip(),
                value=int(match.group("value")),
                type=classify_label(match.group("label")),
                confidence=round(confidence, 4),
            )
        )
    return entries


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/v1/ocr", response_model=OcrResponse)
async def process(file: UploadFile = File(...)) -> OcrResponse:
    if file.content_type not in {"image/jpeg", "image/png", "image/webp"}:
        raise HTTPException(status_code=415, detail="Only JPG, PNG and WEBP images are supported.")

    payload = await file.read()
    if not payload or len(payload) > 10 * 1024 * 1024:
        raise HTTPException(status_code=413, detail="Image must be between 1 byte and 10 MB.")

    try:
        image = Image.open(io.BytesIO(payload)).convert("RGB")
        result = get_ocr().predict(image)
    except Exception as exc:
        raise HTTPException(status_code=422, detail=f"OCR could not process the image: {exc}") from exc

    lines: list[tuple[str, float]] = []
    for page in result:
        data = page.json if hasattr(page, "json") else page
        if callable(data):
            data = data()
        if isinstance(data, str):
            import json
            data = json.loads(data)
        texts = data.get("rec_texts", []) if isinstance(data, dict) else []
        scores = data.get("rec_scores", []) if isinstance(data, dict) else []
        lines.extend((str(text), float(scores[index]) if index < len(scores) else 0.0) for index, text in enumerate(texts))

    raw_text = "\n".join(text for text, _ in lines)
    confidence = sum(score for _, score in lines) / len(lines) if lines else 0.0
    entries = parse_entries(lines)
    errors: list[str] = []
    if not lines:
        errors.append("No text was detected.")
    if not entries:
        errors.append("No structured vote rows were detected; manual review is required.")

    return OcrResponse(rawText=raw_text, confidence=round(confidence, 4), entries=entries, errors=errors)
