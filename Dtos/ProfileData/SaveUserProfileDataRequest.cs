namespace GloryLikeBackend.Dtos.ProfileData;

public class SaveUserProfileDataRequest
{
    public int UserId { get; set; }
    public List<UserSkillProfileDto> Skills { get; set; } = new();
    public List<UserWorkExperienceProfileDto> Experiences { get; set; } = new();
}
