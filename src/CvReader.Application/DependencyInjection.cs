using CvReader.Application.Cv;
using CvReader.Application.Jobs;
using CvReader.Application.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace CvReader.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CvUploadService>();
        services.AddScoped<JobPostingService>();
        services.AddScoped<MatchingService>();

        return services;
    }
}
