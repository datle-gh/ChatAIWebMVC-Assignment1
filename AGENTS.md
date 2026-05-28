# AGENTS.md — ChatAIWeb Project Instructions

## 1. Project Overview

This repository is an ASP.NET Core MVC project named **ChatAIWeb**.

The system is a Vietnamese RAG-based chatbot that allows students to ask questions based on uploaded course documents.

Main goals:

- Allow users to upload learning documents such as PDF, DOCX, and PPTX.
- Extract text from uploaded documents.
- Split extracted text into chunks.
- Generate embeddings for chunks.
- Store document metadata, chunks, chat sessions, and citations.
- Let students ask questions in Vietnamese.
- Retrieve relevant chunks using semantic search.
- Generate answers using an LLM.
- Show answers with citations from uploaded documents.
- Support future RAGAS benchmarking and model comparison.

The project must follow a strict **3-layer architecture**.

---

## 2. Solution Structure

The solution contains 4 projects:

```text
ChatAIWeb
├── BusinessLogic
│   ├── DTOs
│   │   ├── Requests
│   │   ├── Responses
│   ├── Infrastructure
│   └── Services
│
├── BusinessObject
│   ├── Entities
│   └── Enums
│
├── DataAccess
│   └── Repositories
│
└── Presentation
    ├── Controllers
    ├── Models
    ├── Views
    ├── wwwroot
    ├── appsettings.json
    └── Program.cs
```

Layer meaning:

```text
Presentation   = ASP.NET Core MVC UI layer
BusinessLogic  = business rules, orchestration, RAG pipeline
DataAccess     = EF Core, database access, repositories
BusinessObject = shared entities, enums, and common domain models
```

---

## 3. Architecture Rules

Codex must strictly follow the architecture below.

### 3.1. Presentation Layer

Project: `Presentation`

Allowed responsibilities:

- MVC Controllers
- Razor Views
- ViewModels and input models
- UI validation
- Routing
- Dependency injection setup in `Program.cs`
- Reading configuration from `appsettings.json`

Not allowed:

- Do not query EF Core directly in controllers.
- Do not access the database directly.
- Do not implement document chunking logic.
- Do not implement embedding logic.
- Do not implement LLM API logic.
- Do not put business rules inside controllers.

Controllers must be thin and only call services from `BusinessLogic`.

Correct pattern:

```csharp
public class ChatController : Controller
{
    private readonly IChatbotService _chatbotService;

    public ChatController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost]
    public async Task<IActionResult> Ask(ChatRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _chatbotService.AskAsync(model);
        return Json(result);
    }
}
```

---

### 3.2. BusinessLogic Layer

Project: `BusinessLogic`

Allowed responsibilities:

- Business rules
- Upload workflow orchestration
- Document processing workflow
- Text extraction orchestration
- Chunking strategy
- Embedding orchestration
- RAG retrieval workflow
- Prompt construction
- LLM response handling
- Citation handling
- Chat history workflow
- Benchmark workflow

This layer may call:

- DataAccess repositories
- Infrastructure services
- External API wrappers

This layer must not contain:

- MVC View code
- Razor code
- Direct UI concerns
- Direct EF Core queries when a repository already exists

Recommended services:

```text
Services/
├── IDocumentService.cs
├── DocumentService.cs
├── IChunkingService.cs
├── ChunkingService.cs
├── IEmbeddingService.cs
├── EmbeddingService.cs
├── IVectorSearchService.cs
├── VectorSearchService.cs
├── IChatbotService.cs
├── ChatbotService.cs
├── ICitationService.cs
├── CitationService.cs
├── IBenchmarkService.cs
└── BenchmarkService.cs

Infrastructure/
├── IFileStorageService.cs
├── LocalFileStorageService.cs
├── ITextExtractionService.cs
├── PdfTextExtractionService.cs
├── DocxTextExtractionService.cs
├── PptxTextExtractionService.cs
├── ILlmService.cs
├── OpenAiLlmService.cs
├── GeminiLlmService.cs
├── IQdrantVectorService.cs
└── QdrantVectorService.cs
```

---

### 3.3. DataAccess Layer

Project: `DataAccess`

Allowed responsibilities:

- EF Core `DbContext`
- Repository interfaces and implementations
- Database queries
- CRUD operations
- Transactions
- Persistence logic

Not allowed:

- Do not write MVC code.
- Do not write LLM code.
- Do not write document chunking logic.
- Do not write business decision logic.

Recommended structure:

```text
Repositories/
├── IGenericRepository.cs
├── GenericRepository.cs
├── IUserRepository.cs
├── UserRepository.cs
├── ISubjectRepository.cs
├── SubjectRepository.cs
├── IDocumentRepository.cs
├── DocumentRepository.cs
├── IDocumentChunkRepository.cs
├── DocumentChunkRepository.cs
├── IChatRepository.cs
├── ChatRepository.cs
├── ICitationRepository.cs
└── CitationRepository.cs
```

All database operations should be asynchronous.

Use:

```csharp
await _context.SaveChangesAsync();
```

Avoid:

```csharp
_context.SaveChanges();
.Result
.Wait()
```

---

### 3.4. BusinessObject Layer

Project: `BusinessObject`

Allowed responsibilities:

- Entity classes
- Enums
- Shared constants
- Simple DTOs if needed

Not allowed:

- Do not write EF query logic.
- Do not write service logic.
- Do not write controller logic.
- Do not write external API logic.

Recommended structure:

```text
Entities/
├── User.cs
├── Subject.cs
├── SubjectEnrollment.cs
├── Document.cs
├── DocumentChunk.cs
├── ChatSession.cs
├── ChatMessage.cs
├── Citation.cs
├── EvaluationQuestion.cs
└── RagasBenchmarkResult.cs

Enums/
├── UserRole.cs
├── DocumentStatus.cs
├── ChatRole.cs
├── FileType.cs
└── BenchmarkMetric.cs
```

---

## 4. Dependency Rules

Allowed dependency direction:

```text
Presentation
→ BusinessLogic
→ DataAccess
→ BusinessObject
```

Also allowed:

```text
Presentation → BusinessObject
BusinessLogic → BusinessObject
DataAccess → BusinessObject
```

Forbidden dependency direction:

```text
BusinessObject → DataAccess
BusinessObject → BusinessLogic
BusinessObject → Presentation

DataAccess → BusinessLogic
DataAccess → Presentation

BusinessLogic → Presentation
```

Do not create reverse dependencies.

---

## 5. Database Rules

The SQL Server database is named:

```text
ChatAIWebDb
```

Main tables:

- `Users`
- `Subjects`
- `SubjectEnrollments`
- `Documents`
- `DocumentChunks`
- `ChatSessions`
- `ChatMessages`
- `Citations`
- `EvaluationQuestions`
- `RagasBenchmarkResults`

Use EF Core with SQL Server.

The connection string must be placed in `Presentation/appsettings.json`.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Do not hard-code connection strings inside repositories or services.

---

## 6. RAG Pipeline Rules

The expected RAG flow is:

```text
User question
→ Generate question embedding
→ Search top-k relevant chunks
→ Build prompt with retrieved chunks
→ Call LLM
→ Return Vietnamese answer
→ Attach citations
→ Save chat history
```

When implementing chatbot logic, follow this workflow:

```text
ChatController
→ IChatbotService
→ IEmbeddingService
→ IVectorSearchService
→ ILlmService
→ IChatRepository
→ ICitationRepository
```

Answers must:

- Be in Vietnamese by default.
- Use only retrieved document context when the question is document-specific.
- Clearly say when the answer cannot be found in the uploaded documents.
- Include citations when source chunks are available.

Example answer format:

```text
Theo tài liệu đã tải lên, ...

Nguồn tham khảo:
[1] Database_Lecture_01.pdf, trang 5
[2] Normalization_Slides.pptx, slide 12
```

---

## 7. Document Processing Rules

Document upload flow:

```text
Upload file
→ Validate file type
→ Save file
→ Create Document record
→ Extract text
→ Split into chunks
→ Generate embeddings
→ Save chunks and vector IDs
→ Mark document as Indexed
```

Supported file types:

- PDF
- DOCX
- PPTX

At the beginning, implement PDF first if full support is too much.

Document status values:

```text
Uploaded
Processing
Indexed
Failed
```

Each chunk should store:

- `DocumentId`
- `ChunkIndex`
- `Content`
- `PageNumber` or `SlideNumber`
- `TokenCount`
- `VectorId`
- `CreatedAt`

Chunking rules:

- Keep chunk content understandable.
- Preserve page number or slide number if available.
- Avoid chunks that are too short and meaningless.
- Avoid chunks that exceed the embedding model context limit.

---

## 8. Coding Style

General rules:

- Use C# async/await.
- Use dependency injection.
- Use interfaces for services and repositories.
- Keep controllers thin.
- Keep services focused.
- Avoid duplicate logic.
- Use meaningful names.
- Prefer clear code over clever code.
- Validate input before processing.
- Handle exceptions gracefully.
- Use logging for important workflows.

Naming conventions:

- Interfaces start with `I`.
- Services end with `Service`.
- Repositories end with `Repository`.
- ViewModels end with `ViewModel`.
- Async methods end with `Async`.

Examples:

```text
IChatbotService
ChatbotService
IDocumentRepository
DocumentRepository
ChatRequestViewModel
AskAsync()
UploadDocumentAsync()
```

---

## 9. Security Rules

Do not expose API keys in source code.

API keys must be stored in one of these places:

- User secrets for local development
- Environment variables
- `appsettings.Development.json` ignored by Git

Never commit:

- OpenAI API key
- Gemini API key
- Qdrant API key
- Real database password
- Personal credentials

Validate uploaded files:

- Check extension.
- Check content type where possible.
- Limit file size.
- Store files outside executable code paths when possible.
- Never execute uploaded files.

---

## 10. UI Rules

The UI should be simple and suitable for a student learning system.

Main pages:

- Login / Register
- Dashboard
- Subject management
- Document upload
- Document list
- Chatbot page
- Chat history
- Benchmark page

Chat page should include:

- Subject selector
- Question input
- Chat message area
- Answer area
- Citation area
- Loading indicator

User-facing text should be Vietnamese.

Code identifiers should remain English.

Good C# naming:

```csharp
public class DocumentService
{
}
```

Good UI text:

```text
Tải tài liệu lên
Danh sách tài liệu
Đặt câu hỏi
Nguồn tham khảo
```

Avoid Vietnamese class names such as:

```csharp
public class DichVuTaiLieu
{
}
```

---

## 11. Error Handling Rules

Use clear Vietnamese error messages for end users.

Examples:

- `Không thể đọc nội dung tài liệu.`
- `Tài liệu chưa được index.`
- `Không tìm thấy nội dung liên quan trong tài liệu.`
- `Có lỗi khi gọi mô hình AI.`
- `File không đúng định dạng được hỗ trợ.`

Do not show raw exception stack traces to end users.

Log technical details using `ILogger`.

---

## 12. Testing and Verification

When adding or changing functionality:

- Build the solution.
- Fix compile errors.
- Check dependency direction.
- Check async usage.
- Check that controllers do not contain business logic.
- Check that repositories do not contain business rules.
- Check that services do not directly return MVC Views.

Preferred commands:

```bash
dotnet restore
dotnet build
```

If tests are added:

```bash
dotnet test
```

---

## 13. Implementation Priority

When asked to implement the project, follow this priority:

1. Database connection and entities.
2. Repository layer.
3. Service layer.
4. MVC controllers and views.
5. Upload document feature.
6. PDF text extraction.
7. Chunking and saving chunks.
8. Embedding integration.
9. Vector search.
10. Chatbot RAG answer.
11. Citation display.
12. Chat history.
13. RAGAS benchmark page.

If the request is too large, implement the smallest working vertical slice first:

```text
Upload one PDF
→ Extract text
→ Chunk text
→ Save chunks
→ Ask a question
→ Return answer with source chunks
```

---

## 14. ASP.NET Core MVC Rules

Use ASP.NET Core MVC conventions.

Controllers should be in:

```text
Presentation/Controllers
```

Views should be in:

```text
Presentation/Views
```

Static assets should be in:

```text
Presentation/wwwroot
```

Use ViewModels for form input and UI rendering.

Do not pass EF entities directly to complex forms when a ViewModel is more appropriate.

---

## 15. Vietnamese Academic Context

This project is for a Vietnamese academic software project.

When generating:

- Page titles
- Menu labels
- Form labels
- Validation messages
- Notifications
- Demo data

Prefer Vietnamese.

When generating:

- Class names
- Method names
- Variable names
- Interface names
- Repository names
- Service names

Use English.

---

## 16. Git and Commit Safety

Before suggesting or generating code, avoid changes that:

- Delete existing user code without explanation.
- Rewrite the whole project unnecessarily.
- Mix many unrelated features in one change.
- Move files between layers without checking dependency direction.

Prefer small, incremental changes.

---

## 17. Final Reminder for Codex

Always preserve the 3-layer architecture.

The most important rule:

```text
Controller does not contain business logic.
BusinessLogic does not contain UI logic.
DataAccess does not contain business workflow.
BusinessObject does not depend on other layers.
```
