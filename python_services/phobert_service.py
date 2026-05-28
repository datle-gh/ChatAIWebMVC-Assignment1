import os
from functools import lru_cache
from typing import List

import torch
import torch.nn.functional as functional
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from transformers import AutoModel, AutoTokenizer


DEFAULT_MODEL = os.getenv("PHOBERT_MODEL", "vinai/phobert-base")
DEVICE = "cuda" if torch.cuda.is_available() and os.getenv("PHOBERT_DEVICE", "auto") != "cpu" else "cpu"

app = FastAPI(title="ChatAIWeb PhoBERT Embedding Service", version="1.0.0")


class EmbedRequest(BaseModel):
    model: str = Field(default=DEFAULT_MODEL)
    texts: List[str] = Field(min_length=1)


class EmbedResponse(BaseModel):
    model: str
    dimension: int
    embeddings: List[List[float]]


@lru_cache(maxsize=4)
def load_model(model_name: str):
    tokenizer = AutoTokenizer.from_pretrained(model_name, use_fast=False)
    model = AutoModel.from_pretrained(model_name)
    model.to(DEVICE)
    model.eval()
    return tokenizer, model


def mean_pool(last_hidden_state: torch.Tensor, attention_mask: torch.Tensor) -> torch.Tensor:
    mask = attention_mask.unsqueeze(-1).expand(last_hidden_state.size()).float()
    summed = torch.sum(last_hidden_state * mask, dim=1)
    counts = torch.clamp(mask.sum(dim=1), min=1e-9)
    return summed / counts


@app.get("/health")
def health():
    return {"status": "ok", "defaultModel": DEFAULT_MODEL, "device": DEVICE}


@app.post("/embed", response_model=EmbedResponse)
def embed(request: EmbedRequest):
    texts = [text.strip() for text in request.texts if text and text.strip()]
    if not texts:
        raise HTTPException(status_code=400, detail="texts must contain at least one non-empty string")

    try:
        tokenizer, model = load_model(request.model)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"unable to load model {request.model}: {exc}") from exc

    try:
        encoded = tokenizer(
            texts,
            padding=True,
            truncation=True,
            max_length=256,
            return_tensors="pt",
        )
        encoded = {key: value.to(DEVICE) for key, value in encoded.items()}

        with torch.no_grad():
            output = model(**encoded)
            pooled = mean_pool(output.last_hidden_state, encoded["attention_mask"])
            normalized = functional.normalize(pooled, p=2, dim=1)

        vectors = normalized.cpu().tolist()
        return EmbedResponse(
            model=request.model,
            dimension=len(vectors[0]),
            embeddings=vectors,
        )
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"embedding failed: {exc}") from exc


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=int(os.getenv("PHOBERT_PORT", "8001")))
