using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeBackend.Dtos.Ai.Questionnaires;
using GloryLikeBackend.Services.Ai;
using GloryLikeBackend.Services.Interfaces;

namespace GloryLikeBackend.Services;

public class OpenAiSkillQuestionnaireGenerator : IOpenAiSkillQuestionnaireGenerator
{
    private readonly string _apiKey;
    private const string Model = "gpt-5.5";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenAiSkillQuestionnaireGenerator(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;

        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI API key tapılmadı.");
    }

    public async Task<SkillQuestionnaireResponse> GenerateQuestionnaireAsync(
        GenerateSkillQuestionnaireRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = Model,
            instructions = SkillQuestionnairePrompt.Prompt,
            input = $"""
Generate one reusable Skill Depth Questionnaire.

skill: {request.Skill}
skillComplexity: {request.SkillComplexity}
seniority: {request.Seniority}
language: {request.Language}

Return only the JSON object.
""",
            reasoning = new
            {
                effort = "low"
            },
            text = new
            {
                verbosity = "low",
                format = new
                {
                    type = "json_schema",
                    name = "skill_questionnaire_response",
                    strict = true,
                    schema = BuildResponseSchema()
                }
            },
            max_output_tokens = 5000
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = JsonContent.Create(payload)
        };

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI API error. StatusCode: {(int)httpResponse.StatusCode}. Body: {responseBody}");
        }

        var outputText = ExtractOutputText(responseBody);

        if (string.IsNullOrWhiteSpace(outputText))
            throw new InvalidOperationException("OpenAI response içində output text tapılmadı.");

        var result = JsonSerializer.Deserialize<SkillQuestionnaireResponse>(outputText, JsonOptions);

        if (result is null)
            throw new InvalidOperationException("OpenAI response JSON deserialize edilə bilmədi.");

        ValidateGeneratedQuestionnaire(result, request);

        result.Skill = request.Skill;
        result.Seniority = request.Seniority;
        result.SkillComplexity = request.SkillComplexity;

        return result;
    }

    private static object BuildResponseSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                skill = new { type = "string" },
                seniority = new
                {
                    type = "string",
                    @enum = new[] { "junior", "middle", "senior", "lead" }
                },
                skillComplexity = new
                {
                    type = "string",
                    @enum = new[] { "low", "medium", "high" }
                },
                questions = new
                {
                    type = "array",
                    minItems = 5,
                    maxItems = 7,
                    items = BuildQuestionSchema()
                },
                scoring = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        maxComplexity = new { type = "integer", minimum = 1 },
                        maxOwnership = new { type = "integer", minimum = 1 },
                        maxDepth = new { type = "integer", minimum = 1 }
                    },
                    required = new[]
                    {
                        "maxComplexity",
                        "maxOwnership",
                        "maxDepth"
                    }
                }
            },
            required = new[]
            {
                "skill",
                "seniority",
                "skillComplexity",
                "questions",
                "scoring"
            }
        };
    }

    private static object BuildQuestionSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                id = new { type = "string" },
                order = new { type = "integer", minimum = 1, maximum = 7 },
                dimension = new
                {
                    type = "string",
                    @enum = new[] { "context", "complexity", "ownership", "result" }
                },
                hiddenByDefault = new { type = "boolean" },
                text = new { type = "string" },
                type = new
                {
                    type = "string",
                    @enum = new[] { "single", "multi" }
                },
                options = new
                {
                    type = "array",
                    minItems = 3,
                    maxItems = 4,
                    items = BuildOptionSchema()
                },
                branching = new
                {
                    type = "array",
                    maxItems = 2,
                    items = BuildBranchingSchema()
                }
            },
            required = new[]
            {
                "id",
                "order",
                "dimension",
                "hiddenByDefault",
                "text",
                "type",
                "options",
                "branching"
            }
        };
    }

    private static object BuildOptionSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                id = new { type = "string" },
                label = new { type = "string" },
                weights = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        complexity = new { type = "integer", minimum = 0, maximum = 3 },
                        ownership = new { type = "integer", minimum = 0, maximum = 3 },
                        depth = new { type = "integer", minimum = 0, maximum = 3 }
                    },
                    required = new[]
                    {
                        "complexity",
                        "ownership",
                        "depth"
                    }
                }
            },
            required = new[]
            {
                "id",
                "label",
                "weights"
            }
        };
    }

    private static object BuildBranchingSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                ifOption = new { type = "string" },
                revealQuestionId = new { type = "string" }
            },
            required = new[]
            {
                "ifOption",
                "revealQuestionId"
            }
        };
    }

    private static string? ExtractOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var directOutputText) &&
            directOutputText.ValueKind == JsonValueKind.String)
        {
            return directOutputText.GetString();
        }

        if (!root.TryGetProperty("output", out var outputArray) ||
            outputArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentArray) ||
                contentArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var typeElement) &&
                    typeElement.ValueKind == JsonValueKind.String &&
                    typeElement.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    return textElement.GetString();
                }
            }
        }

        return null;
    }

    private static void ValidateGeneratedQuestionnaire(
        SkillQuestionnaireResponse questionnaire,
        GenerateSkillQuestionnaireRequest request)
    {
        var expectedCount = request.SkillComplexity switch
        {
            "low" => 5,
            "medium" => 6,
            "high" => 7,
            _ => 6
        };

        if (questionnaire.Questions.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"AI {questionnaire.Questions.Count} sual qaytardı, amma {request.SkillComplexity} üçün {expectedCount} sual lazımdır.");
        }

        var requiredDimensions = new HashSet<string>
        {
            "context",
            "complexity",
            "ownership",
            "result"
        };

        var actualDimensions = questionnaire.Questions
            .Select(q => q.Dimension.Trim().ToLowerInvariant())
            .ToHashSet();

        if (!requiredDimensions.All(actualDimensions.Contains))
            throw new InvalidOperationException("Questionnaire bütün dimension-ları əhatə etmir.");

        var questionIds = questionnaire.Questions.Select(q => q.Id).ToHashSet();

        if (questionIds.Count != questionnaire.Questions.Count)
            throw new InvalidOperationException("Question id-lər təkrarlandı.");

        var optionIds = questionnaire.Questions
            .SelectMany(q => q.Options)
            .Select(o => o.Id)
            .ToHashSet();

        foreach (var question in questionnaire.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.Id))
                throw new InvalidOperationException("Question id boşdur.");

            if (question.Options.Count is < 3 or > 4)
                throw new InvalidOperationException($"{question.Id} üçün option sayı 3-4 aralığında deyil.");

            foreach (var option in question.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Id))
                    throw new InvalidOperationException($"{question.Id} içində boş option id var.");

                if (string.IsNullOrWhiteSpace(option.Label))
                    throw new InvalidOperationException($"{option.Id} üçün label boşdur.");

                ValidateWeight(option.Weights.Complexity, option.Id, "complexity");
                ValidateWeight(option.Weights.Ownership, option.Id, "ownership");
                ValidateWeight(option.Weights.Depth, option.Id, "depth");
            }

            foreach (var branch in question.Branching)
            {
                if (!optionIds.Contains(branch.IfOption))
                    throw new InvalidOperationException($"{question.Id} branching invalid ifOption: {branch.IfOption}");

                if (!questionIds.Contains(branch.RevealQuestionId))
                    throw new InvalidOperationException($"{question.Id} branching invalid revealQuestionId: {branch.RevealQuestionId}");
            }
        }

        if (questionnaire.Scoring.MaxComplexity <= 0 ||
            questionnaire.Scoring.MaxOwnership <= 0 ||
            questionnaire.Scoring.MaxDepth <= 0)
        {
            throw new InvalidOperationException("Scoring max dəyərləri 0-dan böyük olmalıdır.");
        }
    }

    private static void ValidateWeight(int value, string optionId, string weightName)
    {
        if (value is < 0 or > 3)
            throw new InvalidOperationException($"{optionId} üçün {weightName} weight 0-3 aralığında olmalıdır.");
    }
}
