using System.Net.Http.Json;
using System.Text.Json;
using CvReader.Application.Abstractions;
using CvReader.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace CvReader.Infrastructure.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    // Ollama varsayılan olarak daha küçük bir bağlam kullanır; uzun CV'nin sonu kesilmesin.
    private const int ContextLength = 8192;

    private record EmbedResponse(float[][]? Embeddings);

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

        EmbedResponse? body;
        try
        {
            var response = await _http.PostAsJsonAsync("/api/embed", request, ct);
            response.EnsureSuccessStatusCode();

            body = await response.Content.ReadFromJsonAsync<EmbedResponse>(ct);
        }
        // İstemcinin kendi iptali (ct) hata sayılmaz; HttpClient zaman aşımı da TaskCanceledException fırlatır.
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new EmbeddingException("The embedding service is not reachable.", ex);
        }

        // Boş metin için Ollama boş liste döner.
        if (body?.Embeddings is not [{ Length: EmbeddingVector.Dimensions } embedding, ..])
            throw new EmbeddingException("The embedding service returned an unexpected response.");

        return embedding;
    }
}
