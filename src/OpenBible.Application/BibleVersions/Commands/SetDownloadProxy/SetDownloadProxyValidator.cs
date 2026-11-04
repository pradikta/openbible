using FluentValidation;

namespace OpenBible.Application.BibleVersions.Commands.SetDownloadProxy;

public sealed class SetDownloadProxyValidator : AbstractValidator<SetDownloadProxyCommand>
{
    public SetDownloadProxyValidator()
    {
        RuleFor(x => x.ProxyUrlTemplate!)
            .Must(BeHttpTemplate)
            .When(x => !string.IsNullOrWhiteSpace(x.ProxyUrlTemplate))
            .WithMessage("Use an http(s) URL that contains {url}.");
    }

    private static bool BeHttpTemplate(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("{url}", StringComparison.Ordinal) &&
               Uri.TryCreate(trimmed.Replace("{url}", "x", StringComparison.Ordinal), UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
