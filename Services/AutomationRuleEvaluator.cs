using GloryLikeBackend.Dtos.CompanyTemplates;
namespace GloryLikeBackend.Services;

public static class AutomationRuleEvaluator
{
    public static readonly string[] Events = ["StageAdvanced", "CandidateHired", "VacancyClosed"];
    public static readonly string[] Fields = ["Age", "ExperienceYears", "Score", "MatchScore"];
    public static readonly string[] Operators = ["GreaterThan", "LessThan", "Equal", "GreaterOrEqual", "LessOrEqual"];
    public static string Validate(AutomationRule? rule)
    {
        if (rule is null || !Events.Contains(rule.EventType)) return "Select a supported event.";
        if (rule.EventType == "StageAdvanced" && (string.IsNullOrWhiteSpace(rule.TargetStageName) || rule.TargetStageName!.Trim().Length > 100))
            return "Select the destination funnel stage. This event condition is required.";
        if (rule.EventType != "StageAdvanced" && !string.IsNullOrEmpty(rule.TargetStageName))
            return "Destination stage only applies to a stage advancement event.";
        if (rule.LetterTemplateId == Guid.Empty) return "Select a candidate letter from Letters.";
        if (rule.Conditions is null || rule.Conditions.Count > 10) return "Use at most 10 candidate conditions.";
        foreach (var condition in rule.Conditions)
        {
            if (condition is null || !Fields.Contains(condition.Field) || !Operators.Contains(condition.Operator)) return "Select a valid candidate condition.";
            var maximum = condition.Field == "Age" ? 120m : 100m;
            if (condition.Value < 0 || condition.Value > maximum) return $"{condition.Field} must be between 0 and {maximum}.";
        }
        return string.Empty;
    }
    public static bool Matches(AutomationRule rule, string eventType, string targetStage,
        IReadOnlyDictionary<string, decimal?> metrics)
    {
        if (Validate(rule).Length > 0 || rule.EventType != eventType) return false;
        if (eventType == "StageAdvanced" && !string.Equals(rule.TargetStageName!.Trim(), targetStage.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        return rule.Conditions.All(c => metrics.TryGetValue(c.Field, out var value) && value.HasValue && Compare(value.Value, c.Operator, c.Value));
    }
    public static bool Compare(decimal actual, string op, decimal expected) => op switch
    {
        "GreaterThan" => actual > expected, "LessThan" => actual < expected,
        "Equal" => actual == expected, "GreaterOrEqual" => actual >= expected,
        "LessOrEqual" => actual <= expected, _ => false
    };
    public static int? Age(DateTime? birthDate, int legacyAge, DateTime now)
    {
        if (!birthDate.HasValue) return legacyAge is > 0 and <= 120 ? legacyAge : null;
        var age = now.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > now.Date.AddYears(-age)) age--;
        return age is >= 0 and <= 120 ? age : null;
    }
    // Source profiles store years rather than full dates. Merge overlaps before summing.
    public static decimal? ExperienceYears(IEnumerable<(string Start, string End)> experiences, DateTime now)
    {
        var intervals = new List<(DateTime Start, DateTime End)>();
        foreach (var entry in experiences)
        {
            if (!int.TryParse(entry.Start, out var start) || start < 1900 || start > now.Year) return null;
            var ongoing = string.IsNullOrWhiteSpace(entry.End) || new[] { "present", "current", "now" }.Contains(entry.End.Trim().ToLowerInvariant());
            var endDate = now.Date;
            if (!ongoing)
            {
                if (!int.TryParse(entry.End, out var end) || end < start || end > now.Year) return null;
                endDate = new DateTime(end, 1, 1);
            }
            intervals.Add((new DateTime(start, 1, 1), endDate));
        }
        if (intervals.Count == 0) return null;
        var ordered = intervals.OrderBy(i => i.Start).ToList();
        var current = ordered[0]; double days = 0;
        foreach (var interval in ordered.Skip(1))
        {
            if (interval.Start <= current.End) current.End = interval.End > current.End ? interval.End : current.End;
            else { days += (current.End - current.Start).TotalDays; current = interval; }
        }
        days += (current.End - current.Start).TotalDays;
        return Math.Round((decimal)(days / 365.2425), 2);
    }
}
