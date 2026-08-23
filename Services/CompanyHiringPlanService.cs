using System.Globalization;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyHiringPlan;
using GloryLikeBackend.Models;
using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyHiringPlanService : ICompanyHiringPlanService
{
    private static readonly string[] ImportHeaders =
    [
        "Position Title",
        "Department",
        "Headcount",
        "Seniority",
        "Priority",
        "Target Start Date",
        "Employment Type",
        "Status",
        "Notes"
    ];

    private static readonly HashSet<string> FinishedVacancyStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Closed",
            "Finished",
            "Filled"
        };

    private static readonly HashSet<string> Priorities =
        new(StringComparer.Ordinal)
        {
            "Critical",
            "High",
            "Medium",
            "Low"
        };

    private static readonly HashSet<string> EmploymentTypes =
        new(StringComparer.Ordinal)
        {
            "Full-time",
            "Part-time",
            "Contract",
            "Temporary",
            "Internship"
        };

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly IXlsxTableService _xlsxTableService;

    public CompanyHiringPlanService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService,
        IXlsxTableService xlsxTableService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _xlsxTableService = xlsxTableService;
    }

    public async Task<CompanyHiringPlanResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plans = await BaseQuery()
            .Where(item => item.CompanyOwnerUserId == ownerUserId.Value)
            .OrderBy(item => item.TargetStartDate)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = plans.Count == 0
                ? "Hiring plan is empty."
                : $"{plans.Count} hiring plan rows loaded.",
            CompanyOwnerUserId = ownerUserId.Value,
            Plans = plans.Select(ToDto).ToList()
        };
    }

    public async Task<CompanyHiringPlanResponse> GetByIdAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plan = await BaseQuery().FirstOrDefaultAsync(
            item => item.Id == planId
                && item.CompanyOwnerUserId == ownerUserId.Value,
            cancellationToken);

        return plan is null
            ? NotFound()
            : Successful(ownerUserId.Value, ToDto(plan), "Hiring plan row loaded.");
    }

    public Task<CompanyHiringPlanResponse> CreateAsync(
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(null, request, cancellationToken);
    }

    public Task<CompanyHiringPlanResponse> UpdateAsync(
        int planId,
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(planId, request, cancellationToken);
    }

    public async Task<CompanyHiringPlanResponse> DeleteAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plan = await _dbContext.CompanyHiringPlans
            .Include(item => item.Vacancies)
            .FirstOrDefaultAsync(
                item => item.Id == planId
                    && item.CompanyOwnerUserId == ownerUserId.Value,
                cancellationToken);

        if (plan is null)
            return NotFound();

        foreach (var vacancy in plan.Vacancies)
        {
            vacancy.HiringPlanId = null;
            vacancy.HiringPlan = null;
        }

        _dbContext.CompanyHiringPlans.Remove(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = "Hiring plan row deleted.",
            CompanyOwnerUserId = ownerUserId.Value
        };
    }

    public async Task<CompanyHiringPlanResponse> ImportAsync(
        int actorUserId,
        Stream input,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);
        if (!ownerUserId.HasValue)
            return Forbidden();

        IReadOnlyList<XlsxTableRow> rows;
        try
        {
            rows = _xlsxTableService.ReadSheet(
                input,
                "HiringPlan",
                maxRows: 5001,
                maxColumns: ImportHeaders.Length);
        }
        catch (Exception exception) when (
            exception is InvalidDataException
            or IOException
            or System.Xml.XmlException)
        {
            return Failed(
                $"Hiring Plan Excel could not be read. {exception.Message}",
                CompanyHiringPlanErrorCodes.Validation);
        }

        if (rows.Count == 0)
        {
            return Failed(
                "Hiring Plan Excel is empty.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        var headerError = ValidateImportHeaders(rows[0]);
        if (!string.IsNullOrWhiteSpace(headerError))
            return Failed(headerError, CompanyHiringPlanErrorCodes.Validation);

        var structure = await _dbContext.CompanyStructureDepartments
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.CompanyOwnerUserId == ownerUserId.Value)
            .Include(item => item.Divisions)
                .ThenInclude(item => item.Positions)
            .ToListAsync(cancellationToken);

        var availableSeniorities = await _dbContext.Seniorities
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        if (availableSeniorities.Count == 0)
        {
            return Failed(
                "No seniority levels exist in SQL.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        var taxonomyRows = await (
            from jobFamily in _dbContext.JobFamilies.AsNoTracking()
            join position in _dbContext.Positions.AsNoTracking()
                on jobFamily.Id equals position.JobFamilyId
            join link in _dbContext.PositionSeniorities.AsNoTracking()
                on position.Id equals link.PositionId
            join seniority in _dbContext.Seniorities.AsNoTracking()
                on link.SeniorityId equals seniority.Id
            select new
            {
                JobFamily = jobFamily,
                Position = position,
                Seniority = seniority
            })
            .ToListAsync(cancellationToken);
        var taxonomy = taxonomyRows
            .Select(item => new TaxonomyImportOption(
                item.JobFamily,
                item.Position,
                item.Seniority))
            .ToList();

        var parsedRows = new List<ParsedImportRow>();
        foreach (var row in rows.Skip(1))
        {
            var values = row.Cells
                .Take(ImportHeaders.Length)
                .Select(NormalizeExcelCell)
                .ToArray();

            if (values.All(string.IsNullOrWhiteSpace)
                || values[0].StartsWith("↑", StringComparison.Ordinal)
                || values[0].Contains("Rows 2", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var positionName = values[0];
            var departmentName = values[1];
            if (string.IsNullOrWhiteSpace(positionName))
            {
                return RowError(row.RowNumber, "Position Title is required.");
            }
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                return RowError(row.RowNumber, "Department is required.");
            }

            var department = structure.FirstOrDefault(item =>
                NameEquals(item.Name, departmentName));
            if (department is null)
            {
                return RowError(
                    row.RowNumber,
                    $"Department '{departmentName}' does not exist in your company structure.");
            }

            var structurePosition = department.Divisions
                .SelectMany(item => item.Positions)
                .FirstOrDefault(item => NameEquals(item.Name, positionName));
            if (structurePosition is null)
            {
                return RowError(
                    row.RowNumber,
                    $"Position '{positionName}' does not exist under department '{departmentName}' in your company structure.");
            }

            var taxonomyOptions = taxonomy
                .Where(item => NameEquals(item.Position.Name, positionName))
                .OrderByDescending(item =>
                    NameEquals(item.JobFamily.JobName, departmentName))
                .ThenBy(item => item.JobFamily.JobName)
                .ThenBy(item => item.Seniority.SortOrder)
                .ToList();

            if (!int.TryParse(
                    values[2],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var headcount)
                || headcount is < 1 or > 1000)
            {
                return RowError(
                    row.RowNumber,
                    "Headcount must be a whole number between 1 and 1000.");
            }

            Seniority? selectedSeniority;
            if (string.IsNullOrWhiteSpace(values[3]))
            {
                selectedSeniority = availableSeniorities[0];
            }
            else
            {
                selectedSeniority = availableSeniorities.FirstOrDefault(item =>
                    NameEquals(item.Name, values[3]));
                if (selectedSeniority is null)
                {
                    return RowError(
                        row.RowNumber,
                        $"Seniority '{values[3]}' does not exist in SQL.");
                }
            }

            var selectedTaxonomy = taxonomyOptions.FirstOrDefault(item =>
                item.Seniority.Id == selectedSeniority!.Id);

            var priority = string.IsNullOrWhiteSpace(values[4])
                ? "Medium"
                : CanonicalValue(values[4], Priorities);
            if (string.IsNullOrWhiteSpace(priority))
            {
                return RowError(
                    row.RowNumber,
                    "Priority must be Low, Medium, High, or Critical.");
            }

            if (!TryParseExcelDate(values[5], out var targetStartDate))
            {
                return RowError(
                    row.RowNumber,
                    "Target Start Date must use YYYY-MM-DD format.");
            }

            var employmentType = string.IsNullOrWhiteSpace(values[6])
                ? "Full-time"
                : CanonicalValue(values[6], EmploymentTypes);
            if (string.IsNullOrWhiteSpace(employmentType))
            {
                return RowError(
                    row.RowNumber,
                    "Employment Type must be Full-time, Part-time, Contract, Temporary, or Internship.");
            }

            if (!string.IsNullOrWhiteSpace(values[7])
                && !new[] { "Planned", "In Progress", "Finished", "Filled" }
                    .Any(item => NameEquals(item, values[7])))
            {
                return RowError(
                    row.RowNumber,
                    "Status must be Planned, In Progress, Finished, or Filled.");
            }

            // Hiring Plan status is intentionally recalculated from linked vacancies.
            // The Excel value is validated for template integrity but is not persisted.

            if (values[8].Length > 1000)
                return RowError(row.RowNumber, "Notes can contain at most 1000 characters.");

            parsedRows.Add(new ParsedImportRow(
                row.RowNumber,
                department.Name,
                structurePosition.Name,
                selectedSeniority!,
                selectedTaxonomy,
                headcount,
                priority,
                targetStartDate,
                employmentType,
                values[8]));
        }

        if (parsedRows.Count == 0)
        {
            return Failed(
                "Hiring Plan Excel does not contain any data rows.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var entities = parsedRows.Select(row => new CompanyHiringPlan
            {
                CompanyOwnerUserId = ownerUserId.Value,
                CreatedByUserId = actorUserId,
                DepartmentName = row.DepartmentName,
                PositionName = row.PositionName,
                JobFamilyId = row.Taxonomy?.JobFamily.Id,
                PositionId = row.Taxonomy?.Position.Id,
                SeniorityId = row.Seniority.Id,
                Headcount = row.Headcount,
                Priority = row.Priority,
                TargetStartDate = row.TargetStartDate,
                EmploymentType = row.EmploymentType,
                Notes = row.Notes,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }).ToList();

            _dbContext.CompanyHiringPlans.AddRange(entities);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            for (var index = 0; index < entities.Count; index++)
            {
                entities[index].JobFamily = parsedRows[index].Taxonomy?.JobFamily;
                entities[index].Position = parsedRows[index].Taxonomy?.Position;
                entities[index].Seniority = parsedRows[index].Seniority;
            }

            return new CompanyHiringPlanResponse
            {
                Success = true,
                Message = $"{entities.Count} hiring plan rows imported successfully.",
                CompanyOwnerUserId = ownerUserId.Value,
                Plans = entities.Select(ToDto).ToList()
            };
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failed(
                "No hiring plan rows were imported because SQL rejected the workbook data.",
                CompanyHiringPlanErrorCodes.Conflict);
        }
    }

    private async Task<CompanyHiringPlanResponse> SaveAsync(
        int? planId,
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken)
    {
        Normalize(request);
        var validationMessage = Validate(request);

        if (!string.IsNullOrWhiteSpace(validationMessage))
            return Failed(validationMessage, CompanyHiringPlanErrorCodes.Validation);

        var ownerUserId = await ResolveOwnerUserIdAsync(
            request.ActorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var selectedSeniority = await _dbContext.Seniorities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.SeniorityId,
                cancellationToken);
        if (selectedSeniority is null)
        {
            return Failed(
                "The selected seniority does not exist in SQL.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        var structureDepartments = await _dbContext.CompanyStructureDepartments
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.CompanyOwnerUserId == ownerUserId.Value)
            .Include(item => item.Divisions)
                .ThenInclude(item => item.Positions)
            .ToListAsync(cancellationToken);
        var structureDepartment = structureDepartments.FirstOrDefault(item =>
            NameEquals(item.Name, request.DepartmentName));
        if (structureDepartment is null)
        {
            return Failed(
                $"Department '{request.DepartmentName}' does not exist in your company structure.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        var structurePosition = structureDepartment.Divisions
                .SelectMany(item => item.Positions)
                .FirstOrDefault(item => NameEquals(item.Name, request.PositionName));
        if (structurePosition is null)
        {
            return Failed(
                $"Position '{request.PositionName}' does not exist under department '{structureDepartment.Name}' in your company structure.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        var taxonomyCandidateRows = await (
            from jobFamily in _dbContext.JobFamilies.AsNoTracking()
            join position in _dbContext.Positions.AsNoTracking()
                on jobFamily.Id equals position.JobFamilyId
            join link in _dbContext.PositionSeniorities.AsNoTracking()
                on position.Id equals link.PositionId
            where link.SeniorityId == selectedSeniority.Id
            select new
            {
                JobFamily = jobFamily,
                Position = position
            })
            .ToListAsync(cancellationToken);
        var taxonomyCandidates = taxonomyCandidateRows
            .Select(item => new TaxonomyImportOption(
                item.JobFamily,
                item.Position,
                selectedSeniority))
            .ToList();
        var taxonomy = taxonomyCandidates
            .Where(item => NameEquals(item.Position.Name, structurePosition.Name))
            .OrderByDescending(item =>
                NameEquals(item.JobFamily.JobName, structureDepartment.Name))
            .ThenBy(item => item.JobFamily.JobName)
            .FirstOrDefault();

        CompanyHiringPlan plan;
        var now = DateTime.UtcNow;

        if (planId.HasValue)
        {
            var existingPlan = await _dbContext.CompanyHiringPlans
                .Include(item => item.JobFamily)
                .Include(item => item.Position)
                .Include(item => item.Seniority)
                .Include(item => item.Vacancies)
                .FirstOrDefaultAsync(
                    item => item.Id == planId.Value
                        && item.CompanyOwnerUserId == ownerUserId.Value,
                    cancellationToken);

            if (existingPlan is null)
                return NotFound();

            plan = existingPlan;

            if (request.Headcount < plan.Vacancies.Count)
            {
                return Failed(
                    $"Headcount cannot be lower than the {plan.Vacancies.Count} linked vacancies.",
                    CompanyHiringPlanErrorCodes.Conflict);
            }

            if (plan.Vacancies.Count > 0
                && (!NameEquals(request.DepartmentName, plan.DepartmentName)
                    || !NameEquals(request.PositionName, plan.PositionName)
                    || request.SeniorityId != plan.SeniorityId))
            {
                return Failed(
                    "Department, position, and seniority cannot be changed after a vacancy is linked.",
                    CompanyHiringPlanErrorCodes.Conflict);
            }
        }
        else
        {
            plan = new CompanyHiringPlan
            {
                CompanyOwnerUserId = ownerUserId.Value,
                CreatedByUserId = request.ActorUserId,
                CreatedAtUtc = now
            };
            _dbContext.CompanyHiringPlans.Add(plan);
        }

        plan.DepartmentName = structureDepartment.Name;
        plan.PositionName = structurePosition.Name;
        if (plan.Vacancies.Count == 0)
        {
            plan.JobFamily = null;
            plan.Position = null;
            plan.JobFamilyId = taxonomy?.JobFamily.Id;
            plan.PositionId = taxonomy?.Position.Id;
        }
        plan.SeniorityId = request.SeniorityId;
        plan.Headcount = request.Headcount;
        plan.Priority = request.Priority;
        plan.TargetStartDate = request.TargetStartDate?.Date;
        plan.EmploymentType = request.EmploymentType;
        plan.Notes = request.Notes ?? string.Empty;
        plan.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (plan.Vacancies.Count == 0)
        {
            plan.JobFamily = taxonomy?.JobFamily;
            plan.Position = taxonomy?.Position;
        }
        plan.Seniority = selectedSeniority;
        plan.Vacancies ??= new();

        return Successful(
            ownerUserId.Value,
            ToDto(plan),
            planId.HasValue
                ? "Hiring plan row updated."
                : "Hiring plan row created.");
    }

    private IQueryable<CompanyHiringPlan> BaseQuery()
    {
        return _dbContext.CompanyHiringPlans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.JobFamily)
            .Include(item => item.Position)
            .Include(item => item.Seniority)
            .Include(item => item.Vacancies);
    }

    private async Task<int?> ResolveOwnerUserIdAsync(
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0)
            return null;

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);
        return access?.CompanyOwnerUserId;
    }

    private static CompanyHiringPlanDto ToDto(CompanyHiringPlan plan)
    {
        var vacancyCount = plan.Vacancies.Count;
        var finishedCount = plan.Vacancies.Count(vacancy =>
            FinishedVacancyStatuses.Contains(vacancy.Status));
        var status = vacancyCount == 0
            ? CompanyHiringPlanStatuses.Planned
            : vacancyCount >= plan.Headcount && finishedCount == vacancyCount
                ? CompanyHiringPlanStatuses.Finished
                : CompanyHiringPlanStatuses.InProgress;

        return new CompanyHiringPlanDto
        {
            Id = plan.Id,
            JobFamilyId = plan.JobFamilyId,
            JobFamilyName = plan.JobFamily?.JobName ?? string.Empty,
            DepartmentName = string.IsNullOrWhiteSpace(plan.DepartmentName)
                ? plan.JobFamily?.JobName ?? string.Empty
                : plan.DepartmentName,
            PositionId = plan.PositionId,
            PositionName = string.IsNullOrWhiteSpace(plan.PositionName)
                ? plan.Position?.Name ?? string.Empty
                : plan.PositionName,
            SeniorityId = plan.SeniorityId,
            SeniorityName = plan.Seniority.Name,
            Headcount = plan.Headcount,
            Priority = plan.Priority,
            TargetStartDate = plan.TargetStartDate,
            EmploymentType = plan.EmploymentType,
            Notes = plan.Notes,
            Status = status,
            VacancyCount = vacancyCount,
            FinishedVacancyCount = finishedCount,
            RemainingVacancyCount = Math.Max(0, plan.Headcount - vacancyCount),
            CanCreateVacancy = vacancyCount < plan.Headcount,
            CreatedAtUtc = plan.CreatedAtUtc,
            UpdatedAtUtc = plan.UpdatedAtUtc,
            Vacancies = plan.Vacancies
                .OrderBy(vacancy => vacancy.CreatedAtUtc)
                .Select(vacancy => new CompanyHiringPlanVacancyDto
                {
                    VacancyId = vacancy.Id,
                    PlatformVacancyId = vacancy.PlatformVacancyId,
                    RoleTitle = vacancy.RoleTitle,
                    Status = vacancy.Status
                })
                .ToList()
        };
    }

    private static void Normalize(SaveCompanyHiringPlanRequest request)
    {
        request.DepartmentName = NormalizeExcelCell(request.DepartmentName);
        request.PositionName = NormalizeExcelCell(request.PositionName);
        request.Priority = request.Priority?.Trim() ?? string.Empty;
        request.EmploymentType = request.EmploymentType?.Trim() ?? string.Empty;
        request.Notes = request.Notes?.Trim();
    }

    private static string ValidateImportHeaders(XlsxTableRow row)
    {
        for (var index = 0; index < ImportHeaders.Length; index++)
        {
            var actual = NormalizeHeader(index < row.Cells.Count
                ? row.Cells[index]
                : string.Empty);
            if (!string.Equals(
                    actual,
                    NormalizeHeader(ImportHeaders[index]),
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"Column {index + 1} must be named '{ImportHeaders[index]}'. Do not rename or reorder the columns.";
            }
        }

        return string.Empty;
    }

    private static CompanyHiringPlanResponse RowError(
        int rowNumber,
        string message)
    {
        return Failed(
            $"Row {rowNumber}: {message}",
            CompanyHiringPlanErrorCodes.Validation);
    }

    private static string CanonicalValue(
        string value,
        IEnumerable<string> availableValues)
    {
        return availableValues.FirstOrDefault(item => NameEquals(item, value))
            ?? string.Empty;
    }

    private static bool TryParseExcelDate(
        string value,
        out DateTime? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactDate))
        {
            result = exactDate.Date;
            return true;
        }

        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var serialDate)
            && serialDate is > 1 and < 2958466)
        {
            try
            {
                result = DateTime.FromOADate(serialDate).Date;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return false;
    }

    private static string NormalizeExcelCell(string? value) =>
        string.Join(
            ' ',
            (value ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeHeader(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    private static bool NameEquals(string left, string right) =>
        string.Equals(
            NormalizeExcelCell(left),
            NormalizeExcelCell(right),
            StringComparison.OrdinalIgnoreCase);

    private static string Validate(SaveCompanyHiringPlanRequest request)
    {
        if (request.ActorUserId <= 0 || request.SeniorityId <= 0)
        {
            return "Actor and seniority are required.";
        }

        if (string.IsNullOrWhiteSpace(request.DepartmentName)
            || request.DepartmentName.Length > 120)
        {
            return "Department is required and can contain at most 120 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.PositionName)
            || request.PositionName.Length > 160)
        {
            return "Position is required and can contain at most 160 characters.";
        }

        if (request.Headcount is < 1 or > 1000)
            return "Headcount must be between 1 and 1000.";

        if (!Priorities.Contains(request.Priority))
            return "Priority must be Critical, High, Medium, or Low.";

        if (!EmploymentTypes.Contains(request.EmploymentType))
            return "Employment type is not supported.";

        if ((request.Notes?.Length ?? 0) > 1000)
            return "Notes can contain at most 1000 characters.";

        return string.Empty;
    }

    private static CompanyHiringPlanResponse Successful(
        int ownerUserId,
        CompanyHiringPlanDto plan,
        string message)
    {
        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = ownerUserId,
            Plan = plan
        };
    }

    private static CompanyHiringPlanResponse Forbidden() => Failed(
        "You do not have access to this company's hiring plan.",
        CompanyHiringPlanErrorCodes.Forbidden);

    private static CompanyHiringPlanResponse NotFound() => Failed(
        "Hiring plan row was not found.",
        CompanyHiringPlanErrorCodes.NotFound);

    private static CompanyHiringPlanResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyHiringPlanResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private sealed record TaxonomyImportOption(
        JobFamily JobFamily,
        Position Position,
        Seniority Seniority);

    private sealed record ParsedImportRow(
        int RowNumber,
        string DepartmentName,
        string PositionName,
        Seniority Seniority,
        TaxonomyImportOption? Taxonomy,
        int Headcount,
        string Priority,
        DateTime? TargetStartDate,
        string EmploymentType,
        string Notes);
}
