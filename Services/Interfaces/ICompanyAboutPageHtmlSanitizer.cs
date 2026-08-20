namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyAboutPageHtmlSanitizer
{
    string Sanitize(string? html);
}
