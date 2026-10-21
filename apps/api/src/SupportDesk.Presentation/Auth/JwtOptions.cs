namespace SupportDesk.Presentation.Auth;

/// <summary>
/// The "Jwt" configuration section. In every environment the signing key arrives from
/// configuration (the Jwt__SigningKey environment variable); it is never a literal in code.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key, at least 32 characters.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "supportdesk-api";

    public string Audience { get; set; } = "supportdesk-web";

    /// <summary>How long a token is valid. One hour: short enough to limit a leaked token, long enough for a shift of work without refresh tokens.</summary>
    public int LifetimeMinutes { get; set; } = 60;
}
