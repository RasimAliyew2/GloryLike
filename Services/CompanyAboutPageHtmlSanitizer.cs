using Ganss.Xss;
using GloryLikeBackend.Services.Interfaces;

namespace GloryLikeBackend.Services;

public sealed class CompanyAboutPageHtmlSanitizer
    : ICompanyAboutPageHtmlSanitizer
{
    private static readonly string[] SafeTags =
    [
        "a", "article", "aside", "blockquote", "br", "div", "em",
        "figcaption", "figure", "footer", "h1", "h2", "h3", "h4",
        "header", "hr", "img", "li", "main", "nav", "ol", "p",
        "section", "small", "span", "strong", "ul"
    ];

    private static readonly string[] SafeAttributes =
    [
        "alt", "aria-hidden", "aria-label", "class", "data-company-field",
        "data-company-section", "role", "style", "title"
    ];

    private static readonly string[] SafeCssProperties =
    [
        "align-items", "background-color", "border", "border-color",
        "border-radius", "border-style", "border-width", "box-shadow",
        "color", "column-gap", "display", "flex", "flex-basis",
        "flex-direction", "flex-grow", "flex-shrink", "flex-wrap",
        "font-family", "font-size", "font-style", "font-weight", "gap",
        "grid-template-columns", "height", "justify-content",
        "letter-spacing", "line-height", "margin", "margin-bottom",
        "margin-left", "margin-right", "margin-top", "max-height",
        "max-width", "min-height", "min-width", "object-fit", "opacity",
        "overflow", "padding", "padding-bottom", "padding-left",
        "padding-right", "padding-top", "row-gap", "text-align",
        "text-decoration", "text-transform", "white-space", "width"
    ];

    private readonly HtmlSanitizer _sanitizer;

    public CompanyAboutPageHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedSchemes.Clear();

        foreach (var tag in SafeTags)
            _sanitizer.AllowedTags.Add(tag);

        foreach (var attribute in SafeAttributes)
            _sanitizer.AllowedAttributes.Add(attribute);

        foreach (var property in SafeCssProperties)
            _sanitizer.AllowedCssProperties.Add(property);
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html.Trim());
    }
}
