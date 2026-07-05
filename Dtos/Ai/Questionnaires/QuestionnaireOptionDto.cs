using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class QuestionnaireOptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("weights")]
    public QuestionnaireOptionWeightsDto Weights { get; set; } = new();
}
