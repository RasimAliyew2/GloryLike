using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class QuestionnaireOptionWeightsDto
{
    [JsonPropertyName("complexity")]
    public int Complexity { get; set; }

    [JsonPropertyName("ownership")]
    public int Ownership { get; set; }

    [JsonPropertyName("depth")]
    public int Depth { get; set; }
}
