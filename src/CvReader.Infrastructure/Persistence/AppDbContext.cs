using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(200);
            entity.Property(j => j.Keywords).HasColumnType("varchar(100)[]");
        });

        
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.Property(p => p.FullName).HasMaxLength(200);
            entity.Property(p => p.Email).HasMaxLength(256);
            entity.Property(p => p.Phone).HasMaxLength(32);
            entity.Property(p => p.FileName).HasMaxLength(260);
            
        });

        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.Property(r => r.Tier)
                .HasConversion<string>()
                .HasMaxLength(2);

            entity.Property(r => r.MatchedKeywords).HasColumnType("varchar(100)[]"); 
            entity.Property(r => r.MissingKeywords).HasColumnType("varchar(100)[]"); 

            entity.HasIndex(r => new { r.JobPostingId, r.ProfileId })
                .IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Role).HasMaxLength(32);
            entity.Property(u => u.PasswordHash).HasMaxLength(60); 

            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}