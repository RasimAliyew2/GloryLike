using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class QuestionnaireBranchingRuleDto
{
    [JsonPropertyName("ifOption")]
    public string IfOption { get; set; } = string.Empty;

    [JsonPropertyName("revealQuestionId")]
    public string RevealQuestionId { get; set; } = string.Empty;
}
