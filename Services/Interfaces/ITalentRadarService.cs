using GloryLikeBackend.Dtos.TalentRadar;

namespace GloryLikeBackend.Services.Interfaces;

public interface ITalentRadarService
{
    Task<TalentRadarResponse?> GetAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);
}
