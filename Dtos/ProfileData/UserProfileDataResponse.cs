namespace GloryLikeBackend.Dtos.ProfileData;

public class UserProfileDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<UserSkillProfileDto> Skills { get; set; } = new();
    public List<UserWorkExperienceProfileDto> Experiences { get; set; } = new();
}
