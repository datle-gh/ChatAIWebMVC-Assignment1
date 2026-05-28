using BusinessLogic.Infrastructure;
using BusinessLogic.Services;
using DataAccess;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Presentation.Hubs;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

var uploadSettings = UploadSettings.FromConfiguration(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ChatAIWeb.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadSettings.MaxBatchSizeBytes;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = uploadSettings.MaxBatchSizeBytes;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadSettings.MaxBatchSizeBytes;
});
builder.Services.AddSingleton(uploadSettings);

// ==================== Database ====================
builder.Services.AddDbContext<ChatAIWebDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==================== Services injection ====================
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmbeddingModelRegistry, EmbeddingModelRegistry>();
builder.Services.AddScoped<IEmbeddingService>(provider => provider.GetRequiredService<IEmbeddingModelRegistry>().GetDefault());
builder.Services.AddScoped<SqlVectorStoreService>();
builder.Services.AddHttpClient<QdrantVectorStoreService>();
builder.Services.AddScoped<VectorStoreRouter>();
builder.Services.AddScoped<IVectorStoreService>(provider => provider.GetRequiredService<VectorStoreRouter>());
builder.Services.AddScoped<IVectorSearchService>(provider => provider.GetRequiredService<VectorStoreRouter>());
builder.Services.AddScoped<IEmbeddingBackfillService, EmbeddingBackfillService>();
if (string.Equals(builder.Configuration["Llm:Provider"], "Fake", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<ILlmService, FakeLlmService>();
}
else
{
    builder.Services.AddHttpClient<ILlmService, GeminiLlmService>();
}

builder.Services.AddScoped<PromptBuilder>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();
builder.Services.AddScoped<IUploadProgressReporter, SignalRUploadProgressReporter>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IRagasEvaluationService, RagasEvaluationService>();
builder.Services.AddHttpClient<IRagasEvaluatorClient, RagasEvaluatorClient>();

builder.Services.Configure<SystemSettingsFilePathOptions>(options =>
{
    options.FilePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "system_settings.json");
});
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
// ==================== Repository injection ====================
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<IDocumentChunkEmbeddingRepository, DocumentChunkEmbeddingRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<ICitationRepository, CitationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IEvaluationQuestionRepository, EvaluationQuestionRepository>();
builder.Services.AddScoped<IRagasBenchmarkResultRepository, RagasBenchmarkResultRepository>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}");
app.MapHub<UploadProgressHub>("/hubs/upload-progress");

app.Run();
