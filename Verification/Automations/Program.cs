using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// No external mailbox, live database or third-party test package is used.
var now = new DateTime(2026, 9, 11);
var letter = Guid.NewGuid();
AutomationRule Rule(string type = "StageAdvanced") => new()
{
    EventType = type, TargetStageName = type == "StageAdvanced" ? "Interview" : "",
    LetterTemplateId = letter
};
var metrics = new Dictionary<string, decimal?>
{
    ["Age"] = 30, ["ExperienceYears"] = 5.5m, ["Score"] = 72, ["MatchScore"] = 84
};
var count = 0;
void Check(bool passed, string description)
{
    if (!passed) throw new InvalidOperationException("FAILED: " + description);
    count++; Console.WriteLine("PASS: " + description);
}
foreach (var type in AutomationRuleEvaluator.Events)
    Check(AutomationRuleEvaluator.Matches(Rule(type), type, "Interview", metrics), type + " without optional conditions");
var invalid = Rule(); invalid.TargetStageName = null;
Check(AutomationRuleEvaluator.Validate(invalid).Length > 0, "Mandatory stage cannot be removed");
Check(!AutomationRuleEvaluator.Matches(Rule(), "StageAdvanced", "Offer", metrics), "Wrong destination does not match");
Check(AutomationRuleEvaluator.Matches(Rule(), "StageAdvanced", " interview ", metrics), "Stage comparison trims and ignores case");
Check(!AutomationRuleEvaluator.Matches(Rule(), "CandidateHired", "Interview", metrics), "Wrong event does not match");
var hired = Rule("CandidateHired"); hired.TargetStageName = null;
Check(AutomationRuleEvaluator.Validate(hired) == "", "Null optional stage is accepted for hired event");
foreach (var comparison in new[] { ("GreaterThan", 5m, 4m, true), ("GreaterThan", 5m, 5m, false),
    ("LessThan", 5m, 6m, true), ("LessThan", 5m, 5m, false), ("Equal", 5m, 5m, true),
    ("GreaterOrEqual", 5m, 5m, true), ("LessOrEqual", 5m, 5m, true) })
    Check(AutomationRuleEvaluator.Compare(comparison.Item2, comparison.Item1, comparison.Item3) == comparison.Item4,
        $"{comparison.Item2} {comparison.Item1} {comparison.Item3}");
var conditional = Rule();
conditional.Conditions = [new() { Field = "Age", Operator = "GreaterThan", Value = 25 },
    new() { Field = "MatchScore", Operator = "GreaterOrEqual", Value = 80 }];
Check(AutomationRuleEvaluator.Matches(conditional, "StageAdvanced", "Interview", metrics), "All conditions match");
metrics["MatchScore"] = 79;
Check(!AutomationRuleEvaluator.Matches(conditional, "StageAdvanced", "Interview", metrics), "One failed condition blocks email");
metrics["MatchScore"] = null;
Check(!AutomationRuleEvaluator.Matches(conditional, "StageAdvanced", "Interview", metrics), "Missing score does not become zero");
conditional.Conditions[0].Field = "Email";
Check(AutomationRuleEvaluator.Validate(conditional).Length > 0, "Unsupported profile field rejected");
invalid = Rule(); invalid.LetterTemplateId = Guid.Empty;
Check(AutomationRuleEvaluator.Validate(invalid).Length > 0, "Letter is required");
Check(AutomationRuleEvaluator.Age(new DateTime(1996, 9, 12), 99, now) == 29, "Birthday not yet reached");
Check(AutomationRuleEvaluator.Age(new DateTime(1996, 9, 11), 99, now) == 30, "Birthday reached");
Check(AutomationRuleEvaluator.Age(null, 0, now) is null, "Missing age stays unknown");
Check(AutomationRuleEvaluator.Age(null, 34, now) == 34, "Legacy saved age fallback");
var experience = AutomationRuleEvaluator.ExperienceYears([("2020", "2023"), ("2022", "2025")], now);
Check(experience is >= 4.99m and <= 5.01m, "Overlapping work periods counted once");
Check(AutomationRuleEvaluator.ExperienceYears([], now) is null, "Missing experience stays unknown");
Check(AutomationRuleEvaluator.ExperienceYears([("invalid", "2025")], now) is null, "Invalid experience stays unknown");
Check(AutomationRuleEvaluator.ExperienceYears([("2026", "Present")], now) is > 0.6m and < 0.8m, "Current employment uses current date");

using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
var receive = ReceiveSmtpAsync(listener, deadline.Token);
var sender = new SmtpRegistrationEmailSender(Options.Create(new SmtpOptions
{
    Host = "127.0.0.1", Port = port, FromEmail = "sender@example.test", FromName = "BothFind test", EnableSsl = false
}), NullLogger<SmtpRegistrationEmailSender>.Instance);
var deliveryId = Guid.NewGuid();
await sender.SendAutomationAsync("candidate@example.test", "Automation test", "Hello <b>candidate</b>!", deliveryId, deadline.Token);
var message = await receive;
listener.Stop();
Check(message.Contains("candidate@example.test"), "SMTP receives the intended recipient");
Check(message.Contains("Subject: Automation test", StringComparison.OrdinalIgnoreCase), "SMTP receives the rendered subject");
Check(message.Contains($"<{deliveryId:N}@bothfind.com>", StringComparison.OrdinalIgnoreCase), "Delivery has a stable Message-ID");
var unfolded = message.Replace("=\r\n", "").Replace("=\n", "");
var decoded = System.Text.RegularExpressions.Regex.Replace(unfolded, "=([0-9A-Fa-f]{2})",
    m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
Check(decoded.Contains("&lt;b&gt;candidate&lt;/b&gt;"), "Candidate text is HTML encoded");
var missingConfig = new SmtpRegistrationEmailSender(Options.Create(new SmtpOptions()), NullLogger<SmtpRegistrationEmailSender>.Instance);
var rejected = false;
try { await missingConfig.SendAutomationAsync("candidate@example.test", "Test", "Text", Guid.NewGuid(), deadline.Token); }
catch (InvalidOperationException) { rejected = true; }
Check(rejected, "Missing SMTP configuration fails instead of reporting success");
Console.WriteLine($"{count} backend checks passed. SQL integration and real mailbox delivery still require staging verification.");

static async Task<string> ReceiveSmtpAsync(TcpListener listener, CancellationToken ct)
{
    using var client = await listener.AcceptTcpClientAsync(ct);
    await using var stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
    await using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true) { NewLine = "\r\n", AutoFlush = true };
    await writer.WriteLineAsync("220 localhost ESMTP test");
    var message = new StringBuilder();
    while (await reader.ReadLineAsync(ct) is { } line)
    {
        if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
            await writer.WriteLineAsync("250 localhost");
        else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
            await writer.WriteLineAsync("250 OK");
        else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("354 End with a dot");
            while (await reader.ReadLineAsync(ct) is { } data && data != ".") message.AppendLine(data);
            await writer.WriteLineAsync("250 Accepted");
        }
        else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase)) { await writer.WriteLineAsync("221 Bye"); break; }
        else await writer.WriteLineAsync("250 OK");
    }
    return message.ToString();
}
