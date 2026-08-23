namespace GloryLikeBackend.Models;

public sealed class CompanyCandidateMessage
{
    public int Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public int SenderUserId { get; set; }
    public int RecipientUserId { get; set; }
    public int CandidateUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public User CompanyOwner { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public User Recipient { get; set; } = null!;
    public User Candidate { get; set; } = null!;
}
