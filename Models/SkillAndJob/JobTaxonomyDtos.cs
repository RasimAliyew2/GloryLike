namespace GloryLikeBackend.Models.SkillAndJob;

public sealed class JobFamilyTaxonomyDto
{
    public int Id { get; set; }

    public string JobName { get; set; } = string.Empty;

    public List<PositionTaxonomyDto> Positions { get; set; } = new();
}

public sealed class PositionTaxonomyDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }

    public List<SeniorityTaxonomyDto> Seniorities { get; set; } = new();
}

public sealed class SeniorityTaxonomyDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public List<SkillTaxonomyDto> Skills { get; set; } = new();
}

public sealed class SkillTaxonomyDto
{
    public int Id { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
}

public sealed class SeniorityOptionDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
