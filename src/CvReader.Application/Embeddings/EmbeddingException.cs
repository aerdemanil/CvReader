namespace CvReader.Application.Embeddings;

public class EmbeddingException : Exception
{
    public EmbeddingException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
