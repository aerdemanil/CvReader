namespace CvReader.Application.Abstractions;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
