using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Assessments;

public class SkillDepthAnswerDto
{
    [Required]
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("selectedOptionIds")]
    public List<string> SelectedOptionIds { get; set; } = new();
}
