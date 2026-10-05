using Pgvector;

namespace CvReader.Infrastructure.Persistence;

// Domain'e Pgvector bağımlılığı girmesin diye vektör ayrı bir persistence modelinde tutulur.
public class JobPostingEmbedding
{
    public Guid JobPostingId { get; set; }
    public Vector Embedding { get; set; } = null!;
}
