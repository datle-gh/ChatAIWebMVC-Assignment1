using System;
using System.Collections.Generic;
using BusinessObject.Entities;
using BusinessObject.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public partial class ChatAIWebDbContext : DbContext
{
    public ChatAIWebDbContext(DbContextOptions<ChatAIWebDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<Citation> Citations { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentChunk> DocumentChunks { get; set; }

    public virtual DbSet<DocumentChunkEmbedding> DocumentChunkEmbeddings { get; set; }

    public virtual DbSet<EvaluationQuestion> EvaluationQuestions { get; set; }

    public virtual DbSet<RagasBenchmarkResult> RagasBenchmarkResults { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SubjectEnrollment> SubjectEnrollments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMess__3214EC076F893BC2");

            entity.HasIndex(e => e.ChatSessionId, "IX_ChatMessages_ChatSessionId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasConversion<string>();

            entity.HasOne(d => d.ChatSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatSessionId)
                .HasConstraintName("FK_ChatMessages_ChatSessions");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatSess__3214EC07A58C6DE1");

            entity.HasIndex(e => e.SubjectId, "IX_ChatSessions_SubjectId");

            entity.HasIndex(e => e.UserId, "IX_ChatSessions_UserId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Subject).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK_ChatSessions_Subjects");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<Citation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Citation__3214EC07DA7723D7");

            entity.HasIndex(e => e.ChatMessageId, "IX_Citations_ChatMessageId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SimilarityScore).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.ChatMessage).WithMany(p => p.Citations)
                .HasForeignKey(d => d.ChatMessageId)
                .HasConstraintName("FK_Citations_ChatMessages");

            entity.HasOne(d => d.Chunk).WithMany(p => p.Citations)
                .HasForeignKey(d => d.ChunkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citations_DocumentChunks");

            entity.HasOne(d => d.Document).WithMany(p => p.Citations)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citations_Documents");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC072E6BA052");

            entity.HasIndex(e => e.Status, "IX_Documents_Status");

            entity.HasIndex(e => e.SubjectId, "IX_Documents_SubjectId");

            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.FileType).HasMaxLength(20);
            entity.Property(e => e.IndexedAt).HasPrecision(0);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasConversion<string>()
                .HasDefaultValue(DocumentStatus.Uploaded);
            entity.Property(e => e.StoredFileName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UploadedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Subject).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Subjects");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Documents).HasForeignKey(d => d.UploadedBy);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07AD973549");

            entity.HasIndex(e => e.DocumentId, "IX_DocumentChunks_DocumentId");

            entity.HasIndex(e => e.VectorId, "IX_DocumentChunks_VectorId").HasFilter("([VectorId] IS NOT NULL)");

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }, "UQ_DocumentChunks_Document_ChunkIndex").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingModel).HasMaxLength(100);
            entity.Property(e => e.VectorId).HasMaxLength(100);

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentChunks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("FK_DocumentChunks_Documents");
        });

        modelBuilder.Entity<DocumentChunkEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_DocumentChunkEmbeddings");

            entity.HasIndex(e => e.DocumentChunkId, "IX_DocumentChunkEmbeddings_DocumentChunkId");

            entity.HasIndex(e => e.VectorId, "IX_DocumentChunkEmbeddings_VectorId").HasFilter("([VectorId] IS NOT NULL)");

            entity.HasIndex(e => new { e.DocumentChunkId, e.EmbeddingModel }, "UQ_DocumentChunkEmbeddings_Chunk_Model").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingModel).HasMaxLength(100);
            entity.Property(e => e.EmbeddingProvider).HasMaxLength(50);
            entity.Property(e => e.VectorId).HasMaxLength(100);
            entity.Property(e => e.VectorStore).HasMaxLength(50);

            entity.HasOne(d => d.DocumentChunk).WithMany(p => p.DocumentChunkEmbeddings)
                .HasForeignKey(d => d.DocumentChunkId)
                .HasConstraintName("FK_DocumentChunkEmbeddings_DocumentChunks");
        });

        modelBuilder.Entity<EvaluationQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Evaluati__3214EC073212E25A");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EvaluationQuestions).HasForeignKey(d => d.CreatedBy);

            entity.HasOne(d => d.Subject).WithMany(p => p.EvaluationQuestions)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EvaluationQuestions_Subjects");
        });

        modelBuilder.Entity<RagasBenchmarkResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RagasBen__3214EC0770165910");

            entity.Property(e => e.AnswerRelevancy).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.ChunkingStrategy).HasMaxLength(100);
            entity.Property(e => e.ContextPrecision).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.ContextRecall).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingModel).HasMaxLength(100);
            entity.Property(e => e.Faithfulness).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.LlmModel).HasMaxLength(100);
            entity.Property(e => e.OverallScore).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.RunId).HasMaxLength(50);
            entity.Property(e => e.VectorStore).HasMaxLength(50);

            entity.HasOne(d => d.EvaluationQuestion).WithMany(p => p.RagasBenchmarkResults)
                .HasForeignKey(d => d.EvaluationQuestionId)
                .HasConstraintName("FK_RagasBenchmarkResults_EvaluationQuestions");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subjects__3214EC07850A85F6");

            entity.HasIndex(e => e.SubjectName, "IX_Subjects_SubjectName");

            entity.HasIndex(e => e.SubjectCode, "UQ_Subjects_SubjectCode").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SubjectCode).HasMaxLength(50);
            entity.Property(e => e.SubjectName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Subjects).HasForeignKey(d => d.CreatedBy);
        });

        modelBuilder.Entity<SubjectEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SubjectE__3214EC0790999A76");

            entity.HasIndex(e => new { e.SubjectId, e.UserId }, "UQ_SubjectEnrollments_Subject_User").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RoleInClass).HasMaxLength(30);

            entity.HasOne(d => d.Subject).WithMany(p => p.SubjectEnrollments)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubjectEnrollments_Subjects");

            entity.HasOne(d => d.User).WithMany(p => p.SubjectEnrollments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubjectEnrollments_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07B6655D9C");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
