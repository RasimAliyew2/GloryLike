using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Assessments;

public class SubmitSkillDepthAssessmentRequest
{
    [Required]
    [JsonPropertyName("questionnaireId")]
    public Guid QuestionnaireId { get; set; }

    [MaxLength(150)]
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [MaxLength(30)]
    [JsonPropertyName("companyType")]
    public string? CompanyType { get; set; }

    [Required]
    [MinLength(1)]
    [JsonPropertyName("answers")]
    public List<SkillDepthAnswerDto> Answers { get; set; } = new();
}
