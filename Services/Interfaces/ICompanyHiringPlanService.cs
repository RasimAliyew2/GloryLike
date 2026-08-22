using GloryLikeBackend.Dtos.CompanyHiringPlan;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyHiringPlanService
{
    Task<CompanyHiringPlanResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanResponse> GetByIdAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanResponse> CreateAsync(
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanResponse> UpdateAsync(
        int planId,
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanResponse> DeleteAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default);

    Task<CompanyHiringPlanResponse> ImportAsync(
        int actorUserId,
        Stream input,
        CancellationToken cancellationToken = default);
}
