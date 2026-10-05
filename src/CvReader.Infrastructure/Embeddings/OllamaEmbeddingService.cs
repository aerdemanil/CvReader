using System.Net.Http.Json;
using System.Text.Json;
using CvReader.Application.Abstractions;
using CvReader.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace CvReader.Infrastructure.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    private record EmbedResponse(float[][]? Embeddings);

    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<List<TermEmbedding>> EmbedAsync(IReadOnlyList<string> terms, CancellationToken ct)
    {
        // input bir dizi olunca Ollama her öğe için ayrı bir vektör döner.
        var request = new
        {
            model = _options.Model,
            input = terms
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

        if (body?.Embeddings is not { } embeddings
            || embeddings.Length != terms.Count
            || embeddings.Any(e => e.Length != EmbeddingVector.Dimensions))
            throw new EmbeddingException("The embedding service returned an unexpected response.");

        return terms.Select((term, i) => new TermEmbedding(term, embeddings[i])).ToList();
    }
}
