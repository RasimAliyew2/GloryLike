using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyTemplateService : ICompanyTemplateService
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;

    public CompanyTemplateService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
    }

    public async Task<CompanyTemplateResponse> GetAsync(
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

    public async Task<CompanyTemplateResponse> CreateAsync(
        SaveCompanyTemplateRequest request,
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
        var template = new CompanyLetterTemplate
        {
            Id = Guid.NewGuid(),
            CompanyOwnerUserId = access.CompanyOwnerUserId,
            CreatedByUserId = access.ActorUserId,
            Name = request.Name,
            Audience = request.Audience,
            Category = request.Category,
            Subject = request.Subject,
            Body = request.Body,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.CompanyLetterTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Successful(
            access,
            "Template created.",
            ToDto(template));
    }

    public async Task<CompanyTemplateResponse> UpdateAsync(
        Guid templateId,
        SaveCompanyTemplateRequest request,
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

        if (CompanyLetterTemplateCatalog.ById.TryGetValue(
                templateId,
                out var definition))
        {
            var template = await _dbContext.CompanyLetterTemplates
                .FirstOrDefaultAsync(
                    item => item.CompanyOwnerUserId == access.CompanyOwnerUserId
                        && item.DefaultKey == definition.Key,
                    cancellationToken);

            if (template is null)
            {
                template = new CompanyLetterTemplate
                {
                    Id = Guid.NewGuid(),
                    CompanyOwnerUserId = access.CompanyOwnerUserId,
                    CreatedByUserId = access.ActorUserId,
                    DefaultKey = definition.Key,
                    CreatedAtUtc = now
                };
                _dbContext.CompanyLetterTemplates.Add(template);
            }

            Apply(template, request, now);
            template.IsDeleted = false;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Successful(
                access,
                "Default template customized for this company.",
                ToDto(template, definition));
        }

        var customTemplate = await _dbContext.CompanyLetterTemplates
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Successful(
            access,
            "Template updated.",
            ToDto(customTemplate));
    }

    public async Task<CompanyTemplateResponse> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveManagementAccessAsync(
            actorUserId,
            cancellationToken);
        if (access is null)
            return Forbidden();

        if (CompanyLetterTemplateCatalog.ById.TryGetValue(
                templateId,
                out var definition))
        {
            var now = DateTime.UtcNow;
            var template = await _dbContext.CompanyLetterTemplates
                .FirstOrDefaultAsync(
                    item => item.CompanyOwnerUserId == access.CompanyOwnerUserId
                        && item.DefaultKey == definition.Key,
                    cancellationToken);

            if (template is null)
            {
                template = new CompanyLetterTemplate
                {
                    Id = Guid.NewGuid(),
                    CompanyOwnerUserId = access.CompanyOwnerUserId,
                    CreatedByUserId = access.ActorUserId,
                    DefaultKey = definition.Key,
                    Name = definition.Name,
                    Audience = definition.Audience,
                    Category = definition.Category,
                    Subject = definition.Subject,
                    Body = definition.Body,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    IsDeleted = true
                };
                _dbContext.CompanyLetterTemplates.Add(template);
            }
            else
            {
                template.IsDeleted = true;
                template.UpdatedAtUtc = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Successful(
                access,
                "Default template removed from this company only.");
        }

        var customTemplate = await _dbContext.CompanyLetterTemplates
            .FirstOrDefaultAsync(
                item => item.Id == templateId
                    && item.CompanyOwnerUserId == access.CompanyOwnerUserId
                    && item.DefaultKey == null,
                cancellationToken);
        if (customTemplate is null)
            return NotFound();

        _dbContext.CompanyLetterTemplates.Remove(customTemplate);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Successful(access, "Template deleted.");
    }

    private async Task<List<CompanyTemplateDto>> LoadTemplatesAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.CompanyLetterTemplates
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

        var result = new List<(int Order, CompanyTemplateDto Template)>();
        foreach (var definition in CompanyLetterTemplateCatalog.All)
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
            .OrderBy(item => AudienceOrder(item.Template.Audience))
            .ThenBy(item => item.Order)
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
        return _dbContext.CompanyLetterTemplates.AnyAsync(
            item => item.CompanyOwnerUserId == companyOwnerUserId
                && item.DefaultKey == null
                && !item.IsDeleted
                && (!exceptId.HasValue || item.Id != exceptId.Value)
                && item.Name.ToUpper() == normalizedName,
            cancellationToken);
    }

    private static void Apply(
        CompanyLetterTemplate template,
        SaveCompanyTemplateRequest request,
        DateTime now)
    {
        template.Name = request.Name;
        template.Audience = request.Audience;
        template.Category = request.Category;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.UpdatedAtUtc = now;
    }

    private static void Normalize(SaveCompanyTemplateRequest request)
    {
        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Audience = CompanyLetterTemplateCatalog.Audiences
            .FirstOrDefault(item => string.Equals(
                item,
                request.Audience?.Trim(),
                StringComparison.OrdinalIgnoreCase))
            ?? request.Audience?.Trim()
            ?? string.Empty;
        request.Category = request.Category?.Trim() ?? string.Empty;
        request.Subject = request.Subject?.Trim() ?? string.Empty;
        request.Body = request.Body?.Trim() ?? string.Empty;
    }

    private static string Validate(SaveCompanyTemplateRequest request)
    {
        if (request.ActorUserId <= 0)
            return "Employer sign in is required.";
        if (request.Name.Length is < 1 or > 120)
            return "Template name must be between 1 and 120 characters.";
        if (!CompanyLetterTemplateCatalog.Audiences.Contains(request.Audience))
            return "Select Candidate, Hiring Manager or Recruiter as the audience.";
        if (request.Category.Length is < 1 or > 80)
            return "Category must be between 1 and 80 characters.";
        if (request.Subject.Length is < 1 or > 250)
            return "Subject must be between 1 and 250 characters.";
        if (request.Body.Length is < 1 or > 10000)
            return "Message body must be between 1 and 10,000 characters.";
        return string.Empty;
    }

    private static CompanyTemplateDto ToDto(
        CompanyLetterTemplate template,
        CompanyLetterTemplateDefinition? definition = null)
    {
        return new CompanyTemplateDto
        {
            Id = definition?.Id ?? template.Id,
            DefaultKey = definition?.Key ?? template.DefaultKey ?? string.Empty,
            IsDefault = definition is not null,
            Name = template.Name,
            Audience = template.Audience,
            Category = template.Category,
            Subject = template.Subject,
            Body = template.Body,
            UpdatedAtUtc = template.UpdatedAtUtc
        };
    }

    private static CompanyTemplateDto ToDto(
        CompanyLetterTemplateDefinition definition)
    {
        return new CompanyTemplateDto
        {
            Id = definition.Id,
            DefaultKey = definition.Key,
            IsDefault = true,
            Name = definition.Name,
            Audience = definition.Audience,
            Category = definition.Category,
            Subject = definition.Subject,
            Body = definition.Body
        };
    }

    private static int AudienceOrder(string audience) => audience switch
    {
        CompanyLetterTemplateCatalog.Candidate => 0,
        CompanyLetterTemplateCatalog.HiringManager => 1,
        CompanyLetterTemplateCatalog.Recruiter => 2,
        _ => 3
    };

    private static CompanyTemplateResponse Successful(
        CompanyAccessContext access,
        string message,
        CompanyTemplateDto? template = null,
        List<CompanyTemplateDto>? templates = null)
    {
        return new CompanyTemplateResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = access.CompanyOwnerUserId,
            CanManageTemplates = access.CanManageTemplates,
            Template = template,
            Templates = templates ?? [],
            Variables = CompanyLetterTemplateCatalog.Variables.ToList()
        };
    }

    private static CompanyTemplateResponse Forbidden() => Failed(
        "You do not have access to manage company templates.",
        CompanyTemplateErrorCodes.Forbidden);

    private static CompanyTemplateResponse NotFound() => Failed(
        "Template was not found for this company.",
        CompanyTemplateErrorCodes.NotFound);

    private static CompanyTemplateResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyTemplateResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
