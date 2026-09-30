using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public interface IProfileRepository
{
    Task AddAsync(Profile profile, CancellationToken ct);
    Task<List<Profile>> GetAllAsync(CancellationToken ct);
}