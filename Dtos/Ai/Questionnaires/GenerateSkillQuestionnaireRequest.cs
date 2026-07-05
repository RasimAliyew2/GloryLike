using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GloryLikeBackend.Dtos.Ai.Questionnaires;

public class GenerateSkillQuestionnaireRequest
{
    [Required]
    [MaxLength(150)]
    [JsonPropertyName("skill")]
    public string Skill { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [JsonPropertyName("skillComplexity")]
    public string SkillComplexity { get; set; } = "medium";

    [Required]
    [MaxLength(20)]
    [JsonPropertyName("seniority")]
    public string Seniority { get; set; } = "middle";

    [MaxLength(10)]
    [JsonPropertyName("language")]
    public string Language { get; set; } = "az";
}
