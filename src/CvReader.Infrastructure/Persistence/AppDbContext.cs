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
    public DbSet<ProfileEmbedding> ProfileEmbeddings => Set<ProfileEmbedding>();
    public DbSet<JobPostingEmbedding> JobPostingEmbeddings => Set<JobPostingEmbedding>();

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

        modelBuilder.Entity<JobPostingEmbedding>(entity =>
        {
            entity.HasKey(e => e.JobPostingId);
            entity.Property(e => e.Embedding).HasColumnType(VectorColumnType);

            entity.HasOne<JobPosting>()
                .WithOne()
                .HasForeignKey<JobPostingEmbedding>(e => e.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.Property(p => p.FileName).HasMaxLength(260);
            entity.Property(p => p.ContentHash).HasMaxLength(64); // SHA-256 hex

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

        modelBuilder.Entity<ProfileEmbedding>(entity =>
        {
            entity.HasKey(e => e.ProfileId);
            entity.Property(e => e.Embedding).HasColumnType(VectorColumnType);

            entity.HasOne<Profile>()
                .WithOne()
                .HasForeignKey<ProfileEmbedding>(e => e.ProfileId)
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
