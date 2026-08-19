namespace GloryLikeBackend.Models;

public sealed class CompanyLocation
{
    public int Id { get; set; }

    public int CompanyProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public CompanyProfile CompanyProfile { get; set; } = null!;
}
