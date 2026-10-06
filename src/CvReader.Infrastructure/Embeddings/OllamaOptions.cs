namespace CvReader.Infrastructure.Embeddings;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    // CV'den aday adını çıkaran sohbet modeli.
    public string ChatModel { get; set; } = string.Empty;

    // İlk istekte modelin belleğe yüklenmesi onlarca saniye sürebilir.
    public int TimeoutSeconds { get; set; } = 90;
}
