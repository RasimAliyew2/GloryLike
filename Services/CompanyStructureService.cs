using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyStructure;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyStructureService : ICompanyStructureService
{
    private static readonly string[] StructureHeaders =
    [
        "Department",
        "Division",
        "Position"
    ];

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly IXlsxTableService _xlsxTableService;

    public CompanyStructureService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService,
        IXlsxTableService xlsxTableService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _xlsxTableService = xlsxTableService;
    }

    public async Task<CompanyStructureResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(actorUserId, cancellationToken);
        if (!ownerUserId.HasValue)
            return Forbidden();

        var departments = await LoadDepartmentsAsync(
            ownerUserId.Value,
            cancellationToken);

        return Successful(
            ownerUserId.Value,
            departments,
            departments.Count == 0
                ? "Company structure is empty."
                : "Company structure loaded.");
    }

    public async Task<CompanyStructureResponse> SaveAsync(
        SaveCompanyStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var validationMessage = Validate(request.Departments);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return Failed(
                validationMessage,
                CompanyStructureErrorCodes.Validation);
        }

        var ownerUserId = await ResolveOwnerUserIdAsync(
            request.ActorUserId,
            cancellationToken);
        if (!ownerUserId.HasValue)
            return Forbidden();

        return await ReplaceAsync(
            ownerUserId.Value,
            request.Departments,
            "Company structure saved.",
            cancellationToken);
    }

    public async Task<CompanyStructureResponse> ImportAsync(
        int actorUserId,
        Stream input,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(actorUserId, cancellationToken);
        if (!ownerUserId.HasValue)
            return Forbidden();

        IReadOnlyList<XlsxTableRow> rows;
        try
        {
            rows = _xlsxTableService.ReadSheet(
                input,
                "Structure",
                maxRows: 5001,
                maxColumns: StructureHeaders.Length);
        }
        catch (Exception exception) when (
            exception is InvalidDataException
            or IOException
            or System.Xml.XmlException)
        {
            return Failed(
                $"Structure Excel could not be read. {exception.Message}",
                CompanyStructureErrorCodes.Import);
        }

        if (rows.Count == 0)
        {
            return Failed(
                "Structure Excel is empty.",
                CompanyStructureErrorCodes.Import);
        }

        var headerError = ValidateHeaders(rows[0], StructureHeaders);
        if (!string.IsNullOrWhiteSpace(headerError))
            return Failed(headerError, CompanyStructureErrorCodes.Import);

        var departments = new List<SaveCompanyStructureDepartmentRequest>();
        foreach (var row in rows.Skip(1))
        {
            var departmentName = Cell(row, 0);
            var divisionName = Cell(row, 1);
            var positionName = Cell(row, 2);

            if (string.IsNullOrWhiteSpace(departmentName)
                && string.IsNullOrWhiteSpace(divisionName)
                && string.IsNullOrWhiteSpace(positionName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(departmentName))
            {
                return Failed(
                    $"Row {row.RowNumber}: Department is required.",
                    CompanyStructureErrorCodes.Import);
            }

            if (string.IsNullOrWhiteSpace(divisionName)
                && !string.IsNullOrWhiteSpace(positionName))
            {
                return Failed(
                    $"Row {row.RowNumber}: Division is required when a position is provided.",
                    CompanyStructureErrorCodes.Import);
            }

            var department = departments.FirstOrDefault(item =>
                NameEquals(item.Name, departmentName));
            if (department is null)
            {
                department = new SaveCompanyStructureDepartmentRequest
                {
                    Name = departmentName
                };
                departments.Add(department);
            }

            if (string.IsNullOrWhiteSpace(divisionName))
                continue;

            var division = department.Divisions.FirstOrDefault(item =>
                NameEquals(item.Name, divisionName));
            if (division is null)
            {
                division = new SaveCompanyStructureDivisionRequest
                {
                    Name = divisionName
                };
                department.Divisions.Add(division);
            }

            if (string.IsNullOrWhiteSpace(positionName)
                || division.Positions.Any(item => NameEquals(item.Name, positionName)))
            {
                continue;
            }

            division.Positions.Add(new SaveCompanyStructurePositionRequest
            {
                Name = positionName
            });
        }

        var request = new SaveCompanyStructureRequest
        {
            ActorUserId = actorUserId,
            Departments = departments
        };
        Normalize(request);
        var validationMessage = Validate(request.Departments);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return Failed(
                validationMessage,
                CompanyStructureErrorCodes.Import);
        }

        return await ReplaceAsync(
            ownerUserId.Value,
            request.Departments,
            $"{request.Departments.Count} departments imported from Excel.",
            cancellationToken);
    }

    public async Task<CompanyStructureExportResult> ExportAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(actorUserId, cancellationToken);
        if (!ownerUserId.HasValue)
        {
            return new CompanyStructureExportResult
            {
                Success = false,
                Message = "You do not have access to this company's structure.",
                ErrorCode = CompanyStructureErrorCodes.Forbidden
            };
        }

        var departments = await LoadDepartmentsAsync(
            ownerUserId.Value,
            cancellationToken);
        var rows = new List<IReadOnlyList<string>>();

        foreach (var department in departments)
        {
            if (department.Divisions.Count == 0)
            {
                rows.Add([department.Name, string.Empty, string.Empty]);
                continue;
            }

            foreach (var division in department.Divisions)
            {
                if (division.Positions.Count == 0)
                {
                    rows.Add([department.Name, division.Name, string.Empty]);
                    continue;
                }

                rows.AddRange(division.Positions.Select(position =>
                    (IReadOnlyList<string>)
                    [department.Name, division.Name, position.Name]));
            }
        }

        while (rows.Count < 25)
            rows.Add([string.Empty, string.Empty, string.Empty]);

        return new CompanyStructureExportResult
        {
            Success = true,
            Message = "Company structure Excel created.",
            Content = _xlsxTableService.CreateWorkbook(
                "Structure",
                StructureHeaders,
                rows)
        };
    }

    private async Task<CompanyStructureResponse> ReplaceAsync(
        int ownerUserId,
        IReadOnlyList<SaveCompanyStructureDepartmentRequest> departments,
        string message,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        try
        {
            await _dbContext.CompanyStructureDepartments
                .Where(item => item.CompanyOwnerUserId == ownerUserId)
                .ExecuteDeleteAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var entities = departments.Select((department, departmentIndex) =>
                new CompanyStructureDepartment
                {
                    CompanyOwnerUserId = ownerUserId,
                    Name = department.Name,
                    SortOrder = departmentIndex + 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Divisions = department.Divisions.Select((division, divisionIndex) =>
                        new CompanyStructureDivision
                        {
                            Name = division.Name,
                            SortOrder = divisionIndex + 1,
                            Positions = division.Positions.Select((position, positionIndex) =>
                                new CompanyStructurePosition
                                {
                                    Name = position.Name,
                                    SortOrder = positionIndex + 1
                                }).ToList()
                        }).ToList()
                }).ToList();

            _dbContext.CompanyStructureDepartments.AddRange(entities);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Successful(ownerUserId, entities, message);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failed(
                "Company structure could not be saved because duplicate or invalid data was found.",
                CompanyStructureErrorCodes.Persistence);
        }
    }

    private async Task<List<CompanyStructureDepartment>> LoadDepartmentsAsync(
        int ownerUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CompanyStructureDepartments
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.CompanyOwnerUserId == ownerUserId)
            .Include(item => item.Divisions)
                .ThenInclude(item => item.Positions)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
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

    private static void Normalize(SaveCompanyStructureRequest request)
    {
        request.Departments ??= new();
        foreach (var department in request.Departments)
        {
            department.Name = NormalizeName(department.Name);
            department.Divisions ??= new();
            foreach (var division in department.Divisions)
            {
                division.Name = NormalizeName(division.Name);
                division.Positions ??= new();
                foreach (var position in division.Positions)
                    position.Name = NormalizeName(position.Name);
            }
        }
    }

    private static string Validate(
        IReadOnlyList<SaveCompanyStructureDepartmentRequest> departments)
    {
        if (departments.Count > 200)
            return "A company structure can contain at most 200 departments.";

        var duplicateDepartment = departments
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDepartment is not null)
            return $"Department '{duplicateDepartment.Key}' is duplicated.";

        var divisionCount = 0;
        var positionCount = 0;
        foreach (var department in departments)
        {
            if (string.IsNullOrWhiteSpace(department.Name))
                return "Department name is required.";
            if (department.Name.Length > 120)
                return $"Department '{department.Name}' is longer than 120 characters.";

            divisionCount += department.Divisions.Count;
            var duplicateDivision = department.Divisions
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateDivision is not null)
            {
                return $"Division '{duplicateDivision.Key}' is duplicated in department '{department.Name}'.";
            }

            foreach (var division in department.Divisions)
            {
                if (string.IsNullOrWhiteSpace(division.Name))
                    return $"Division name is required in department '{department.Name}'.";
                if (division.Name.Length > 120)
                    return $"Division '{division.Name}' is longer than 120 characters.";

                positionCount += division.Positions.Count;
                var duplicatePosition = division.Positions
                    .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(group => group.Count() > 1);
                if (duplicatePosition is not null)
                {
                    return $"Position '{duplicatePosition.Key}' is duplicated in division '{division.Name}'.";
                }

                foreach (var position in division.Positions)
                {
                    if (string.IsNullOrWhiteSpace(position.Name))
                        return $"Position name is required in division '{division.Name}'.";
                    if (position.Name.Length > 160)
                        return $"Position '{position.Name}' is longer than 160 characters.";
                }
            }
        }

        if (divisionCount > 1000)
            return "A company structure can contain at most 1000 divisions.";
        if (positionCount > 5000)
            return "A company structure can contain at most 5000 positions.";

        return string.Empty;
    }

    private static string ValidateHeaders(
        XlsxTableRow row,
        IReadOnlyList<string> expectedHeaders)
    {
        for (var index = 0; index < expectedHeaders.Count; index++)
        {
            var actual = NormalizeHeader(Cell(row, index));
            if (!string.Equals(
                    actual,
                    NormalizeHeader(expectedHeaders[index]),
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"Column {index + 1} must be named '{expectedHeaders[index]}'. Do not rename or reorder the columns.";
            }
        }

        return string.Empty;
    }

    private static string Cell(XlsxTableRow row, int index) =>
        index < row.Cells.Count ? NormalizeName(row.Cells[index]) : string.Empty;

    private static string NormalizeHeader(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    private static string NormalizeName(string? value) =>
        string.Join(
            ' ',
            (value ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

    private static bool NameEquals(string left, string right) =>
        string.Equals(
            NormalizeName(left),
            NormalizeName(right),
            StringComparison.OrdinalIgnoreCase);

    private static CompanyStructureResponse Successful(
        int ownerUserId,
        IEnumerable<CompanyStructureDepartment> departments,
        string message)
    {
        return new CompanyStructureResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = ownerUserId,
            Departments = departments
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .Select(ToDto)
                .ToList()
        };
    }

    private static CompanyStructureDepartmentDto ToDto(
        CompanyStructureDepartment department)
    {
        return new CompanyStructureDepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            SortOrder = department.SortOrder,
            Divisions = department.Divisions
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .Select(division => new CompanyStructureDivisionDto
                {
                    Id = division.Id,
                    Name = division.Name,
                    SortOrder = division.SortOrder,
                    Positions = division.Positions
                        .OrderBy(item => item.SortOrder)
                        .ThenBy(item => item.Name)
                        .Select(position => new CompanyStructurePositionDto
                        {
                            Id = position.Id,
                            Name = position.Name,
                            SortOrder = position.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static CompanyStructureResponse Forbidden() => Failed(
        "You do not have access to this company's structure.",
        CompanyStructureErrorCodes.Forbidden);

    private static CompanyStructureResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyStructureResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
