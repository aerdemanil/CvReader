using Pgvector;

namespace CvReader.Infrastructure.Persistence;

// Domain'e Pgvector bağımlılığı girmesin diye vektör ayrı bir persistence modelinde tutulur.
public class ProfileEmbedding
{
    public Guid ProfileId { get; set; }
    public Vector Embedding { get; set; } = null!;
}
