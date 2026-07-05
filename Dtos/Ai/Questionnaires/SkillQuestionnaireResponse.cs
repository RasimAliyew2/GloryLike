using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class SkillQuestionnaireResponse
{
    [JsonPropertyName("questionnaireId")]
    public Guid QuestionnaireId { get; set; }

    [JsonPropertyName("skill")]
    public string Skill { get; set; } = string.Empty;

    [JsonPropertyName("seniority")]
    public string Seniority { get; set; } = string.Empty;

    [JsonPropertyName("skillComplexity")]
    public string SkillComplexity { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<QuestionnaireQuestionDto> Questions { get; set; } = new();

    [JsonPropertyName("scoring")]
    public QuestionnaireScoringDto Scoring { get; set; } = new();
}
