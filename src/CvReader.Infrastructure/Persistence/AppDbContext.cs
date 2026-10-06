using CvReader.Application.Cv;
using CvReader.Application.Matching;
using CvReader.Domain.Entities;
using CvReader.Infrastructure.Embeddings;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private static readonly string VectorColumnType = $"vector({EmbeddingVector.Dimensions})";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ProfileTerm> ProfileTerms => Set<ProfileTerm>();
    public DbSet<JobPostingTerm> JobPostingTerms => Set<JobPostingTerm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(200);
            entity.Property(j => j.Keywords).HasColumnType("varchar(100)[]");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(j => j.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // İlan listesi sahibine göre filtrelenir ve yeniden eskiye sıralanır.
            entity.HasIndex(j => new { j.OwnerId, j.CreatedAt });
        });

        modelBuilder.Entity<JobPostingTerm>(entity =>
        {
            entity.HasKey(t => new { t.JobPostingId, t.Term });
            entity.Property(t => t.Term).HasMaxLength(TermExtractor.MaxTermLength);
            entity.Property(t => t.Embedding).HasColumnType(VectorColumnType);

            entity.HasOne<JobPosting>()
                .WithMany()
                .HasForeignKey(t => t.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.Property(p => p.FileName).HasMaxLength(260);
            entity.Property(p => p.ContentHash).HasMaxLength(64); // SHA-256 hex
            entity.Property(p => p.FullName).HasMaxLength(CvFieldExtractionService.MaxNameLength);
            entity.Property(p => p.Email).HasMaxLength(ContactExtractor.MaxEmailLength);
            entity.Property(p => p.Phone).HasMaxLength(ContactExtractor.MaxPhoneLength);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Klasör silinince CV'leri silinmez, klasörsüz kalır.
            entity.HasOne<Folder>()
                .WithMany()
                .HasForeignKey(p => p.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Aynı kullanıcı aynı dosyayı iki kez kaydedemez; sahibine göre filtrelemeyi de karşılar.
            entity.HasIndex(p => new { p.OwnerId, p.ContentHash }).IsUnique();

            // Yalnızca alan çıkarımı bekleyen CV'leri içerir; kuyruk boşken indeks de boştur.
            entity.HasIndex(p => p.CreatedAt)
                .HasFilter("\"FieldsExtractedAt\" IS NULL")
                .HasDatabaseName("IX_Profiles_PendingExtraction");
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.Property(f => f.Name).HasMaxLength(100);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.OwnerId, f.Name }).IsUnique();
        });

        modelBuilder.Entity<ProfileTerm>(entity =>
        {
            // Bir CV'nin terimleri bu anahtar üzerinden okunur.
            entity.HasKey(t => new { t.ProfileId, t.Term });
            entity.Property(t => t.Term).HasMaxLength(TermExtractor.MaxTermLength);
            entity.Property(t => t.Embedding).HasColumnType(VectorColumnType);

            entity.HasOne<Profile>()
                .WithMany()
                .HasForeignKey(t => t.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.PasswordHash).HasMaxLength(60);

            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}
