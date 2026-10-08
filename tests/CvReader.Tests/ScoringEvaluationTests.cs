using System.Text.Json;
using CvReader.Api.Auth;
using CvReader.Application;
using CvReader.Application.Abstractions;
using CvReader.Application.Matching;
using CvReader.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CvReader.Tests;

// Etiket dosyasının yolu verilmediyse (CI dahil) test atlanır.
public sealed class EvaluationFactAttribute : FactAttribute
{
    public const string LabelsPathVariable = "CVREADER_EVAL_LABELS";

    public EvaluationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LabelsPathVariable)))
            Skip = $"{LabelsPathVariable} is not set.";
    }
}

// Skorlamayı elle etiketlenmiş gerçek CV'lerle ölçer; geliştirme veritabanına bağlanır, hiçbir şey yazmaz.
// Etiket dosyası kişisel veri içerir ve depoya girmez.
public class ScoringEvaluationTests
{
    // Labels: dosya adı -> 0 uygun değil, 1 kısmen, 2 uygun.
    private record LabelledJob(string Title, Dictionary<string, int> Labels);

    private record LabelSet(string OwnerEmail, List<LabelledJob> Jobs);

    private const int TopCount = 10;
    private const int Suitable = 2;
    private const int Unsuitable = 0;

    private readonly ITestOutputHelper _output;

    public ScoringEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [EvaluationFact]
    public async Task Reports_how_well_the_scores_rank_the_labelled_cvs()
    {
        var ct = CancellationToken.None;
        var path = Environment.GetEnvironmentVariable(EvaluationFactAttribute.LabelsPathVariable)!;
        var labelSet = JsonSerializer.Deserialize<LabelSet>(await File.ReadAllTextAsync(path, ct), JsonSerializerOptions.Web)!;

        // Bağlantı dizesi API projesinin user-secrets deposundan okunur.
        var configuration = new ConfigurationBuilder().AddUserSecrets(typeof(JwtOptions).Assembly).Build();
        await using var provider = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var jobs = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();
        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();

        var owner = await users.GetByEmailAsync(labelSet.OwnerEmail, ct);
        Assert.NotNull(owner);
        var ownerJobs = await jobs.GetAllAsync(owner.Id, ct);

        foreach (var labelled in labelSet.Jobs)
        {
            var job = Assert.Single(ownerJobs, j => j.Title == labelled.Title);
            var page = await matching.GetResultsAsync(owner.Id, job.Id, 1, int.MaxValue, ct);

            // Sonuçlar skora göre sıralı gelir; etiketlenmemiş CV'ler ölçüme girmez.
            var ranked = page!.Items
                .Where(i => labelled.Labels.ContainsKey(i.FileName))
                .Select(i => (i.FileName, i.Score, Label: labelled.Labels[i.FileName]))
                .ToList();

            // Dosya adı yanlış yazılmış bir etiket sessizce yok sayılmasın.
            Assert.Empty(labelled.Labels.Keys.Except(ranked.Select(r => r.FileName)));

            var suitableCount = ranked.Count(r => r.Label == Suitable);
            var suitableInTop = ranked.Take(TopCount).Count(r => r.Label == Suitable);
            var gap = AverageScore(ranked.Where(r => r.Label == Suitable).Select(r => r.Score))
                      - AverageScore(ranked.Where(r => r.Label == Unsuitable).Select(r => r.Score));

            _output.WriteLine($"{labelled.Title}: ilk {TopCount} içinde uygun {suitableInTop}/{Math.Min(TopCount, suitableCount)}, skor farkı {gap:F1}");
            foreach (var (fileName, score, label) in ranked)
                _output.WriteLine($"  {score,5:F1}  {label}  {fileName}");
        }
    }

    private static double AverageScore(IEnumerable<double> scores) =>
        scores.DefaultIfEmpty().Average();
}
