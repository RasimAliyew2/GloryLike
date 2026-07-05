using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class QuestionnaireQuestionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;

    [JsonPropertyName("hiddenByDefault")]
    public bool HiddenByDefault { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "single";

    [JsonPropertyName("options")]
    public List<QuestionnaireOptionDto> Options { get; set; } = new();

    [JsonPropertyName("branching")]
    public List<QuestionnaireBranchingRuleDto> Branching { get; set; } = new();
}
