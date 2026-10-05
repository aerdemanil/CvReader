using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
using CvReader.Infrastructure.Embeddings;
using CvReader.Infrastructure.Parsing;
using CvReader.Infrastructure.Persistence;
using CvReader.Infrastructure.Persistence.Repositories;
using CvReader.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CvReader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));

        services.AddSingleton<ICvParser, PdfPigCvParser>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IJobPostingRepository, JobPostingRepository>();
        services.AddScoped<IMatchResultRepository, MatchResultRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((sp, client) =>
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl));

        return services;
    }
}
