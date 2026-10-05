using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public interface IProfileRepository
{
    Task AddAsync(Profile profile, float[] embedding, CancellationToken ct);

    // Her profilin sorgu vektörüne cosine benzerliği (profil Id → benzerlik).
    Task<Dictionary<Guid, double>> GetSimilaritiesAsync(float[] query, CancellationToken ct);
}
