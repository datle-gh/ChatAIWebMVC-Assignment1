import argparse
import json
import random
from pathlib import Path
from typing import Iterable, List, Tuple

import pyodbc
from sentence_transformers import InputExample, SentenceTransformer, losses
from torch.utils.data import DataLoader


def fetch_training_pairs(connection_string: str, subject_id: int) -> List[Tuple[str, str, int]]:
    query = """
        SELECT q.Question, q.GroundTruthAnswer, c.Content
        FROM EvaluationQuestions q
        JOIN DocumentChunks c ON c.DocumentId IN (
            SELECT Id FROM Documents WHERE SubjectId = q.SubjectId AND Status = 'Indexed'
        )
        WHERE q.SubjectId = ?
    """
    rows: List[Tuple[str, str, str]] = []
    with pyodbc.connect(connection_string) as connection:
        cursor = connection.cursor()
        for question, ground_truth, content in cursor.execute(query, subject_id):
            rows.append((question or "", ground_truth or "", content or ""))

    positives: List[Tuple[str, str, int]] = []
    negatives: List[Tuple[str, str, int]] = []
    for question, ground_truth, content in rows:
        if not question.strip() or not content.strip():
            continue

        combined = f"{question}\n{ground_truth}".lower()
        content_lower = content.lower()
        overlap = len(set(combined.split()) & set(content_lower.split()))
        if overlap >= 3:
            positives.append((question, content, 1))
        else:
            negatives.append((question, content, 0))

    random.shuffle(negatives)
    return positives + negatives[: max(len(positives), 1) * 2]


def load_jsonl(path: Path) -> List[Tuple[str, str, int]]:
    pairs: List[Tuple[str, str, int]] = []
    with path.open("r", encoding="utf-8") as handle:
        for line in handle:
            if not line.strip():
                continue
            row = json.loads(line)
            pairs.append((row["question"], row["chunk"], int(row["label"])))
    return pairs


def save_jsonl(path: Path, rows: Iterable[Tuple[str, str, int]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as handle:
        for question, chunk, label in rows:
            handle.write(json.dumps({
                "question": question,
                "chunk": chunk,
                "label": label,
            }, ensure_ascii=False) + "\n")


def train(base_model: str, rows: List[Tuple[str, str, int]], output_dir: Path, epochs: int, batch_size: int) -> None:
    if not rows:
        raise RuntimeError("No training rows available")

    model = SentenceTransformer(base_model)
    examples = [
        InputExample(texts=[question, chunk], label=float(label))
        for question, chunk, label in rows
    ]
    loader = DataLoader(examples, shuffle=True, batch_size=batch_size)
    train_loss = losses.CosineSimilarityLoss(model)
    model.fit(
        train_objectives=[(loader, train_loss)],
        epochs=epochs,
        warmup_steps=max(1, len(loader) // 10),
        show_progress_bar=True,
    )
    output_dir.mkdir(parents=True, exist_ok=True)
    model.save(str(output_dir))


def main() -> None:
    parser = argparse.ArgumentParser(description="Export and fine-tune a PhoBERT/SentenceTransformer embedding model.")
    parser.add_argument("--connection-string", help="SQL Server ODBC connection string")
    parser.add_argument("--subject-id", type=int, help="Subject id to export training pairs from")
    parser.add_argument("--dataset", type=Path, default=Path("artifacts/embedding_train.jsonl"))
    parser.add_argument("--base-model", default="vinai/phobert-base")
    parser.add_argument("--output-dir", type=Path, default=Path("models/phobert-finetuned"))
    parser.add_argument("--epochs", type=int, default=1)
    parser.add_argument("--batch-size", type=int, default=8)
    parser.add_argument("--export-only", action="store_true")
    args = parser.parse_args()

    if args.connection_string and args.subject_id:
        rows = fetch_training_pairs(args.connection_string, args.subject_id)
        save_jsonl(args.dataset, rows)
    else:
        rows = load_jsonl(args.dataset)

    if args.export_only:
        print(f"Exported {len(rows)} rows to {args.dataset}")
        return

    train(args.base_model, rows, args.output_dir, args.epochs, args.batch_size)
    print(f"Saved fine-tuned embedding model to {args.output_dir}")


if __name__ == "__main__":
    main()
