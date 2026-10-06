using CvReader.Application.Cv;

namespace CvReader.Api.Background;

// Yüklenen CV'lerin aday bilgilerini istek dışında çıkarır; yükleme, sohbet modelinin hızına bağlı kalmaz.
public class CvFieldExtractionWorker : BackgroundService
{
    private const int BatchSize = 5;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CvFieldExtractionWorker> _logger;

    public CvFieldExtractionWorker(IServiceScopeFactory scopeFactory, ILogger<CvFieldExtractionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<CvFieldExtractionService>();

                processed = await service.ProcessPendingAsync(BatchSize, stoppingToken);
            }
            // Veritabanı gibi geçici bir hata arka plan işini kalıcı olarak durdurmamalı.
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "CV field extraction failed.");
            }

            // Kuyruk boşsa ya da isim servisi yanıt vermiyorsa bir süre beklenir.
            if (processed == 0)
                await Task.Delay(IdleDelay, stoppingToken);
        }
    }
}
