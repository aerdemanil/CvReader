using Pgvector;

namespace CvReader.Infrastructure.Persistence;

// İlanın anahtar kelimelerinden çıkan bir terim ve vektörü.
public class JobPostingTerm
{
    public Guid JobPostingId { get; set; }
    public string Term { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
}
