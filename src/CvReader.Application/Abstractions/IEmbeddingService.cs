namespace CvReader.Application.Abstractions;

public record TermEmbedding(string Term, float[] Embedding);

public interface IEmbeddingService
{
    // Her terim için bir vektör, verilen sırayla.
    Task<List<TermEmbedding>> EmbedAsync(IReadOnlyList<string> terms, CancellationToken ct);
}
