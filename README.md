# ChatAIWeb

ChatAIWeb la ung dung ASP.NET Core MVC ho tro chatbot RAG cho tai lieu hoc tap tieng Viet. He thong cho phep Admin/Giang vien tai len PDF, DOCX, PPTX, tach noi dung thanh chunks, tao embeddings, truy xuat ngu canh lien quan va sinh cau tra loi co nguon trich dan.

Du an duoc xay theo kien truc 3 lop cho mon PRN222/FPT: `Presentation`, `BusinessLogic`, `DataAccess`, `BusinessObject`.

## Muc Luc

- [Tinh nang chinh](#tinh-nang-chinh)
- [Tech stack](#tech-stack)
- [Yeu cau truoc khi chay](#yeu-cau-truoc-khi-chay)
- [Cai dat nhanh](#cai-dat-nhanh)
- [Cau hinh AI/RAG](#cau-hinh-airag)
- [Chay ung dung](#chay-ung-dung)
- [Tai khoan seed](#tai-khoan-seed)
- [Kien truc du an](#kien-truc-du-an)
- [Database](#database)
- [Python services](#python-services)
- [Lenh thuong dung](#lenh-thuong-dung)
- [Troubleshooting](#troubleshooting)

## Tinh nang chinh

- Dang nhap/dang xuat theo role: `Admin`, `Teacher`, `Student`.
- Quan ly mon hoc va nguoi dung.
- Upload tai lieu hoc tap dinh dang PDF, DOCX, PPTX.
- Trich xuat text tu tai lieu, chia chunk, tao embedding.
- Chatbot RAG tra loi cau hoi dua tren tai lieu da upload.
- Hien thi citation theo document, page/slide, chunk va similarity score.
- Luu lich su hoi thoai theo user/subject.
- Benchmark chat/RAG bang bo cau hoi danh gia.
- Ho tro nhieu embedding model:
  - `bge-m3` qua Ollama.
  - `vinai/phobert-base` qua PhoBERT FastAPI service.
  - model PhoBERT fine-tuned neu co artifact.
- Ho tro Qdrant lam vector store, SQL lam fallback.
- Ho tro RAGAS Python service de cham diem RAG.

## Tech Stack

| Thanh phan | Cong nghe |
| --- | --- |
| Backend web | ASP.NET Core MVC, .NET 8 |
| UI | Razor Views, Bootstrap, jQuery, SignalR |
| Database | SQL Server, Entity Framework Core |
| Auth | Cookie Authentication |
| LLM | Google Gemini, Fake LLM fallback |
| Embedding | Ollama `bge-m3`, PhoBERT FastAPI, Fake embedding fallback |
| Vector search | SQL cosine fallback, Qdrant REST API |
| File parsing | PdfPig, OpenXML |
| Benchmark | RAGAS service, LLM-as-judge fallback |
| Python services | FastAPI, transformers, sentence-transformers, ragas |

## Yeu cau truoc khi chay

Can cai dat:

- .NET SDK 8.0 tro len.
- SQL Server hoac SQL Server Express.
- Visual Studio 2022 hoac VS Code.
- Ollama neu dung embedding `bge-m3`.
- Python 3.10+ neu dung PhoBERT/RAGAS/fine-tuning.
- Qdrant neu muon dung vector store Qdrant.

Kiem tra nhanh:

```powershell
dotnet --version
python --version
ollama --version
```

## Cai dat nhanh

### 1. Clone hoac mo source code

```powershell
cd D:\Study_Research\Knowledge\IT\FPT\kì_7\PRN222\Assignments
```

Neu clone tu Git:

```powershell
git clone <repo-url> ChatAIWeb
cd ChatAIWeb
```

### 2. Restore va build

```powershell
dotnet restore ChatAIWeb.slnx
dotnet build ChatAIWeb.slnx
```

Build thanh cong se thay:

```text
Build succeeded.
```

### 3. Tao database

Mo SQL Server Management Studio hoac Azure Data Studio, sau do chay file:

```text
ChatAIWeb_Database.sql
```

File nay tao database `ChatAIWebDb`, cac bang can thiet va seed du lieu demo.

Neu ban da co database cu, hay chay them script migration Phase 2 neu file ton tai trong project:

```text
Phase2_Database_Migration.sql
```

Ung dung cung co startup initializer de tu them mot so bang/cot Phase 2 neu co the ket noi database.

### 4. Kiem tra connection string

Mo file:

```text
Presentation/appsettings.json
```

Mac dinh:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=true;"
}
```

Neu may ban gap loi encryption cua SQL Server, thu doi thanh:

```json
"DefaultConnection": "Server=localhost;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=false;"
```

Neu dung SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=false;"
```

### 5. Chay ung dung

Bang command line:

```powershell
dotnet run --project Presentation\Presentation.csproj --launch-profile https
```

Hoac mo solution trong Visual Studio va bam Run.

URL mac dinh:

- HTTPS: <https://localhost:7108>
- HTTP: <http://localhost:5039>

## Cau hinh AI/RAG

Cau hinh nam trong:

```text
Presentation/appsettings.json
```

### LLM Gemini

Phan cau hinh:

```json
"Llm": {
  "Provider": "Gemini",
  "Model": "gemini-2.5-flash",
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "Temperature": 0.2,
    "MaxOutputTokens": 1024
  }
}
```

API key nen luu bang User Secrets hoac environment variable, khong commit vao source code.

Vi du voi User Secrets:

```powershell
cd Presentation
dotnet user-secrets set "Llm:Gemini:ApiKey" "<your-gemini-api-key>"
```

### Ollama embedding bge-m3

Pull model:

```powershell
ollama pull bge-m3
```

Warm up embedding model:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:11434/api/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"bge-m3","input":"Day la cau kiem tra warm up embedding.","truncate":true}'
```

Kiem tra model dang load:

```powershell
ollama ps
```

### Embedding models

Mac dinh trong `appsettings.json`:

```json
"Embedding": {
  "Provider": "Ollama",
  "DefaultModel": "bge-m3",
  "DefaultModelKey": "bge-m3",
  "Models": [
    {
      "Key": "bge-m3",
      "Provider": "Ollama",
      "Model": "bge-m3",
      "BaseUrl": "http://localhost:11434",
      "Dimension": 1024,
      "Enabled": true,
      "IncludeInBenchmark": true
    },
    {
      "Key": "phobert-base",
      "Provider": "PhoBert",
      "Model": "vinai/phobert-base",
      "BaseUrl": "http://localhost:8001",
      "Dimension": 768,
      "Enabled": true,
      "IncludeInBenchmark": true
    }
  ]
}
```

Neu chua chay PhoBERT service, co the tam tat model PhoBERT:

```json
"Enabled": false
```

### Qdrant

Cau hinh:

```json
"VectorStore": {
  "Provider": "Qdrant",
  "DualWrite": true
},
"Qdrant": {
  "Host": "localhost",
  "Port": 6333,
  "UseHttps": false,
  "ApiKey": "",
  "CollectionPrefix": "chataiweb"
}
```

Neu chua chay Qdrant, ung dung se log warning va fallback ve SQL vector search.

Chay Qdrant bang Docker:

```powershell
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

Hoac doi provider ve SQL:

```json
"VectorStore": {
  "Provider": "Sql",
  "DualWrite": false
}
```

## Chay ung dung

### Bang Visual Studio

1. Mo `ChatAIWeb.slnx`.
2. Chon startup project la `Presentation`.
3. Chon profile `https`.
4. Bam Run.
5. Mo <https://localhost:7108>.

### Bang terminal

```powershell
dotnet run --project Presentation\Presentation.csproj --launch-profile https
```

Neu gap loi port dang duoc su dung:

```powershell
netstat -ano | Select-String ':7108|:5039'
```

Dung process dang giu port:

```powershell
Stop-Process -Id <PID> -Force
```

## Tai khoan seed

Mat khau seed duoc ghi trong `ChatAIWeb_Database.sql`.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@chataiweb.local` | `Admin@123` |
| Teacher demo | `teacher@chataiweb.local` | `Teacher@123` |
| Student demo | `student@chataiweb.local` | `Student@123` |

Ngoai ra database seed con co nhieu giang vien va sinh vien khac.

## Kien truc du an

```text
ChatAIWeb
├── BusinessObject
│   ├── Entities
│   └── Enums
├── DataAccess
│   ├── ChatAIWebDbContext.cs
│   └── Repositories
├── BusinessLogic
│   ├── DTOs
│   ├── Infrastructure
│   └── Services
├── Presentation
│   ├── Controllers
│   ├── Models
│   ├── Views
│   ├── wwwroot
│   └── Program.cs
└── python_services
    ├── phobert_service.py
    ├── ragas_service.py
    ├── finetune_embedding.py
    └── requirements.txt
```

### Presentation

Chua ASP.NET Core MVC UI:

- Controllers.
- Razor views.
- ViewModels.
- Cookie authentication.
- Dependency injection trong `Program.cs`.
- SignalR upload progress hub.

Controllers nen mong, chi goi service o `BusinessLogic`.

### BusinessLogic

Chua business workflow:

- Upload va index tai lieu.
- Text extraction orchestration.
- Chunking.
- Embedding generation.
- Vector search.
- Chatbot RAG.
- Citation mapping.
- RAGAS benchmark.
- System settings.

### DataAccess

Chua EF Core DbContext va repositories:

- Query database.
- CRUD.
- Include navigation data.
- Luu chat, document, chunk, citation, benchmark result.

### BusinessObject

Chua entity va enum dung chung:

- `User`
- `Subject`
- `SubjectEnrollment`
- `Document`
- `DocumentChunk`
- `DocumentChunkEmbedding`
- `ChatSession`
- `ChatMessage`
- `Citation`
- `EvaluationQuestion`
- `RagasBenchmarkResult`

## RAG Pipeline

Luon chay theo luong:

```text
Upload document
→ Save file
→ Extract text
→ Split into chunks
→ Generate embeddings
→ Save SQL metadata
→ Upsert Qdrant if available
→ Mark document as Indexed

Student question
→ Generate question embedding
→ Search vector store
→ Filter by similarity threshold
→ Build prompt with retrieved chunks
→ Call LLM
→ Save chat message
→ Save citations
→ Return answer + sources
```

## Database

Database chinh: `ChatAIWebDb`.

Bang quan trong:

| Table | Muc dich |
| --- | --- |
| `Users` | Tai khoan va role |
| `Subjects` | Mon hoc |
| `SubjectEnrollments` | Gan user vao mon hoc |
| `Documents` | Metadata tai lieu upload |
| `DocumentChunks` | Chunk text tu tai lieu |
| `DocumentChunkEmbeddings` | Embedding theo tung chunk/model |
| `ChatSessions` | Phien chat |
| `ChatMessages` | Cau hoi/cau tra loi |
| `Citations` | Nguon trich dan cua cau tra loi |
| `EvaluationQuestions` | Bo cau hoi benchmark |
| `RagasBenchmarkResults` | Ket qua benchmark |

### Tao lai database tu dau

Can than: thao tac nay se xoa database cu neu script co `DROP DATABASE`/`DROP TABLE`.

1. Mo `ChatAIWeb_Database.sql`.
2. Chay bang SSMS/Azure Data Studio.
3. Kiem tra database `ChatAIWebDb`.
4. Run app.

### Database cu sau Phase 2

Neu database cu thieu cot:

- `RunId`
- `VectorStore`
- `RetrievedContextsJson`
- bang `DocumentChunkEmbeddings`

Hay chay:

```text
Phase2_Database_Migration.sql
```

## Python services

Python services la tuy chon, nhung can neu muon dung PhoBERT/RAGAS/fine-tuning.

### Cai dependency

```powershell
cd python_services
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### Chay PhoBERT service

```powershell
python phobert_service.py
```

Mac dinh:

- Health: <http://localhost:8001/health>
- Embed: `POST http://localhost:8001/embed`

Test:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:8001/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"vinai/phobert-base","texts":["Xin chao, day la cau kiem tra."]}'
```

### Chay RAGAS service

Can Gemini API key:

```powershell
$env:GOOGLE_API_KEY="<your-gemini-api-key>"
python ragas_service.py
```

Mac dinh:

- Health: <http://localhost:8002/health>
- Evaluate: `POST http://localhost:8002/evaluate`

Neu RAGAS service khong chay, .NET service se fallback ve LLM-as-judge.

### Fine-tune embedding

Export dataset tu SQL:

```powershell
python finetune_embedding.py `
  --connection-string "Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=ChatAIWebDb;Trusted_Connection=yes;TrustServerCertificate=yes;" `
  --subject-id 1 `
  --dataset artifacts/embedding_train.jsonl `
  --export-only
```

Train model:

```powershell
python finetune_embedding.py `
  --dataset artifacts/embedding_train.jsonl `
  --base-model vinai/phobert-base `
  --output-dir models/phobert-finetuned `
  --epochs 1 `
  --batch-size 8
```

Sau khi co model fine-tuned, bat model trong `appsettings.json`:

```json
{
  "Key": "phobert-finetuned",
  "Provider": "PhoBert",
  "Model": "models/phobert-finetuned",
  "Enabled": true
}
```

## Lenh thuong dung

| Lenh | Mo ta |
| --- | --- |
| `dotnet restore ChatAIWeb.slnx` | Restore NuGet packages |
| `dotnet build ChatAIWeb.slnx` | Build toan bo solution |
| `dotnet run --project Presentation\Presentation.csproj --launch-profile https` | Chay web app |
| `ollama pull bge-m3` | Tai embedding model |
| `ollama ps` | Kiem tra model Ollama dang load |
| `python python_services\phobert_service.py` | Chay PhoBERT embedding service |
| `python python_services\ragas_service.py` | Chay RAGAS service |

## Troubleshooting

### 1. `Failed to bind to address ... address already in use`

Port `7108` hoac `5039` dang bi process khac giu.

Kiem tra:

```powershell
netstat -ano | Select-String ':7108|:5039'
```

Dung process:

```powershell
Stop-Process -Id <PID> -Force
```

### 2. SQL Server encryption error

Loi thuong gap:

```text
The instance of SQL Server you attempted to connect to requires encryption but this machine does not support it.
```

Thu doi connection string:

```json
"Encrypt=false;TrustServerCertificate=true;"
```

### 3. RAGAS page bao invalid column

Neu gap:

```text
Invalid column name 'RunId'
Invalid column name 'VectorStore'
Invalid column name 'RetrievedContextsJson'
```

Hay chay:

```text
Phase2_Database_Migration.sql
```

Hoac restart app de startup initializer co co hoi tu cap nhat schema.

### 4. Ollama timeout khi backfill embedding

Log co dang:

```text
Unable to backfill embedding ... TaskCanceledException
```

Nguyen nhan thuong gap:

- Ollama chua chay.
- Model `bge-m3` chua pull.
- Model dang load lan dau nen cham.
- Chunk qua dai hoac may thieu RAM/CPU/GPU.

Xu ly:

```powershell
ollama pull bge-m3
ollama ps
```

Warm up:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:11434/api/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"bge-m3","input":"Day la cau kiem tra warm up embedding.","truncate":true}'
```

### 5. Qdrant khong chay

Neu Qdrant khong chay, app se fallback ve SQL search. Neu muon tat Qdrant:

```json
"VectorStore": {
  "Provider": "Sql",
  "DualWrite": false
}
```

### 6. Trang upload chi hien mot so mon hoc

Trang upload lay danh sach mon theo rule trong `DocumentService.GetUploadSubjectOptionsAsync`.

Trong ban hien tai:

- Admin thay tat ca mon.
- Teacher thay tat ca mon.
- Student khong duoc upload.

Neu muon gioi han giang vien chi upload mon duoc phan cong, sua lai service de dung `GetUploadableByTeacherAsync`.

### 7. Build bi lock file DLL

Neu build bao:

```text
The file is locked by: Microsoft Visual Studio, Presentation
```

Dung app dang chay trong Visual Studio roi build lai.

## Ghi chu bao mat

- Khong commit Gemini API key, OpenAI key, Qdrant API key.
- Nen luu secret bang User Secrets trong development.
- Nen dung environment variables hoac secret manager trong production.

## Goi y luong demo

1. Dang nhap Admin hoac Teacher.
2. Vao `Kho tai lieu`.
3. Chon `Them tai lieu moi`.
4. Upload PDF/DOCX/PPTX vao mot mon hoc.
5. Cho qua trinh extract/chunk/embed/index hoan tat.
6. Vao chat theo mon hoc.
7. Dat cau hoi lien quan den tai lieu vua upload.
8. Kiem tra cau tra loi va citations.
9. Vao trang benchmark RAGAS de chay danh gia nhieu embedding model.

## License

Du an phuc vu muc dich hoc tap va demo mon hoc. Hay cap nhat license neu can cong khai hoac trien khai production.
