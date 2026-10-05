namespace CvReader.Infrastructure.Embeddings;

public static class EmbeddingVector
{
    // bge-m3 boyutu. Kolon tipi ve servis yanıtı bununla doğrulanır; model değişirse migration gerekir.
    public const int Dimensions = 1024;
}
