using Pgvector;

namespace CvReader.Infrastructure.Persistence;

// CV'de geçen bir terim ve vektörü. Domain'e Pgvector bağımlılığı girmesin diye ayrı bir persistence modelinde tutulur.
public class ProfileTerm
{
    public Guid ProfileId { get; set; }
    public string Term { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
}
