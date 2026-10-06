using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
using CvReader.Infrastructure.Embeddings;
using CvReader.Infrastructure.Extraction;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Set it with 'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.UseVector()));

        services.AddSingleton<ICvParser, PdfPigCvParser>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IJobPostingRepository, JobPostingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();

        // Eksik ya da hatalı ayar ilk istekte değil, uygulama açılırken fark edilir.
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .Validate(
                o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _)
                     && !string.IsNullOrWhiteSpace(o.Model)
                     && !string.IsNullOrWhiteSpace(o.ChatModel)
                     && o.TimeoutSeconds > 0,
                "Ollama:BaseUrl must be an absolute URL, Ollama:Model and Ollama:ChatModel must be set and Ollama:TimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(ConfigureOllamaClient);
        services.AddHttpClient<ICvNameExtractor, OllamaCvNameExtractor>(ConfigureOllamaClient);

        return services;
    }

    private static void ConfigureOllamaClient(IServiceProvider sp, HttpClient client)
    {
        var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}
