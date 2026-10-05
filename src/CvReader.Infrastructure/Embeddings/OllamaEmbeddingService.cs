using System.Net.Http.Json;
using CvReader.Application.Abstractions;
using CvReader.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace CvReader.Infrastructure.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    // Ollama varsayılan olarak daha küçük bir bağlam kullanır; uzun CV'nin sonu kesilmesin.
    private const int ContextLength = 8192;

    private record EmbedResponse(float[][] Embeddings);

    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var request = new
        {
            model = _options.Model,
            input = text,
            options = new { num_ctx = ContextLength }
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/embed", request, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(ct);
            return body!.Embeddings[0];
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingException("The embedding service is not reachable.", ex);
        }
    }
}
