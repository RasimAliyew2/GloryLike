using GloryLikeBackend.Dtos.CompanyStructure;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyStructureService
{
    Task<CompanyStructureResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureResponse> SaveAsync(
        SaveCompanyStructureRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureResponse> ImportAsync(
        int actorUserId,
        Stream input,
        CancellationToken cancellationToken = default);

    Task<CompanyStructureExportResult> ExportAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}
