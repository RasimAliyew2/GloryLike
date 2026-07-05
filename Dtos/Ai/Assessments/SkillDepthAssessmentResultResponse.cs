using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Assessments;

public class SkillDepthAssessmentResultResponse
{
    [JsonPropertyName("questionnaireId")]
    public Guid QuestionnaireId { get; set; }

    [JsonPropertyName("skill")]
    public string Skill { get; set; } = string.Empty;

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("companyType")]
    public string? CompanyType { get; set; }

    [JsonPropertyName("rawComplexity")]
    public int RawComplexity { get; set; }

    [JsonPropertyName("rawOwnership")]
    public int RawOwnership { get; set; }

    [JsonPropertyName("rawDepth")]
    public int RawDepth { get; set; }

    [JsonPropertyName("maxComplexity")]
    public int MaxComplexity { get; set; }

    [JsonPropertyName("maxOwnership")]
    public int MaxOwnership { get; set; }

    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; }

    [JsonPropertyName("complexityRatio")]
    public double ComplexityRatio { get; set; }

    [JsonPropertyName("ownershipRatio")]
    public double OwnershipRatio { get; set; }

    [JsonPropertyName("depthRatio")]
    public double DepthRatio { get; set; }

    [JsonPropertyName("depthScore")]
    public int DepthScore { get; set; }

    [JsonPropertyName("taskComplexity")]
    public string TaskComplexity { get; set; } = string.Empty;

    [JsonPropertyName("ownershipLevel")]
    public string OwnershipLevel { get; set; } = string.Empty;

    [JsonPropertyName("depthTier")]
    public string DepthTier { get; set; } = string.Empty;

    [JsonPropertyName("answeredQuestionCount")]
    public int AnsweredQuestionCount { get; set; }
}
