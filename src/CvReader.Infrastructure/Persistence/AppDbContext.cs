using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchResult>(entity =>
        {
            // Seviyeyi veritabanında 1/2/3 yerine "K1"/"K2"/"K3" olarak sakla.
            entity.Property(r => r.Tier)
                .HasConversion<string>()
                .HasMaxLength(2);

            // Bir profil bir ilan için yalnızca bir kez skorlanabilir.
            entity.HasIndex(r => new { r.JobPostingId, r.ProfileId })
                .IsUnique();
        });
    }
}
