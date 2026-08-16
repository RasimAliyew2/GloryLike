namespace GloryLikeBackend.Models.Profile;

public sealed class UserJob
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
