using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyFunnelService : ICompanyFunnelService
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;

    public CompanyFunnelService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
    }

    public async Task<CompanyFunnelResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);
        if (access is null)
            return Forbidden();

        var templates = await LoadTemplatesAsync(
            access.CompanyOwnerUserId,
            cancellationToken);

        return Successful(
            access,
            templates.Count == 0
                ? "The company template library is empty."
                : $"{templates.Count} company templates loaded.",
            templates: templates);
    }

    public async Task<CompanyFunnelResponse> CreateAsync(
        SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var validation = Validate(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return Failed(validation, CompanyTemplateErrorCodes.Validation);

        var access = await ResolveManagementAccessAsync(
            request.ActorUserId,
            cancellationToken);
        if (access is null)
            return Forbidden();

        if (await HasCustomNameConflictAsync(
                access.CompanyOwnerUserId,
                request.Name,
                null,
                cancellationToken))
        {
            return Failed(
                "A custom template with this name already exists.",
                CompanyTemplateErrorCodes.Conflict);
        }

        var now = DateTime.UtcNow;
        var template = new CompanyFunnelTemplate
        {
            Id = Guid.NewGuid(),
            CompanyOwnerUserId = access.CompanyOwnerUserId,
            CreatedByUserId = access.ActorUserId,
            Name = request.Name,
            Description = request.Description,
            StagesJson = JsonSerializer.Serialize(request.Stages),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.CompanyFunnelTemplates.Add(template);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is
            Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            return Failed("This template was changed concurrently. Reload and try again.",
                CompanyTemplateErrorCodes.Conflict);
        }

        return Successful(
            access,
            "Template created.",
            ToDto(template));
    }

    public async Task<CompanyFunnelResponse> UpdateAsync(
        Guid templateId,
        SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var validation = Validate(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return Failed(validation, CompanyTemplateErrorCodes.Validation);

        var access = await ResolveManagementAccessAsync(
            request.ActorUserId,
            cancellationToken);
        if (access is null)
            return Forbidden();

        var now = DateTime.UtcNow;

        if (CompanyFunnelTemplateCatalog.ById.TryGetValue(
                templateId,
                out var definition))
        {
            var template = await _dbContext.CompanyFunnelTemplates
                .FirstOrDefaultAsync(
                    item => item.CompanyOwnerUserId == access.CompanyOwnerUserId
                        && item.DefaultKey == definition.Key,
                    cancellationToken);

            if (template is null)
            {
                template = new CompanyFunnelTemplate
                {
                    Id = Guid.NewGuid(),
                    CompanyOwnerUserId = access.CompanyOwnerUserId,
                    CreatedByUserId = access.ActorUserId,
                    DefaultKey = definition.Key,
                    CreatedAtUtc = now
                };
                _dbContext.CompanyFunnelTemplates.Add(template);
            }

            Apply(template, request, now);
            template.IsDeleted = false;
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is
                Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                return Failed("This template was changed concurrently. Reload and try again.",
                    CompanyTemplateErrorCodes.Conflict);
            }

            return Successful(
                access,
                "Default template customized for this company.",
                ToDto(template, definition));
        }

        var customTemplate = await _dbContext.CompanyFunnelTemplates
            .FirstOrDefaultAsync(
                item => item.Id == templateId
                    && item.CompanyOwnerUserId == access.CompanyOwnerUserId
                    && item.DefaultKey == null
                    && !item.IsDeleted,
                cancellationToken);
        if (customTemplate is null)
            return NotFound();

        if (await HasCustomNameConflictAsync(
                access.CompanyOwnerUserId,
                request.Name,
                templateId,
                cancellationToken))
        {
            return Failed(
                "A custom template with this name already exists.",
                CompanyTemplateErrorCodes.Conflict);
        }

        Apply(customTemplate, request, now);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is
            Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            return Failed("This template was changed concurrently. Reload and try again.",
                CompanyTemplateErrorCodes.Conflict);
        }

        return Successful(
            access,
            "Template updated.",
            ToDto(customTemplate));
    }

    public async Task<CompanyFunnelResponse> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveManagementAccessAsync(
            actorUserId,
            cancellationToken);
        if (access is null)
            return Forbidden();

        if (CompanyFunnelTemplateCatalog.ById.TryGetValue(
                templateId,
                out var definition))
        {
            var now = DateTime.UtcNow;
            var template = await _dbContext.CompanyFunnelTemplates
                .FirstOrDefaultAsync(
                    item => item.CompanyOwnerUserId == access.CompanyOwnerUserId
                        && item.DefaultKey == definition.Key,
                    cancellationToken);

            if (template is null)
            {
                template = new CompanyFunnelTemplate
                {
                    Id = Guid.NewGuid(),
                    CompanyOwnerUserId = access.CompanyOwnerUserId,
                    CreatedByUserId = access.ActorUserId,
                    DefaultKey = definition.Key,
                    Name = definition.Name,
                    Description = definition.Description,
                    StagesJson = JsonSerializer.Serialize(definition.Stages),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    IsDeleted = true
                };
                _dbContext.CompanyFunnelTemplates.Add(template);
            }
            else
            {
                template.IsDeleted = true;
                template.UpdatedAtUtc = now;
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is
                Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                return Failed("This template was changed concurrently. Reload and try again.",
                    CompanyTemplateErrorCodes.Conflict);
            }
            return Successful(
                access,
                "Default template removed from this company only.");
        }

        var customTemplate = await _dbContext.CompanyFunnelTemplates
            .FirstOrDefaultAsync(
                item => item.Id == templateId
                    && item.CompanyOwnerUserId == access.CompanyOwnerUserId
                    && item.DefaultKey == null,
                cancellationToken);
        if (customTemplate is null)
            return NotFound();

        _dbContext.CompanyFunnelTemplates.Remove(customTemplate);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is
            Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            return Failed("This template was changed concurrently. Reload and try again.",
                CompanyTemplateErrorCodes.Conflict);
        }

        return Successful(access, "Template deleted.");
    }

    private async Task<List<CompanyFunnelDto>> LoadTemplatesAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.CompanyFunnelTemplates
            .AsNoTracking()
            .Where(item => item.CompanyOwnerUserId == companyOwnerUserId)
            .ToListAsync(cancellationToken);

        var overrides = rows
            .Where(item => !string.IsNullOrWhiteSpace(item.DefaultKey))
            .GroupBy(item => item.DefaultKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.UpdatedAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);

        var result = new List<(int Order, CompanyFunnelDto Template)>();
        foreach (var definition in CompanyFunnelTemplateCatalog.All)
        {
            if (overrides.TryGetValue(definition.Key, out var companyOverride))
            {
                if (!companyOverride.IsDeleted)
                    result.Add((definition.SortOrder, ToDto(companyOverride, definition)));
                continue;
            }

            result.Add((definition.SortOrder, ToDto(definition)));
        }

        result.AddRange(rows
            .Where(item => item.DefaultKey == null && !item.IsDeleted)
            .OrderBy(item => item.CreatedAtUtc)
            .Select((item, index) => (1000 + index, ToDto(item))));

        return result
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Template.Name)
            .Select(item => item.Template)
            .ToList();
    }

    private async Task<CompanyAccessContext?> ResolveManagementAccessAsync(
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);
        return access?.CanManageTemplates == true ? access : null;
    }

    private Task<bool> HasCustomNameConflictAsync(
        int companyOwnerUserId,
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToUpper();
        return _dbContext.CompanyFunnelTemplates.AnyAsync(
            item => item.CompanyOwnerUserId == companyOwnerUserId
                && item.DefaultKey == null
                && !item.IsDeleted
                && (!exceptId.HasValue || item.Id != exceptId.Value)
                && item.Name.ToUpper() == normalizedName,
            cancellationToken);
    }

    private static void Apply(
        CompanyFunnelTemplate template,
        SaveCompanyFunnelRequest request,
        DateTime now)
    {
        template.Name = request.Name;
        template.Description = request.Description;
        template.StagesJson = JsonSerializer.Serialize(request.Stages);
        template.UpdatedAtUtc = now;
    }

    private static void Normalize(SaveCompanyFunnelRequest request)
    {
        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Description = request.Description?.Trim() ?? string.Empty;
        request.Stages ??= [];
        foreach (var stage in request.Stages.Where(s => s is not null))
        {
            stage.StageName = stage.StageName?.Trim() ?? string.Empty;
            stage.ResponsibleRole = stage.ResponsibleRole?.Trim() ?? string.Empty;
        }
    }
    private static string Validate(SaveCompanyFunnelRequest request)
    {
        if (request.ActorUserId <= 0) return "Employer sign in is required.";
        if (request.Name.Length is < 1 or > 120) return "Name must contain 1–120 characters.";
        if (request.Description.Length > 1000) return "Description must not exceed 1,000 characters.";
        if (request.Stages.Count is < 1 or > 20) return "Add between 1 and 20 stages.";
        if (request.Stages.Any(s => s is null || s.StageName.Length is < 1 or > 100
            || s.Hours is < 0 or > 8760
            || s.ResponsibleRole is not ("Recruiter" or "Hiring Manager" or "HR")))
            return "Each stage needs a name, 0–8760 hours and a valid responsible role.";
        if (request.Stages.Select(s => s.StageName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Stages.Count)
            return "Stage names must be unique.";
        return string.Empty;
    }

    private static CompanyFunnelDto ToDto(
        CompanyFunnelTemplate template,
        CompanyFunnelTemplateDefinition? definition = null)
    {
        return new CompanyFunnelDto
        {
            Id = definition?.Id ?? template.Id,
            DefaultKey = definition?.Key ?? template.DefaultKey ?? string.Empty,
            IsDefault = definition is not null,
            Name = template.Name,
            Description = template.Description,
            Stages = JsonSerializer.Deserialize<List<FunnelTemplateStage>>(template.StagesJson) ?? [],
            UpdatedAtUtc = template.UpdatedAtUtc
        };
    }

    private static CompanyFunnelDto ToDto(
        CompanyFunnelTemplateDefinition definition)
    {
        return new CompanyFunnelDto
        {
            Id = definition.Id,
            DefaultKey = definition.Key,
            IsDefault = true,
            Name = definition.Name,
            Description = definition.Description,
            Stages = definition.Stages.Select(s => new FunnelTemplateStage { StageName = s.StageName, Hours = s.Hours, ResponsibleRole = s.ResponsibleRole }).ToList()
        };
    }

    private static CompanyFunnelResponse Successful(
        CompanyAccessContext access,
        string message,
        CompanyFunnelDto? template = null,
        List<CompanyFunnelDto>? templates = null)
    {
        return new CompanyFunnelResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = access.CompanyOwnerUserId,
            CanManageTemplates = access.CanManageTemplates,
            Template = template,
            Templates = templates ?? []
        };
    }

    private static CompanyFunnelResponse Forbidden() => Failed(
        "You do not have access to manage company templates.",
        CompanyTemplateErrorCodes.Forbidden);

    private static CompanyFunnelResponse NotFound() => Failed(
        "Template was not found for this company.",
        CompanyTemplateErrorCodes.NotFound);

    private static CompanyFunnelResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyFunnelResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
