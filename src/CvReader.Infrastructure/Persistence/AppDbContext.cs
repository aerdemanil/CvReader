using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
}
