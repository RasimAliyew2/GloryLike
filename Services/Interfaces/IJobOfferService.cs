using GloryLikeBackend.Dtos.JobOffers;

namespace GloryLikeBackend.Services.Interfaces;

public interface IJobOfferService
{
    Task<List<JobOfferDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
