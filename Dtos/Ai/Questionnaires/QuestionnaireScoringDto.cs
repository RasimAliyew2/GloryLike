using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class QuestionnaireScoringDto
{
    [JsonPropertyName("maxComplexity")]
    public int MaxComplexity { get; set; }

    [JsonPropertyName("maxOwnership")]
    public int MaxOwnership { get; set; }

    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; }
}
