import os
from typing import List

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field


app = FastAPI(title="ChatAIWeb RAGAS Evaluation Service", version="1.0.0")


class EvaluationSample(BaseModel):
    question: str
    groundTruthAnswer: str
    generatedAnswer: str
    retrievedContexts: List[str] = Field(default_factory=list)


class EvaluationRequest(BaseModel):
    samples: List[EvaluationSample] = Field(min_length=1)


class EvaluationScore(BaseModel):
    faithfulness: float
    answerRelevancy: float
    contextPrecision: float
    contextRecall: float


class EvaluationResponse(BaseModel):
    results: List[EvaluationScore]


@app.get("/health")
def health():
    return {"status": "ok", "evaluator": "ragas"}


@app.post("/evaluate", response_model=EvaluationResponse)
async def evaluate(request: EvaluationRequest):
    try:
        return EvaluationResponse(results=await evaluate_with_ragas(request.samples))
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"ragas evaluation failed: {exc}") from exc


async def evaluate_with_ragas(samples: List[EvaluationSample]) -> List[EvaluationScore]:
    from ragas import EvaluationDataset, evaluate as ragas_evaluate
    from ragas.metrics import Faithfulness, LLMContextPrecisionWithReference, LLMContextRecall, ResponseRelevancy
    from ragas.llms import llm_factory

    api_key = os.getenv("GOOGLE_API_KEY") or os.getenv("GEMINI_API_KEY")
    if not api_key:
        raise RuntimeError("GOOGLE_API_KEY or GEMINI_API_KEY is required for RAGAS Gemini evaluation")

    # RAGAS reads GOOGLE_API_KEY internally for Gemini. Keep both env names usable.
    os.environ["GOOGLE_API_KEY"] = api_key
    llm = llm_factory(os.getenv("RAGAS_GEMINI_MODEL", "gemini-2.0-flash"))

    dataset = EvaluationDataset.from_list([
        {
            "user_input": sample.question,
            "response": sample.generatedAnswer,
            "reference": sample.groundTruthAnswer,
            "retrieved_contexts": sample.retrievedContexts,
        }
        for sample in samples
    ])

    result = ragas_evaluate(
        dataset=dataset,
        metrics=[
            Faithfulness(llm=llm),
            ResponseRelevancy(llm=llm),
            LLMContextPrecisionWithReference(llm=llm),
            LLMContextRecall(llm=llm),
        ],
    )

    rows = result.to_pandas().to_dict("records")
    scores: List[EvaluationScore] = []
    for row in rows:
        scores.append(EvaluationScore(
            faithfulness=float(row.get("faithfulness", 0.0) or 0.0),
            answerRelevancy=float(row.get("answer_relevancy", row.get("response_relevancy", 0.0)) or 0.0),
            contextPrecision=float(row.get("llm_context_precision_with_reference", row.get("context_precision", 0.0)) or 0.0),
            contextRecall=float(row.get("context_recall", row.get("llm_context_recall", 0.0)) or 0.0),
        ))

    return scores


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=int(os.getenv("RAGAS_PORT", "8002")))
