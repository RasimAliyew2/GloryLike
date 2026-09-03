namespace GloryLikeBackend.Models;

public sealed class CompanyLetterTemplate
{
    public Guid Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public string? DefaultKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User CompanyOwnerUser { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
