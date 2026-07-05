using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Models;

public class JobOffer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string RequiredJob { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;


    [Required]
    [MaxLength(1500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Seniority { get; set; } = string.Empty;

    [Required]
    public string Skills { get; set; } = string.Empty;

    public int SkillsWeight { get; set; }
}
